
<#

Objective is simple : 

PREEXISTING CONTENT FOLDERS 
jumping_map_map (map) | jumping_map_script (vdata/vpulse)


>Compile folder 1 map (.vpk)
>Move modified heroes.vdata from VDataEditor Project to jumping_map_script CONTENT
>Move compiled vpk map to jumping_map_script GAME folder
>Compile jumping_map_script (vdata/vpulse)
>Package jumping_map_script (containing vdata_c compiled & map vpk)  directly into game/addons folder

 
<#backup#>

#fast compile
C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads  -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\testtimer\maps\template.vmap" -noassert -world -nolightmaps -phys -nav -retail -breakpad -nop4 -outroot "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game"


 #-and $_.Extension -ne ".vpulse"

$InputFolder = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\testtimer"  # Change this to your target folder
$ResourceCompilerPath = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe"  # Full path if not in PATH
# ========================

# Validate input folder exists
if (-not (Test-Path -Path $InputFolder -PathType Container)) {
    Write-Error "Input folder does not exist: $InputFolder"
    exit 1
}

# Get all files in the folder, excluding .vmap and .vpk files
$filesToCompile = Get-ChildItem -Path $InputFolder -File -Recurse | Where-Object { $_.Extension -ne ".vmap"  -and $_.Extension -ne ".dmx" -and $_.Extension -ne ".vpk"  -and $_.Extension -ne ".vpulse" -and $_.Extension -ne ".ron"  -and $_.Extension -ne ".png"}

# Build the file list argument
$filesList = $filesToCompile.FullName -join ' '

Write-Host "Found $($filesToCompile.Count) file(s) to compile"
Write-Host ""

foreach ($file in $filesToCompile) {
    Write-Host "Compiling: $($file.FullName)"
    
    & $ResourceCompilerPath -game "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel" -i $file.FullName

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to compile: $($file.FullName) (exit code $LASTEXITCODE)"
        $hasError = $true
    }
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
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\testtimer",
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak10_dir.vpk"
    ) -PassThru

Start-Sleep 1
$wshell.AppActivate($proc.Id)
$wshell.SendKeys("{ENTER}")

 
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak10_dir.vpk" "C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\addons\pak10_dir.vpk" -Force
 

