# C7
C7 is an in-development 4X strategy game with a historical focus inspired by games such as _Civilization_, _Galactic Civilizations_, and _Humankind_.

## Status

The "Aztec" release is an early proof-of-concept for early engine mechanics, and is available on the Releases page.

We will be making another release in March 2022 that features improved mechanics around settling new cities, animation of units, and prototype AI opponents, among other improvements.

At this stage, C7 is not yet a fully playable game, but becomes a little bit closer every month.  Take the latest release for a spin, leave some [feedback](https://forums.civfanatics.com/forums/civ3-future-development.604/), and star the repo to remember to try it again in a few months.

An installation of Sid Meier's Civilzation III Complete (or Conquests) is required for art assets.

## Contributing

Find the project interesting enough you'd like to contribute?  See the [Contributing Page](https://github.com/C7-Game/Prototype/wiki/Contributing) on our Wiki for more information!

At the moment, additional developer bandwidth is probably the most-needed asset, but all sorts of help (art, writing, project management, sheep-herding) could be useful.

## What is those subfolders?

- Blast - An Apache-2.0 library for decompressing PKWare DCL, the compression used for Civ3 BIQ and SAV files. Copied from [jamestefler/Blast/Blast](https://github.com/jamestelfer/Blast/tree/3f8c7919c0444c75121f7371c812ec5c2bb9905b/Blast), used by QueryCiv3
- C7 - The core game, which runs on the Godot engine.
- C7Engine - The mechanics of the game, including AI logic.
- ConvertCiv3Media - A library dedicated to reading images and animations from Civ3, used by C7 at the time being.
- EngineTests - Tests for logic in the engine.
- QueryCiv3 - A data reader for Civ3 BIQ and SAV files that fetches data based on offsets from labeled section headers, used by C7
