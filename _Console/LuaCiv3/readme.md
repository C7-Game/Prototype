- loads & runs process.lua
- pass it paths to bic or sav files, and process_sav or process_bic will be called in lua for each
- show_results called in lua to let it know it is done

#### Build & Use

- `nuget restore LuaCiv3.csproj`
- `msbuild`
- Modify process.lua as desired
- `find /path/to/saves -iname '*.sav' -print0 | xargs -0 mono bin/Debug/net472/LuaCiv3.exe`
- `find /path/to/bics -iname '*.bi*' -print0 | xargs -0 mono bin/Debug/net472/LuaCiv3.exe`
