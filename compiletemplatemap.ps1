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

C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads  -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumping_map_map\maps\template_map9.vmap" -noassert -world -nolightmaps -phys -nav -retail -breakpad -nop4 -outroot "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput"

Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\VDataEditor\heroes_modified.vdata" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumping_map_script\scripts\heroes.vdata" -Force

New-Item -ItemType Directory -Force -Path "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script\maps"
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\citadel_addons\jumping_map_map\maps\template_map9.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script\maps" -Force -Recurse

C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\jumping_map_script\*" -r 

C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin\win64\CSDKCfgVPK.exe C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\jumping_map_script C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk