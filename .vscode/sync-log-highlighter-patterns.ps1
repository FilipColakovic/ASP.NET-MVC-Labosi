$ErrorActionPreference = "Stop"

$settingsPath = Join-Path $PSScriptRoot "settings.json"
$patternsPath = Join-Path $PSScriptRoot "logFileHighlighter.customPatterns.json"

if (-not (Test-Path $settingsPath)) { throw "Settings file not found: $settingsPath" }
if (-not (Test-Path $patternsPath)) { throw "Patterns file not found: $patternsPath" }

$settings = Get-Content $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
$patterns = Get-Content $patternsPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($null -ne $settings.PSObject.Properties['logFileHighlighter.customPatterns']) {
	$settings.'logFileHighlighter.customPatterns' = @($patterns)
} else {
	$settings | Add-Member -MemberType NoteProperty -Name "logFileHighlighter.customPatterns" -Value @($patterns)
}

$settings | ConvertTo-Json -Depth 50 | Set-Content $settingsPath -Encoding UTF8
Write-Host "Updated logFileHighlighter.customPatterns in .vscode/settings.json"
