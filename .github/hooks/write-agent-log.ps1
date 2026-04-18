# Logs one completed turn: latest user message + latest agent response.
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$logDir = Join-Path $repoRoot "lab-3"
$logFile = Join-Path $logDir "agent_log.log"
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

function Get-LatestStringByPropertyNames {
    param(
        $Node,
        [string[]]$PropertyNames
    )

    if ($null -eq $Node) { return $null }

    $latest = $null

    if ($Node -is [System.Collections.IEnumerable] -and -not ($Node -is [string])) {
        foreach ($item in $Node) {
            $candidate = Get-LatestStringByPropertyNames -Node $item -PropertyNames $PropertyNames
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                $latest = $candidate
            }
        }
        return $latest
    }

    if ($Node.PSObject -and $Node.PSObject.Properties) {
        foreach ($prop in $Node.PSObject.Properties) {
            if ($PropertyNames -contains $prop.Name) {
                $candidate = $null
                if ($prop.Value -is [string]) {
                    $candidate = [string]$prop.Value
                }
                elseif ($null -ne $prop.Value -and $prop.Value.PSObject -and $prop.Value.PSObject.Properties) {
                    $candidate = Get-FirstNonEmpty @(
                        [string]$prop.Value.name,
                        [string]$prop.Value.id,
                        [string]$prop.Value.model,
                        [string]$prop.Value.modelName
                    )
                }
                if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                    $latest = $candidate
                }
            }

            $candidate = Get-LatestStringByPropertyNames -Node $prop.Value -PropertyNames $PropertyNames
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                $latest = $candidate
            }
        }
    }

    return $latest
}

function Normalize-AgentMode {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) { return $null }

    $v = $Value.Trim().ToLowerInvariant()
    if ($v -in @("agent", "ask", "plan")) { return $v }
    if ($v -like "*custom*") { return "custom" }
    if ($v -like "copilot-*") {
        $suffix = $v.Substring(8)
        if ($suffix -in @("agent", "ask", "plan")) { return $suffix }
        if (-not [string]::IsNullOrWhiteSpace($suffix)) { return $suffix }
    }

    return $null
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
$agentName = $null
$agentMode = $null
$sessionProducer = $null
$assistantUsedTools = $false
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

    $agentName = Get-FirstNonEmpty @(
        [string]$payload.agentName,
        [string]$payload.agent,
        [string]$payload.modelName,
        [string]$payload.model,
        [string]$payload.hookSpecificOutput.agentName,
        [string]$payload.hookSpecificOutput.agent,
        [string]$payload.hookSpecificOutput.modelName,
        [string]$payload.hookSpecificOutput.model,
        (Get-LatestStringByPropertyNames -Node $payload -PropertyNames @("agentName", "agent", "modelName", "model"))
    )

    $agentMode = Get-FirstNonEmpty @(
        (Normalize-AgentMode ([string]$payload.mode)),
        (Normalize-AgentMode ([string]$payload.chatMode)),
        (Normalize-AgentMode ([string]$payload.agentMode)),
        (Normalize-AgentMode ([string]$payload.hookSpecificOutput.mode)),
        (Normalize-AgentMode ([string]$payload.hookSpecificOutput.chatMode)),
        (Normalize-AgentMode ([string]$payload.hookSpecificOutput.agentMode)),
        (Normalize-AgentMode (Get-LatestStringByPropertyNames -Node $payload -PropertyNames @("mode", "chatMode", "agentMode", "producer")))
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
    foreach ($line in Get-Content -LiteralPath $transcriptPath -ErrorAction SilentlyContinue) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try {
            $row = $line | ConvertFrom-Json
            if ($row.type -eq "user.message") {
                $userInput = Get-FirstNonEmpty @(
                    (Get-MessageText $row.data),
                    (Get-MessageText $row.data.content),
                    [string]$row.data.content
                )
            }
            elseif (Is-AssistantRow $row) {
                if ($row.PSObject.Properties.Name -contains "data" -and $null -ne $row.data) {
                    if ($row.data.PSObject.Properties.Name -contains "toolRequests" -and $null -ne $row.data.toolRequests) {
                        if (@($row.data.toolRequests).Count -gt 0) {
                            $assistantUsedTools = $true
                        }
                    }
                }
                $agentOutput = Get-FirstNonEmpty @(
                    (Get-AssistantOutputFromRow $row),
                    $agentOutput
                )
                $agentName = Get-FirstNonEmpty @(
                    $agentName,
                    (Get-LatestStringByPropertyNames -Node $row -PropertyNames @("agentName", "agent", "modelName", "model"))
                )
                $agentMode = Get-FirstNonEmpty @(
                    $agentMode,
                    (Normalize-AgentMode (Get-LatestStringByPropertyNames -Node $row -PropertyNames @("mode", "chatMode", "agentMode", "producer")))
                )
            }
            elseif ($row.type -eq "session.start") {
                $sessionProducer = Get-FirstNonEmpty @(
                    $sessionProducer,
                    [string]$row.data.producer
                )
            }
        }
        catch {
            # Ignore malformed transcript lines.
        }
    }
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

if (-not $agentName) {
    $agentNameMatch = [regex]::Match($rawPayload, '"agentName"\s*:\s*"(?<v>(?:\\.|[^"])*)"|"modelName"\s*:\s*"(?<v2>(?:\\.|[^"])*)"|"model"\s*:\s*"(?<v3>(?:\\.|[^"])*)"')
    if ($agentNameMatch.Success) {
        $candidate = if ($agentNameMatch.Groups["v"].Success) { $agentNameMatch.Groups["v"].Value } elseif ($agentNameMatch.Groups["v2"].Success) { $agentNameMatch.Groups["v2"].Value } else { $agentNameMatch.Groups["v3"].Value }
        if ($candidate) { $agentName = [regex]::Unescape($candidate) }
    }
}

if (-not $agentMode) {
    $agentModeMatch = [regex]::Match($rawPayload, '"mode"\s*:\s*"(?<v>(?:\\.|[^"])*)"|"chatMode"\s*:\s*"(?<v2>(?:\\.|[^"])*)"|"agentMode"\s*:\s*"(?<v3>(?:\\.|[^"])*)"|"producer"\s*:\s*"(?<v4>(?:\\.|[^"])*)"')
    if ($agentModeMatch.Success) {
        $candidate = if ($agentModeMatch.Groups["v"].Success) { $agentModeMatch.Groups["v"].Value } elseif ($agentModeMatch.Groups["v2"].Success) { $agentModeMatch.Groups["v2"].Value } elseif ($agentModeMatch.Groups["v3"].Success) { $agentModeMatch.Groups["v3"].Value } else { $agentModeMatch.Groups["v4"].Value }
        if ($candidate) { $agentMode = Normalize-AgentMode ([regex]::Unescape($candidate)) }
    }
}

$eventName = [string](Get-FirstNonEmpty @($eventName, "unknown"))
$eventNameLower = $eventName.ToLowerInvariant()

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
            if ($row.PSObject.Properties.Name -contains "data" -and $null -ne $row.data) {
                if ($row.data.PSObject.Properties.Name -contains "toolRequests" -and $null -ne $row.data.toolRequests) {
                    if (@($row.data.toolRequests).Count -gt 0) {
                        $assistantUsedTools = $true
                    }
                }
            }
            $agentOutput = Get-FirstNonEmpty @(
                (Get-AssistantOutputFromRow $row),
                $agentOutput
            )
            $agentName = Get-FirstNonEmpty @(
                $agentName,
                (Get-LatestStringByPropertyNames -Node $row -PropertyNames @("agentName", "agent", "modelName", "model"))
            )
            $agentMode = Get-FirstNonEmpty @(
                $agentMode,
                (Normalize-AgentMode (Get-LatestStringByPropertyNames -Node $row -PropertyNames @("mode", "chatMode", "agentMode", "producer")))
            )
        }
        elseif ($row.type -eq "session.start") {
            $sessionProducer = Get-FirstNonEmpty @(
                $sessionProducer,
                [string]$row.data.producer
            )
        }
    }
}

$agentMode = Get-FirstNonEmpty @(
    $agentMode,
    (Normalize-AgentMode $sessionProducer),
    ($(if ($assistantUsedTools) { "agent" } else { $null })),
    "unknown"
)

$agentLabel = "unknown"
if (-not [string]::IsNullOrWhiteSpace($agentMode) -and $agentMode -ne "unknown") {
    $agentLabel = $agentMode
    if (-not [string]::IsNullOrWhiteSpace($agentName)) {
        $agentNameLower = $agentName.Trim().ToLowerInvariant()
        if ($agentNameLower -ne $agentMode) {
            $agentLabel = "$agentMode ($agentName)"
        }
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($agentName)) {
    $agentLabel = $agentName
}

$entry = @(
    "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "Agent: $agentLabel"
    "User Input: $(Get-FirstNonEmpty @($userInput, '[not found]'))"
    "Agent output: $(Get-FirstNonEmpty @($agentOutput, '[not found]'))"
    ""
)
$entry | Add-Content -Path $logFile -Encoding utf8
