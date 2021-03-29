# Prototype
Prototyping C7 features and game engine integration

## Status

Exploratory programming. Current activity includes transforming Civ3 data and media files into formats usable in modern software.

The Development branch should work but not be polished, or even sanely usable by a non-developer for any specific purpose.

An installation of Sid Meier's Civilzation III Complete (or Conquests) is required.

## What is that?

- Blast - An Apache-2.0 library for decompressing PKWare DCL, the compression used for Civ3 BIQ and SAV files. Copied from [jamestefler/Blast/Blast](https://github.com/jamestelfer/Blast/tree/3f8c7919c0444c75121f7371c812ec5c2bb9905b/Blast), used by QueryCiv3
- C7 - The deliverable of this repo, a prototype game in Godot Mono project form
- ConvertCiv3Media - A library dedicated to reading images and animations from Civ3, used by C7
- QueryCiv3 - A data reader for Civ3 BIQ and SAV files that fetches data based on offsets from labeled section headers, used by C7
