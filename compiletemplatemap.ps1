<#

Objective is simple : 

PREEXISTING CONTENT FOLDERS 
jumping_map_map (map) | jumping_map_script (vdata/vpulse)


>Compile folder 1 map (.vpk)
>Move modified heroes.vdata from VDataEditor Project to jumping_map_script CONTENT
>Move compiled vpk map to jumping_map_script GAME folder
>Compile jumping_map_script (vdata/vpulse)
>Package jumping_map_script (containing vdata_c compiled & map vpk)  directly into game/addons folder


WHY 2 FOLDERS : For me in the future :
bin_cs2\win64\resourcecompiler.exe -r parameter will FIND the vmap file and will try to compile it. But it can't.
Unfortunately, the "skiptype" parameter does NOT work for vmap while using -r.
So I would need to either find all files inside the folders that are not map and compile them individually. Or use the "filelist" parameter. But damn I'm bored. I will do it whnever this becomes a chore. 
Which should not happen (?)
#>



<#backup#>
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap\maps\template_map9.vmap" "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput"

C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap\maps\template_map9.vmap" -noassert -world -nolightmaps -phys -nav -retail -breakpad -nop4 -outroot  "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game"


Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\heroes_modified.vdata" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap\scripts\heroes.vdata" -Force




# PowerShell script to compile resource files using resourcecompiler.exe
# Excludes .vmap and .vpk files

# ===== CONFIGURATION =====
$InputFolder = "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap"  # Change this to your target folder
$ResourceCompilerPath = " C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe"  # Full path if not in PATH
# ========================

# Validate input folder exists
if (-not (Test-Path -Path $InputFolder -PathType Container)) {
    Write-Error "Input folder does not exist: $InputFolder"
    exit 1
}

# Get all files in the folder, excluding .vmap and .vpk files
$filesToCompile = Get-ChildItem -Path $InputFolder -File -Recurse | 
    Where-Object { $_.Extension -ne ".vmap" -and $_.Extension -ne ".vpk"  -and $_.Extension -ne ".vpulse" -and $_.Extension -ne ".ron" }

if ($filesToCompile.Count -eq 0) {
    Write-Warning "No files found to compile (excluding .vmap and .vpk files)"
    exit 0
}
 
# Build the file list argument
$filesList = $filesToCompile.FullName -join ' '

Write-Host "Found $($filesToCompile.Count) file(s) to compile"
Write-Host "Executing: $ResourceCompilerPath -i $filesList"
Write-Host ""

# Execute resourcecompiler with the files
& $ResourceCompilerPath -game "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel" -i $filesList 

if ($LASTEXITCODE -ne 0) {
    Write-Error "resourcecompiler.exe exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Compilation completed successfully!"
  
$wshell = New-Object -ComObject WScript.Shell

$proc = Start-Process "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin\win64\CSDKCfgVPK.exe" `
    -ArgumentList @(
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumpmap",
        "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk"
    ) -PassThru

Start-Sleep 1
$wshell.AppActivate($proc.Id)
$wshell.SendKeys("{ENTER}")

 
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk" "C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\addons" -Force


Get-Process -Name "deadlock" -ErrorAction SilentlyContinue | Stop-Process -Force


# Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumpmap\scripts\heroes.vdata" -Force


