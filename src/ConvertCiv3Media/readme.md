# ReadCiv3Data.ConvertCiv3Media

A class library assembly to convert from Civ3 media formats to palette and image byte arrays that should be easily translatable into other image formats. e.g. png, Godot texture, Unity texture.

## Status

Under Construction.

The PCX reader works fairly well, but there is no accounting for civ colors (e.g. popHeads).

The Flic reader works well on units.

## Objects

### Pcx

Example: `Pcx MyImage = new Pcx(@"path/to/file.pcx");`

#### Properties

- `int Width` - Width of the image
- `int Height` - Height of the image
- `byte[,] Palette` - A 256x3 byte array of 256 colors in red, green, blue order
- `byte[] Image` - A flat byte array of the image data. Each byte is a pixel, the value of which is the index of the Palette indicating the color

#### Methods

- Can pass a path string to the constructor and it will run Load() on creation
- `void Load(string path)` - Loads and decodes the PCX file at the path location and populates the object properties

### Flic

Example: `Flic MyImage = new Flic(@"path/to/file.flc");`

#### Properties

- `int Width` - Width of the images
- `int Height` - Height of the images
- `byte[,] Palette` - A 256x3 byte array of 256 colors in red, green, blue order
- `byte[,][] Images` - An array of animations, images, each of which is a byte array of the image data. Each byte is a pixel, the value of which is the index of the Palette indicating the color

#### Methods

- Can pass a path string to the constructor and it will run Load() on creation
- `void Load(string path)` - Loads and decodes the FLC file at the path location and populates the object properties

### Civ3UnitSprite

This reads the unit INI file and loads a Flic[] property so that all of a unit's animations are in one object. It is intended to be inherited by Godot or Unity objects that translate the byte arrays into the native image formats.

## Build

\* Note: These build instructions are for a shared dotnet library.
For C7 game, just use Godot Mono to build the C7 project.

- Uses dotnet core 3.1 cli
- CD to this folder and `dotnet build`
- Files will be in obj/Debug/
- To use dll in another project, cd into that projects directory and `dotnet add reference path/to/ReadCivData.ConvertCiv3Media` and reference the namespace `ReadCivData.ConvertCiv3Media` in `using` or as a prefix to the functions.

## Known Issues

- Does not account for endianness; if compiled on a big endian system, number formats may be wrong
- Does not yet handle unit smoke colors
- Unit color parameterization not yet fully plumbed
- Only stores one palette per Flic, but the spec allows for palette changes beween frames, and Civ3 reportedly has some of that, although I haven't seen it yet
- No time/speed is currently extracted from Flic
- Only Flic and PCX formats in use by Civ3 are implemented; not for general-purpose use
- PCX reader only handles 8-bit palette PCX files
- Flic reader uses custom header fields from Civ3
