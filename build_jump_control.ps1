param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("csdk", "deadlock")]
    [string]$Target
)

Push-Location $PSScriptRoot

Write-Host "Running VDataEditor addhero $Target..."
dotnet run --project ".\VDataEditor\VDataEditor.csproj" -- $Target jumpcontrol

if ($LASTEXITCODE -ne 0) {
    Write-Error "VDataEditor failed with exit code $LASTEXITCODE. Aborting."
    exit $LASTEXITCODE
}

Write-Host "VDataEditor completed successfully."
Write-Host ""
 
  
  
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jump_control\maps\jump_control.vmap" "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput"
 
C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jump_control\maps\jump_control.vmap" -noassert -world -nolightmaps  -phys -nav -retail -breakpad -nop4 -outroot  "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game"
    
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\generated\heroes_modified.vdata" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jump_control\scripts\heroes.vdata" -Force
  
# ===== CONFIGURATION =====
$InputFolder = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jump_control"  # Change this to your target folder
$ResourceCompilerPath = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe"  # Full path if not in PATH
# ========================

# Validate input folder exists
if (-not (Test-Path -Path $InputFolder -PathType Container)) {
    Write-Error "Input folder does not exist: $InputFolder"(TODO: Hammer clipboard data on the system clipboard.)
    exit 1
}

# Get all files in the folder, excluding .vmap and .vpk files
$filesToCompile = Get-ChildItem -Path $InputFolder -File -Recurse | 
    Where-Object { $_.Extension -ne ".vmap" -and $_.Extension -ne ".dmx" -and $_.Extension -ne ".vpk"  -and $_.Extension -ne ".vpulse" -and $_.Extension -ne ".ron"  -and $_.Extension -ne ".png"}

if ($filesToCompile.Count -eq 0) {
    Write-Warning "No files found to compile (excluding .vmap and .vpk files)"
    exit 0
}
 
# Build the file list argument
$filesList = $filesToCompile.FullName -join ' '

Write-Host "Found $($filesToCompile.Count) file(s) to compile"
Write-Host "Executing: $ResourceCompilerPath -v -game C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel -i $filesList"
Write-Host ""


if ($hasError) {
    Write-Error "One or more files failed to compile."
    exit 1
}

$pulseFiles = Get-ChildItem -Path $InputFolder -File -Recurse | Where-Object { $_.Extension -eq ".vpulse" }
$PulseCompilerPath = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_tools\win64\resourcecompiler.exe"

foreach ($file in $pulseFiles) {
    Write-Host "Compiling pulse: $($file.FullName)"
    & $PulseCompilerPath -game "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel" -danger_mode_ignore_schema_mismatches -i $file.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to compile pulse: $($file.FullName) (exit code $LASTEXITCODE)"
        $hasError = $true
    }
}

Write-Host ""
Write-Host "Compilation completed successfully!"

$wshell = New-Object -ComObject WScript.Shell

$proc = Start-Process "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin\win64\CSDKCfgVPK.exe" `
    -ArgumentList @(
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jump_control",
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk"
    ) -PassThru

Start-Sleep 1
$wshell.AppActivate($proc.Id)
$wshell.SendKeys("{ENTER}")

 
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk" "C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\addons\pak03_dir.vpk" -Force


Get-Process -Name "deadlock" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "vconsole2" -ErrorAction SilentlyContinue | Stop-Process -Force

# Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap\scripts\heroes.vdata" -Force


