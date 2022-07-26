# C7
C7 is an in-development 4X strategy game built with Godot and C#, with a historical focus inspired by games such as _Civilization_, _Galactic Civilizations_, and _Humankind_. We aim to create a new game that looks and plays like classic Civ but incorporates features from the best of the genre and our own dreams.

[Homepage](https://c7-game.github.io/)

[GitHub](https://github.com/C7-Game)

[CivFanatics subforum](https://forums.civfanatics.com/forums/civ3-future-development.604/)

[Discord](https://discord.gg/uwxUuWhM89)

## Status

The first preview of the third milestone, "Carthage", is available on the [Releases page](https://github.com/C7-Game/Prototype/releases).  As of this release, you can build cities and units of your choosing among a set of early-game units, and engage in combat with AI civs that are also exanding and exploring. Current information on installation and features can always be found on the [project homepage.](https://c7-game.github.io/)

You can view our progress towards the full "Carthage" milestone from [this project page](https://github.com/C7-Game/Prototype/projects/3).  Key features being planned include improved combat detail, roads, tile visibility/exploration for human players, and save/load.

At this stage, C7 is not yet a fully playable game, but becomes a little bit closer every month.  Take the latest release for a spin, leave some [feedback](https://forums.civfanatics.com/forums/civ3-future-development.604/), and follow the repo for updates.

For now, an installation of _Sid Meier's Civilzation III Complete_ (or _Conquests_) is required for art assets, which you can find on Steam or GOG.

## Contributing

Find the project interesting and want to contribute?  See [Contributing](https://github.com/C7-Game/Prototype/wiki/Contributing) on our Wiki for more information! At the moment, additional developer support is the most-needed asset, but all sorts of help (art, writing, project management, playtesting) could be useful.

To set up a working development environment, see [Developing and Setting Up IDEs](https://github.com/C7-Game/Prototype/wiki/Developing-and-Setting-Up-IDEs).

## What are those subfolders?

- Blast - An Apache-2.0 library for decompressing PKWare DCL, the compression used for Civ3 BIQ and SAV files. Copied from [jamestefler/Blast/Blast](https://github.com/jamestelfer/Blast/tree/3f8c7919c0444c75121f7371c812ec5c2bb9905b/Blast), used by QueryCiv3
- C7 - The core game, which runs on the Godot engine.
- C7Engine - The mechanics of the game, including AI logic.
- C7GameData - Stores native game data, which will be saved to disc when the save feature is merged.
- ConvertCiv3Media - A library dedicated to reading images and animations from Civ3, used by C7 at the time being.
- EngineTests - Tests for logic in the engine.
- QueryCiv3 - A data reader for Civ3 BIQ and SAV files that fetches data based on offsets from labeled section headers, used by C7
