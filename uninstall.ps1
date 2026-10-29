param(
    # Also delete saved profiles and the tokens stored in Credential Manager.
    [switch]$RemoveData,
    # Keep saved profiles and tokens without asking.
    [switch]$KeepData
)

$ErrorActionPreference = 'Stop'

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeTokenManager'
$dataDir = Join-Path $env:APPDATA 'ClaudeTokenManager'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Claude Token Manager.lnk'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeTokenManager'

Get-Process ClaudeTokenManager -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Remove-ItemProperty $runKey -Name 'ClaudeTokenManager' -ErrorAction SilentlyContinue
Remove-Item $shortcutPath -Force -ErrorAction SilentlyContinue

# This script may be running from the install folder, so move out before deleting it.
Set-Location $env:TEMP
Remove-Item $installDir -Recurse -Force -ErrorAction SilentlyContinue

# Keep the Installed apps entry when files are left behind, so the uninstall can be retried.
if (Test-Path $installDir) {
    Write-Host "Uninstall failed: could not delete $installDir. Close anything using it and run Uninstall again."
    if ($Host.Name -eq 'ConsoleHost' -and [Environment]::UserInteractive) { Read-Host 'Press Enter to close' }
    exit 1
}
Remove-Item $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue

if (-not $RemoveData -and -not $KeepData) {
    $answer = Read-Host 'Also delete your saved profiles and the tokens stored in Credential Manager? (y/N)'
    $RemoveData = $answer -match '^(y|yes)$'
}

if ($RemoveData) {
    $targets = cmdkey /list | Select-String 'target=(ClaudeTokenManager:\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }
    foreach ($target in $targets) {
        cmdkey /delete:$target | Out-Null
    }
    Remove-Item $dataDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Removed $(@($targets).Count) stored tokens and the profile list."
}
else {
    Write-Host "Kept your profiles in $dataDir and your tokens in Credential Manager."
}

Write-Host 'Claude Token Manager has been uninstalled.'

# Keep the window open when launched from Installed apps.
if ($Host.Name -eq 'ConsoleHost' -and [Environment]::UserInteractive) {
    Read-Host 'Press Enter to close'
}
