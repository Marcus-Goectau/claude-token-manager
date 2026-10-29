param(
    [switch]$StartWithWindows
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\ClaudeTokenManager\ClaudeTokenManager.csproj'
$publishDir = Join-Path $PSScriptRoot 'publish'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeTokenManager'
$exe = Join-Path $installDir 'ClaudeTokenManager.exe'

dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=none -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

# The exe is locked while the app is running.
Get-Process ClaudeTokenManager -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force $installDir | Out-Null
Copy-Item (Join-Path $publishDir 'ClaudeTokenManager.exe') $exe -Force

$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Claude Token Manager.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $installDir
$shortcut.Save()

if ($StartWithWindows) {
    Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'ClaudeTokenManager' -Value "`"$exe`""
}

Start-Process $exe
Write-Host "Installed to $installDir. Look for the icon in the system tray."
