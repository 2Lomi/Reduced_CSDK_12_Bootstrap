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
So I would need to either find all files inside the folders that are not map and compile them individually. Or use the "filelist" parameter. But damn I'm bored. I will do it whonelanenever this becomes a chore. 
Which should not happen (?)
#>



<#backup#>
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\onelane_backup1.vmap" "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\onelane_backup2.vmap"
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\onelane.vmap" "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput\onelane_backup1.vmap"
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\onelanebackup\maps\onelane.vmap" "C:\Repos\Reduced_CSDK_12_Bootstrap\mapOutput"

 

        
C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\bin_cs2\win64\resourcecompiler.exe -threads -fshallow -maxtextureres 256 -dxlevel 110 -quiet -unbufferedio -i "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\onelanebackup\maps\onelane.vmap" -noassert -world -nolightmaps -phys -nav -retail -breakpad -nop4 -outroot  "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game"

Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\onelanebackup\maps\onelane.vpk" "C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\maps" -Force
Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel_addons\onelanebackup\maps\onelane.vpk" "C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel" -Force




# Copy-Item "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\game\citadel\addons\pak01_dir.vpk" "C:\Repos\Reduced_CSDK_12_Bootstrap\ConsoleApp1\Reduced_CSDK_12\content\citadel_addons\onelanebackup\scripts\heroes.vdata" -Force


#func_nav_markup 
    #GROUND & AIR & WALKABLE SEED & USE REFERENCE POSITION
#point_nav_walkable

#TEST 0
# ALL WORKING FINE
#TEST 1
# DELETE ALL TROOPERS
#TEST 2 
# DELETE ALL ZIPLINES
#TEST 3
# DELETE LOGIC
#TEST 4
# DELETE ZAP
#TEST 5
# DELETE CATAPULT
