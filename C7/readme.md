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

- Fonts/ - Fonts for Godot text
- MainMenu.tscn & MainMenu.cs - The startup scene and main menu
- C7Game.tscn & Game.cs - An early prototype map view accessible from the main menu
