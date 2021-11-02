# QueryCiv3

QueryCiv3 is an assembly designed to read a Civilization III SAV or BIQ file, identify offsets of the 4-character section headers, and retrieve data based on offsets in the data stream.

It is a C# port of my [Go library in c3sat](https://github.com/myjimnelson/c3sat/tree/master/queryciv3).

## Methods

- `Civ3File.SectionOffset(string name, int nth)` - Returns an int offset of the nth (1, 2, 3, etc.) occurrence of the named section header (GAME, TILE, CITY, etc.).
- `Civ3File.ReadInt32(int offset, bool signed = true)` - Returns an int containing the 32-bit integer at the provided offset.
- `Civ3File.ReadInt16(int offset, bool signed = false)` - Returns an int containing the 16-bit integer at the provided offset.
- `Civ3File.ReadByte(int offset)` - Returns an int containing the 8-bit integer at the provided offset.
