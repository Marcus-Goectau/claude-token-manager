param(
    [switch]$StartWithWindows
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\ClaudeTokenManager\ClaudeTokenManager.csproj'
$publishDir = Join-Path $PSScriptRoot 'publish'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeTokenManager'
$exe = Join-Path $installDir 'ClaudeTokenManager.exe'
$uninstallScript = Join-Path $installDir 'uninstall.ps1'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeTokenManager'

dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=none -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

# The exe is locked while the app is running.
Get-Process ClaudeTokenManager -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force $installDir | Out-Null
Copy-Item (Join-Path $publishDir 'ClaudeTokenManager.exe') $exe -Force
Copy-Item (Join-Path $PSScriptRoot 'uninstall.ps1') $uninstallScript -Force

$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Claude Token Manager.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $installDir
$shortcut.Save()

if ($StartWithWindows) {
    Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'ClaudeTokenManager' -Value "`"$exe`""
}

# Lists the app under Settings > Apps > Installed apps for the current user.
$version = (Get-Item $exe).VersionInfo.ProductVersion -replace '\+.*$', ''
$sizeKb = [int][Math]::Ceiling((Get-ChildItem $installDir -File | Measure-Object Length -Sum).Sum / 1KB)
# Bypass applies to this one run only, so a later policy change can't leave the app impossible to uninstall.
$uninstallCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallScript`""

New-Item -Force $uninstallKey | Out-Null
$values = @{
    DisplayName     = 'Claude Token Manager'
    DisplayVersion  = $version
    Publisher       = $env:USERNAME
    DisplayIcon     = "`"$exe`",0"
    InstallLocation = $installDir
    UninstallString = $uninstallCommand
    InstallDate     = (Get-Date -Format 'yyyyMMdd')
}
foreach ($name in $values.Keys) {
    Set-ItemProperty $uninstallKey -Name $name -Value $values[$name]
}
Set-ItemProperty $uninstallKey -Name EstimatedSize -Value $sizeKb -Type DWord
Set-ItemProperty $uninstallKey -Name NoModify -Value 1 -Type DWord
Set-ItemProperty $uninstallKey -Name NoRepair -Value 1 -Type DWord

Start-Process $exe
Write-Host "Installed version $version to $installDir. Look for the icon in the system tray."
