## Path to Civlization III Files

If running on Windows 64-bit, C7 will grab the install folder from the registry. On Mac or Linux, copy over the files from your Windows installation of Civ III and set the environment variable `CIV3_HOME` to the top-level Civ III install folder. e.g. `export CIV3_HOME=/path/to/civ3` and then run Godot Mono from that session.

- If you have the Steam version of Civ III, the top-level folder is called "Sid Meier's Civilization III Complete"

### Setting the environment on MacOS

If you're having trouble setting the environment variable on Mac to apply to all apps, you can use the following workaround to get Godot to launch with it:

1. Create a folder in Applications called "Godot_with_env.app"
2. In that folder create a script called "Godot_with_env":
```bash
#!/bin/bash
export CIV3_HOME="/path/to/civ3"
open /Applications/Godot_mono.app
```
3. `chmod +x Godot_with_env`
4. [Copy over the app icon](https://9to5mac.com/2021/11/08/change-mac-icons/) if you want
5. Launch Godot by using the Godot_with_env app instead of Godot_mono

## What is that?

- Experiments/ - Experimental or prototype scenes and/or scripts
- Fonts/ - Fonts for Godot text
- LegacyMap/ - A script to display a map using native Civ3 information and media. It is intended to be separate from game logic & data.
- OldLegacyMap/ - The previous iteration of LegacyMap currently still around as some of the logic may be needed for LegacyMap to display other map features
- TempTiles/ - Since there is no C7 native map/tile format yet, TempTiles will read a SAV file and generate tile data for LegacyMap to use
- UtilScenes/ - A collection of scenes useful for dev time activities but not necessarily for game inclusion
- MainMenu.tscn & MainMenu.cs - The startup scene and main menu
- C7Game.tscn & Game.cs - An early prototype map view accessible from the main menu
