This was originally intended to be extended into a general-purpose Lua script reader to have access to QueryCiv3, but the immediate need was to export select Civ3 map data to JSON for temp use in C7-Game/Prototype. After I got Lua working and printing out the info I realized it's simpler just to skip Lua. This is now both acting as a tester for the import Civ3 save function of C7GameData and to set the default/hard-coded map for C7Game.

The script file reference, save file path, and civ3 path are hard-coded for now, and it's currently unclear how long we'll need this, but it might be nice to parameterize this.

#### Build & Use

- Civ3 home is currently hard, coded, and so is relative /Conquests/Saves/for-c7-seed-1234567.SAV 
- `nuget restore BuildDevSave.csproj`
- `msbuild`
- `mono bin/Debug/net472/BuildDevSave.exe`
