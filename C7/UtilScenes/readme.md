# UtilScenes

These aren't necessarily for the game. This is especially to help with dev tasks.

## ViewPcxFlc

I wanted to browse Pcx files and realized I didn't have an easy way to do so, but I do have Godot, a PCX library, and a Godot file browser working. So this is to open and view Pcx files. It opens in the Civ3 directory by default.

It needs some intelligence on positioning, but hey it works. FLC support will be added later.

Just open the scene in Godot and run it. It is not hooked up to the game menu.

## TempTiles

While working on LegacyMap I need a set of 'native' tiles to pull data from. We have no native structures yet, so this is a temporary and exploratory scene and code to read a legacy SAV file and provide data for the LegacyMap renderer.

Mock city and unit data may be included, too, as their info will be needed to draw a game-ready map.

BIQ data will also need to be read and probably exposed here.

This has been moved to UtilScenes as the debug offest will show the offset's value on the map tiles.
