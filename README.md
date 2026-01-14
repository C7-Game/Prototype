# OpenCiv3

**OpenCiv3** (formerly known by the codename "C7") is an open-source, mod-oriented remake of _Civilization III_ by the fan community built with Godot and C#, with capabilities inspired by other 4X games and lessons learned in Civ3 modding. We aim to create a new game that looks and plays like Civ3 but incorporates features from the best of the genre and our own dreams.

***OpenCiv3 is under active development and currently in an early pre-alpha state.*** It is a rudimentary playable game but lacking many mechanics and late-game content, and errors are likely. Keep up with our development for the latest updates and opportunities to contribute!

- [OpenCiv3.org](https://www.openciv3.org)
- [CivFanatics subforum](https://forums.civfanatics.com/forums/civ3-future-development.604/)
- [Discord](https://discord.gg/uwxUuWhM89)

> [!NOTE]
> OpenCiv3 is not affiliated with civfanatics.com,
> Firaxis Games, BreakAway Games, Hasbro Interactive, Infogrames Interactive,
> Atari Interactive, or Take-Two Interactive Software.
> All trademarks are property of their respective owners.

## Status

The latest stable version can be downloaded from the [Releases page](releases). Current information on installation and features can always be found on the [project homepage.](https://www.openciv3.org/)

OpenCiv3 is in a pre-alpha state. It is a rudimentary playable game but lacking many mechanics and late-game content, and errors are likely. Keep up with our development for the latest updates and opportunities to contribute!

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
