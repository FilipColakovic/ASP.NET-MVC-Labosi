param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot,
    [string]$TranscriptPath,
    [string]$UserInput,
    [int]$MaxAttempts = 30,
    [int]$SleepSeconds = 1
)

$logDir = Join-Path $RepoRoot "lab-1"
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

    if ($Message.PSObject -and $Message.PSObject.Properties) {
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

    $typeVal = if ($Row.PSObject.Properties.Name -contains "type") { [string]$Row.type } else { "" }
    $roleVal = if ($Row.PSObject.Properties.Name -contains "role") { [string]$Row.role } else { "" }
    $messageRoleVal = ""
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
        (Get-MessageText $Row.data),
        (Get-MessageText $Row.data.content),
        (Get-MessageText $Row.content),
        (Get-MessageText $Row.message),
        [string]$Row.data.content,
        [string]$Row.message.content,
        [string]$Row.content
    )
}

$pathUsed = $TranscriptPath
$agentOutput = $null
$attempt = 0

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
                $row = $line | ConvertFrom-Json
                $rows += $row
                if (Is-AssistantRow $row) {
                    $agentOutput = Get-FirstNonEmpty @(
                        (Get-AssistantOutputFromRow $row),
                        $agentOutput
                    )
                }
            }
            catch {
                # Ignore malformed rows.
            }
        }
    }

    @(
        "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
        "FinalizeRetry: true attempt=$attempt rows=$($rows.Count) foundOutput=$(-not [string]::IsNullOrWhiteSpace($agentOutput)) path=$pathUsed"
        ""
    ) | Add-Content -Path $debugFile -Encoding utf8

    if (-not [string]::IsNullOrWhiteSpace($agentOutput)) {
        break
    }

    Start-Sleep -Seconds $SleepSeconds
}

$entry = @(
    "Timestamp: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
    "Status: final"
    "User Input: $(Get-FirstNonEmpty @($UserInput, '[not found]'))"
    "Agent output: $(Get-FirstNonEmpty @($agentOutput, '[not found]'))"
    ""
)
$entry | Add-Content -Path $logFile -Encoding utf8
