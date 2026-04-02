# Logs one completed turn: latest user message + latest agent response.
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$logDir = Join-Path $repoRoot "lab-1"
$logFile = Join-Path $logDir "agent_log.txt"
$debugFile = Join-Path $logDir "agent_log_debug.txt"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Get-FirstNonEmpty {
    param([string[]]$Values)
    foreach ($value in $Values) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }
    return $null
}

function Get-MessageText {
    param($Message)

    if ($null -eq $Message) { return $null }
    if ($Message -is [string]) { return [string]$Message }

    if ($Message.PSObject.Properties.Name -contains "content") {
        $contentText = Get-MessageText $Message.content
        if (-not [string]::IsNullOrWhiteSpace($contentText)) { return $contentText }
    }
    if ($Message.PSObject.Properties.Name -contains "text") {
        $textValue = Get-MessageText $Message.text
        if (-not [string]::IsNullOrWhiteSpace($textValue)) { return $textValue }
    }
    if ($Message.PSObject.Properties.Name -contains "data") {
        $dataText = Get-MessageText $Message.data
        if (-not [string]::IsNullOrWhiteSpace($dataText)) { return $dataText }
    }

    if ($Message -is [System.Collections.IEnumerable] -and -not ($Message -is [string])) {
        $parts = @()
        foreach ($item in $Message) {
            $itemText = Get-MessageText $item
            if (-not [string]::IsNullOrWhiteSpace($itemText)) {
                $parts += $itemText
            }
        }
        if ($parts.Count -gt 0) {
            return ($parts -join " ")
        }
    }

    return $null
}

function Get-LatestTranscriptPath {
    $transcriptRoot = Join-Path $env:APPDATA "Code\User\workspaceStorage"
    if (-not (Test-Path -LiteralPath $transcriptRoot)) { return $null }

    $latest = Get-ChildItem -Path $transcriptRoot -Recurse -Filter *.jsonl -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*GitHub.copilot-chat*transcripts*" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latest) { return $null }
    return $latest.FullName
}

function Is-AssistantRow {
    param($Row)

    if ($null -eq $Row) { return $false }

    $typeVal = ""
    $roleVal = ""
    $messageRoleVal = ""

    if ($Row.PSObject.Properties.Name -contains "type") {
        $typeVal = [string]$Row.type
    }
    if ($Row.PSObject.Properties.Name -contains "role") {
        $roleVal = [string]$Row.role
    }
    if ($Row.PSObject.Properties.Name -contains "message" -and $null -ne $Row.message) {
        if ($Row.message.PSObject.Properties.Name -contains "role") {
            $messageRoleVal = [string]$Row.message.role
        }
    }

    return (
        $typeVal -eq "assistant.message" -or
        $typeVal -like "*assistant*" -or
        $roleVal -eq "assistant" -or
        $messageRoleVal -eq "assistant"
    )
}

function Get-AssistantOutputFromRow {
    param($Row)

    return Get-FirstNonEmpty @(
        (Get-MessageText $Row),
        (Get-MessageText $Row.data),
        (Get-MessageText $Row.content),
        (Get-MessageText $Row.message),
        (Get-MessageText $Row.message.content),
        (Get-MessageText $Row.delta),
        (Get-MessageText $Row.parts),
        [string]$Row.data.content,
        [string]$Row.message.content,
        [string]$Row.delta,
        [string]$Row.parts
    )
}

function Get-LatestMessageTextByType {
    param(
        $Node,
        [string]$WantedType
    )

    if ($null -eq $Node) { return $null }

    $latest = $null

    if ($Node -is [System.Collections.IEnumerable] -and -not ($Node -is [string])) {
        foreach ($item in $Node) {
            $candidate = Get-LatestMessageTextByType -Node $item -WantedType $WantedType
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                $latest = $candidate
            }
        }
        return $latest
    }

    if ($Node.PSObject -and $Node.PSObject.Properties) {
        $nodeType = if ($Node.PSObject.Properties.Name -contains "type") { [string]$Node.type } else { "" }
        if ($nodeType -eq $WantedType) {
            $msg = Get-FirstNonEmpty @(
                (Get-MessageText $Node.data),
                (Get-MessageText $Node.data.content),
                (Get-MessageText $Node.content),
                [string]$Node.data.content,
                [string]$Node.content
            )
            if (-not [string]::IsNullOrWhiteSpace($msg)) {
                $latest = $msg
            }
        }

        foreach ($prop in $Node.PSObject.Properties) {
            $candidate = Get-LatestMessageTextByType -Node $prop.Value -WantedType $WantedType
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                $latest = $candidate
            }
        }
    }

    return $latest
}

function Read-TranscriptWithRetry {
    param(
        [string]$InitialTranscriptPath,
        [int]$MaxAttempts = 20,
        [int]$SleepSeconds = 1
    )

    $attempt = 0
    $rows = @()
    $pathUsed = $InitialTranscriptPath

    while ($attempt -lt $MaxAttempts) {
        $attempt += 1

        if ([string]::IsNullOrWhiteSpace($pathUsed) -or -not (Test-Path -LiteralPath $pathUsed)) {
            $pathUsed = Get-LatestTranscriptPath
        }

        $rows = @()
        if (-not [string]::IsNullOrWhiteSpace($pathUsed) -and (Test-Path -LiteralPath $pathUsed)) {
            foreach ($line in Get-Content -LiteralPath $pathUsed -ErrorAction SilentlyContinue) {
                if ([string]::IsNullOrWhiteSpace($line)) { continue }
                try {
                    $rows += ($line | ConvertFrom-Json)
                }
                catch {
                    # Ignore malformed transcript lines.
                }
            }
        }

        @(
            "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
            "RetryRead: true attempt=$attempt rows=$($rows.Count) path=$pathUsed"
            ""
        ) | Add-Content -Path $debugFile -Encoding utf8

        if ($rows.Count -gt 0) {
            break
        }

        Start-Sleep -Seconds $SleepSeconds
    }

    return [pscustomobject]@{
        Path = $pathUsed
        Rows = @($rows)
        Attempts = $attempt
    }
}

$rawPayload = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($rawPayload)) {
    exit 0
}

$transcriptPath = $null
$eventName = $null
$userInput = $null
$agentOutput = $null
$lastUserMessage = $null
$lastAssistantMessage = $null
try {
    $payload = $rawPayload | ConvertFrom-Json
    $eventName = [string]$payload.hook_event_name
    if ([string]::IsNullOrWhiteSpace($eventName)) {
        $eventName = [string]$payload.hookEventName
    }
    if ([string]::IsNullOrWhiteSpace($eventName)) {
        $eventName = [string]$payload.hookSpecificOutput.hookEventName
    }

    $transcriptPath = [string]$payload.transcript_path
    if ([string]::IsNullOrWhiteSpace($transcriptPath)) {
        $transcriptPath = [string]$payload.transcriptPath
    }
    if ([string]::IsNullOrWhiteSpace($transcriptPath)) {
        $transcriptPath = [string]$payload.hookSpecificOutput.transcript_path
    }
    if ([string]::IsNullOrWhiteSpace($transcriptPath)) {
        $transcriptPath = [string]$payload.hookSpecificOutput.transcriptPath
    }

    $userInput = Get-FirstNonEmpty @(
        [string]$payload.userPrompt,
        [string]$payload.prompt,
        [string]$payload.input,
        [string]$payload.message,
        [string]$payload.hookSpecificOutput.userPrompt,
        [string]$payload.hookSpecificOutput.prompt,
        [string]$payload.hookSpecificOutput.input,
        [string]$payload.hookSpecificOutput.message
    )

    $agentOutput = Get-FirstNonEmpty @(
        [string]$payload.agentResponse,
        [string]$payload.response,
        [string]$payload.output,
        [string]$payload.hookSpecificOutput.agentResponse,
        [string]$payload.hookSpecificOutput.response,
        [string]$payload.hookSpecificOutput.output
    )

    $payloadUserFromRows = Get-LatestMessageTextByType -Node $payload -WantedType "user.message"
    $payloadAssistantFromRows = Get-LatestMessageTextByType -Node $payload -WantedType "assistant.message"
    $userInput = Get-FirstNonEmpty @($userInput, $payloadUserFromRows)
    $agentOutput = Get-FirstNonEmpty @($agentOutput, $payloadAssistantFromRows)

    $messages = @()
    if ($payload.messages) { $messages = @($payload.messages) }
    elseif ($payload.transcript.messages) { $messages = @($payload.transcript.messages) }
    elseif ($payload.conversation.messages) { $messages = @($payload.conversation.messages) }
    elseif ($payload.hookSpecificOutput.messages) { $messages = @($payload.hookSpecificOutput.messages) }

    if ($messages.Count -gt 0) {
        $lastUserMessage = $messages | Where-Object { $_.role -eq "user" -or $_.type -eq "user.message" } | Select-Object -Last 1
        $lastAssistantMessage = $messages | Where-Object { $_.role -eq "assistant" -or $_.type -eq "assistant.message" } | Select-Object -Last 1

        if (-not $userInput) {
            $userInput = Get-MessageText $lastUserMessage
        }
        if (-not $agentOutput) {
            $agentOutput = Get-MessageText $lastAssistantMessage
        }
    }
}
catch {
    # Continue with regex fallback parsing.
}

if (-not [string]::IsNullOrWhiteSpace($transcriptPath) -and (Test-Path -LiteralPath $transcriptPath)) {
    $debugRows = @()
    foreach ($line in Get-Content -LiteralPath $transcriptPath -ErrorAction SilentlyContinue) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try {
            $row = $line | ConvertFrom-Json
            $debugRows += $row
            if ($row.type -eq "user.message") {
                $userInput = Get-FirstNonEmpty @(
                    (Get-MessageText $row.data),
                    (Get-MessageText $row.data.content),
                    [string]$row.data.content
                )
            }
            elseif (Is-AssistantRow $row) {
                $agentOutput = Get-FirstNonEmpty @(
                    (Get-AssistantOutputFromRow $row),
                    $agentOutput
                )
            }
        }
        catch {
            # Ignore malformed transcript lines.
        }
    }

    # Temporary debug: capture the latest transcript row shapes.
    $lastRows = @($debugRows | Select-Object -Last 10)
    $debugLines = @(
        "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
        "TranscriptPath: $transcriptPath"
        "RecentRows: $($lastRows.Count)"
    )
    foreach ($r in $lastRows) {
        $typeVal = if ($r.PSObject.Properties.Name -contains 'type') { [string]$r.type } else { '' }
        $roleVal = if ($r.PSObject.Properties.Name -contains 'role') { [string]$r.role } else { '' }
        $msgRoleVal = ''
        if ($r.PSObject.Properties.Name -contains 'message' -and $null -ne $r.message) {
            if ($r.message.PSObject.Properties.Name -contains 'role') {
                $msgRoleVal = [string]$r.message.role
            }
        }
        $debugLines += "type=$typeVal role=$roleVal message.role=$msgRoleVal"
        $debugLines += (($r | ConvertTo-Json -Depth 10 -Compress))
    }
    $debugLines += ""
    $debugLines | Add-Content -Path $debugFile -Encoding utf8
}

if ([string]::IsNullOrWhiteSpace($eventName)) {
    $eventMatch = [regex]::Match($rawPayload, '"hook_event_name"\s*:\s*"(?<v>[^"]+)"|"hookEventName"\s*:\s*"(?<v2>[^"]+)"')
    if ($eventMatch.Success) {
        $eventName = if ($eventMatch.Groups["v"].Success) { $eventMatch.Groups["v"].Value } else { $eventMatch.Groups["v2"].Value }
    }
}

if (-not $userInput) {
    $promptMatch = [regex]::Match($rawPayload, '"userPrompt"\s*:\s*"(?<v>(?:\\.|[^"])*)"|"prompt"\s*:\s*"(?<v2>(?:\\.|[^"])*)"|"message"\s*:\s*"(?<v3>(?:\\.|[^"])*)"')
    if ($promptMatch.Success) {
        $candidate = if ($promptMatch.Groups["v"].Success) { $promptMatch.Groups["v"].Value } elseif ($promptMatch.Groups["v2"].Success) { $promptMatch.Groups["v2"].Value } else { $promptMatch.Groups["v3"].Value }
        if ($candidate) { $userInput = [regex]::Unescape($candidate) }
    }
}

if (-not $agentOutput) {
    $responseMatch = [regex]::Match($rawPayload, '"agentResponse"\s*:\s*"(?<v>(?:\\.|[^"])*)"|"response"\s*:\s*"(?<v2>(?:\\.|[^"])*)"|"output"\s*:\s*"(?<v3>(?:\\.|[^"])*)"')
    if ($responseMatch.Success) {
        $candidate = if ($responseMatch.Groups["v"].Success) { $responseMatch.Groups["v"].Value } elseif ($responseMatch.Groups["v2"].Success) { $responseMatch.Groups["v2"].Value } else { $responseMatch.Groups["v3"].Value }
        if ($candidate) { $agentOutput = [regex]::Unescape($candidate) }
    }
}

$eventName = [string](Get-FirstNonEmpty @($eventName, "unknown"))
$eventNameLower = $eventName.ToLowerInvariant()

@(
    "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
    "HookEvent: $eventName"
    "TranscriptPath: $transcriptPath"
    ""
) | Add-Content -Path $debugFile -Encoding utf8

# On submit, do not write to main log (final-only policy).
if ($eventNameLower -eq "userpromptsubmit") {
    exit 0
}

# Any non-submit hook invocation is treated as completion.
Start-Sleep -Seconds 2

$retryResult = Read-TranscriptWithRetry -InitialTranscriptPath $transcriptPath -MaxAttempts 20 -SleepSeconds 1
$transcriptPath = [string]$retryResult.Path
$debugRows = @($retryResult.Rows)
if ($debugRows.Count -gt 0) {
    foreach ($row in $debugRows) {
        if ($row.type -eq "user.message") {
            $userInput = Get-FirstNonEmpty @(
                (Get-MessageText $row.data),
                (Get-MessageText $row.data.content),
                [string]$row.data.content,
                $userInput
            )
        }
        elseif (Is-AssistantRow $row) {
            $agentOutput = Get-FirstNonEmpty @(
                (Get-AssistantOutputFromRow $row),
                $agentOutput
            )
        }
    }

    # Temporary debug: capture latest transcript row shapes for completion read.
    $lastRows = @($debugRows | Select-Object -Last 10)
    $debugLines = @(
        "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
        "CompletionRead: true"
        "TranscriptPath: $transcriptPath"
        "Attempts: $($retryResult.Attempts)"
        "RecentRows: $($lastRows.Count)"
    )
    foreach ($r in $lastRows) {
        $typeVal = if ($r.PSObject.Properties.Name -contains 'type') { [string]$r.type } else { '' }
        $roleVal = if ($r.PSObject.Properties.Name -contains 'role') { [string]$r.role } else { '' }
        $msgRoleVal = ''
        if ($r.PSObject.Properties.Name -contains 'message' -and $null -ne $r.message) {
            if ($r.message.PSObject.Properties.Name -contains 'role') {
                $msgRoleVal = [string]$r.message.role
            }
        }
        $debugLines += "type=$typeVal role=$roleVal message.role=$msgRoleVal"
        $debugLines += (($r | ConvertTo-Json -Depth 10 -Compress))
    }
    $debugLines += ""
    $debugLines | Add-Content -Path $debugFile -Encoding utf8
}

$entry = @(
    "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
    "Status: final"
    "User Input: $(Get-FirstNonEmpty @($userInput, '[not found]'))"
    "Agent output: $(Get-FirstNonEmpty @($agentOutput, '[not found]'))"
    ""
)
$entry | Add-Content -Path $logFile -Encoding utf8
