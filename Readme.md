Vibecoded most of the thing in there.

I only made this to run only windows64 bits, so it won't use proper .exes if you don't go for windows
Also, It's probably not functionnal at all on your side, I barely checked what happens in there ... ! 

Steps : 
- Run ConsoleApp1 (need to rename that) dontet (dotnet run --project .\ConsoleApp1\ConsoleApp1.csproj)
- It will download from google drive the reduced_csdk12.zip
- Delete any existing files inside Reduced_CSDK_12 folder (!) 
- Extract the newly donwloaded zip
- Downaload and run DepotDownloader (getting the 2 depot indicated inside https://deadlockmodding.pages.dev/modding-tools/csdk-12) ()
    - Both depot will spawn a QR code you need to scan to connect to steam and be allowed to download the files.
    - First depot is weightless. The second one will take few seconds to few minutes to download. (Full deadlock game)
- Download and extract Source 2 viewer CLI (cli-windows-x64.zip)
- Extract (not decompile) the pak01 from depotDownloader deadlock downloaded files and place them inside Reduced_CSDK_12 folder
- Re-extract the zip and override files like explained inside  https://deadlockmodding.pages.dev/modding-tools/csdk-12
- French Sauce : Swap Qwerty to Azerty for Hammer controls (can't modify them from the UI)

How to check if everything worked out : You should now be able to run csdkcfg.exe, take any existing template addon, and run bin_server. If the game pop-up and you can control your character, it's a bingo.
Otherwise, good luck finding out what broke.

VDataEditor project is a non-generic purely local replacer of "heroes.vdata_c" file (using structure from https://github.com/ValveResourceFormat/ValveResourceFormat/tree/master/ValveResourceFormat)

compiletemplatemap.ps1 is a script to help me move around files and improve my workflow while testing scripting for maps. Do not reuse it as is, but you can find usage CSDKCfgVPK & resourcecompiler as well as the map compiler CLI

I will never thank enough Artemon121 on discord !