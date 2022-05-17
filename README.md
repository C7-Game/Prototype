# C7
C7 is an in-development 4X strategy game with a historical focus inspired by games such as _Civilization_, _Galactic Civilizations_, and _Humankind_.

## Status

The first preview of the third milestone, "Carthage", is available on the [Releases page](https://github.com/C7-Game/Prototype/releases).  As of this release, you can build cities and units of your choosing among a set of early-game units, and engage in combat with AI civs that are also exanding and exploring.

You can view our progress towards the full "Carthage" milestone from [this project page](https://github.com/C7-Game/Prototype/projects/3).  Key features being planned include improved combat detail, roads, tile visibility/exploration for human players, and save/load.

At this stage, C7 is not yet a fully playable game, but becomes a little bit closer every month.  Take the latest release for a spin, leave some [feedback](https://forums.civfanatics.com/forums/civ3-future-development.604/), and star the repo to remember to try it again in a few months.

An installation of Sid Meier's Civilzation III Complete (or Conquests) is required for art assets.

## Contributing

Find the project interesting enough you'd like to contribute?  See the [Contributing Page](https://github.com/C7-Game/Prototype/wiki/Contributing) on our Wiki for more information!

To set up a working development environment, check out [the docs](./doc/dev_environment.md).

At the moment, additional developer bandwidth is probably the most-needed asset, but all sorts of help (art, writing, project management, sheep-herding) could be useful.

## What is those subfolders?

- Blast - An Apache-2.0 library for decompressing PKWare DCL, the compression used for Civ3 BIQ and SAV files. Copied from [jamestefler/Blast/Blast](https://github.com/jamestelfer/Blast/tree/3f8c7919c0444c75121f7371c812ec5c2bb9905b/Blast), used by QueryCiv3
- C7 - The core game, which runs on the Godot engine.
- C7Engine - The mechanics of the game, including AI logic.
- C7GameData - Stores native game data, which will be saved to disc when the save feature is merged.
- ConvertCiv3Media - A library dedicated to reading images and animations from Civ3, used by C7 at the time being.
- EngineTests - Tests for logic in the engine.
- QueryCiv3 - A data reader for Civ3 BIQ and SAV files that fetches data based on offsets from labeled section headers, used by C7
