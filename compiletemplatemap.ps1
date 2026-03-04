<#compile map#>

C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads  -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumping_map_map\maps\template_map9.vmap" -noassert -world -nolightmaps -phys -nav -retail -breakpad -nop4 -outroot "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput"
<#
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\citadel_addons\test\maps\template_map9.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel" -Force
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\citadel_addons\test\maps\template_map9.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\maps" -Force
#>

<#move map to game/citadel_addons/<folder>#>
New-Item -ItemType Directory -Force -Path "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script\maps"
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\citadel_addons\jumping_map_map\maps\template_map9.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script\maps" -Force -Recurse

<#compile all script files#>
C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumping_map_script\*" -r 

<#package into VPK#>
C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin\win64\CSDKCfgVPK.exe C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk


