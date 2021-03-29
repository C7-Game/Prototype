## Path to Civlization III Files

If running on Windows 64-bit, C7 will grab the install folder from the registry. Otherwise, set the environment variable `CIV3_HOME` to the top-level Civ III install folder. e.g. `export CIV3_HOME=/path/to/civ3` and then run Godot Mono from that session.

## What is that?

- LegacyMap/ - A script to display a map using native Civ3 information and media. It is intended to be separate from game logic & data.
- OldLegacyMap/ - The previous iteration of LegacyMap currently still around as some of the logic may be needed for LegacyMap to display other map features
- TempTiles/ - Since there is no C7 native map/tile format yet, TempTiles will read a SAV file and generate tile data for LegacyMap to use
- UtilScenes/ - A collection of scenes useful for dev time activities but not necessarily for game inclusion
- MainMenu.tscn & MainMenu.cs - The startup scene and main menu
- C7Game.tscn & Game.cs - An early prototype map view accessible from the main menu