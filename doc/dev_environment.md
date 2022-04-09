# Development Environment

This document provides steps to set up a working C7 development environment.

# Requirements
## .NET SDK
Download [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet).

## Godot
Install the Mono version of [Godot](https://godotengine.org/download). This includes the Godot IDE which is useful for building C7 and in some cases developing UI, but it is not recommended for C# code editing.

# IDEs
There are two free IDEs recommended for C# development. Follow the steps below for setting up a working development environment in these editors and troubleshooting for common issues. Another alternative is to use [JetBrains Rider](https://www.jetbrains.com/rider/download/), a commercial IDE for C# development. There is a
[CFC thread](https://forums.civfanatics.com/threads/dev-jetbrains-rider-impressions.675190/) on Rider that may provide additional information on setting it up.

## Visual Studio Code
Follow the [official guide](https://code.visualstudio.com/docs/setup/setup-overview#_cross-platform) to install Visual Studio Code for your platform.

Next, install the following plugins from the marketplace:
1. [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) - this provides syntax highlighting, IntelliSense, find references, and other nice IDE-like features for C#.
2. [C# Tools for Godot](https://marketplace.visualstudio.com/items?itemName=neikeq.godot-csharp-vscode) - this enables launching C7 from VS Code and debugging

Finally, set up code formatting. For Visual Studio Code, formatting is done with OmniSharp and configured by the `.editorconfig` file. In order to configure OmniSharp, do the following:
1. In your home directory, create `~/.omnisharp/omnisharp.json` with the following contents
```
{
  "RoslynExtensionsOptions": {
    "enableAnalyzersSupport": true,
  },
  "FormattingOptions": {
    "enableEditorConfigSupport": true,
  }
}
```
2. Add the following options to your VS Code `settings.json` (either workspace settings or your user level settings)
```
"omnisharp.enableMsBuildLoadProjectsOnDemand": true,
"omnisharp.enableEditorConfigSupport": true,
"omnisharp.enableRoslynAnalyzers": true,
```
3. Optionally but recommended, add this option to your VS Code `settings.json` to enable formatting on save
```
"[csharp]": {
  "editor.formatOnSave": true,
}
```

### Troubleshooting
- Linting or IntelliSense are completely broken
  - try adding `"omnisharp.useGlobalMono": "always"` to `settings.json`
  - you may need to rebuild the entire project through the Godot IDE

## Visual Studio 2019 Community Edition
It's important to use Visual Studio 2019, as you cannot run/debug Godot from Visual Studio 2022 yet, and 2019 was the first version to be supported.

The Visual Studio solution file is already aware of the `.editorconfig` file, so no further setup is required.
