# Development Environment

This document provides steps to set up a working OpenCiv3 development environment. Some source paths still use the older `C7` name; the Godot project and C# solution are in the `C7` directory.

# Requirements
## .NET SDK
Download the [.NET SDK](https://dotnet.microsoft.com/en-us/download), version 8.0 or higher. The projects currently target `net8.0`.

## Godot
Install the [.NET version of Godot 4.4](https://godotengine.org/download). The Godot editor is useful for importing the project, running the game, wiring scene events, and creating UI, but a full C# IDE is still recommended for code editing.

The first time you check out OpenCiv3, import `C7/project.godot` from the Godot Project Manager. Godot will build the C# project before running it.

# IDEs
Most OpenCiv3 development is C#, so use Visual Studio Code or [JetBrains Rider](https://www.jetbrains.com/rider/download/) alongside the Godot editor. Visual Studio 2019 is no longer a supported setup for current Godot 4/.NET development. There is a [CFC thread](https://forums.civfanatics.com/threads/dev-jetbrains-rider-impressions.675190/) on Rider that may provide additional information on setting it up.

## Visual Studio Code
Follow the [official guide](https://code.visualstudio.com/docs/setup/setup-overview#_cross-platform) to install Visual Studio Code for your platform.

Next, install the following extensions from the marketplace:
1. [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) - this provides syntax highlighting, IntelliSense, find references, and other IDE-like features for C#.
2. [C# Tools for Godot](https://marketplace.visualstudio.com/items?itemName=neikeq.godot-csharp-vscode) - this helps with launching and debugging Godot C# projects from VS Code.

Finally, set up code formatting. The repository uses `.editorconfig`; VS Code can honor it through the C# extension settings. Add the following options to your VS Code `settings.json` if they are not already enabled:

```json
"omnisharp.enableMsBuildLoadProjectsOnDemand": true,
"omnisharp.enableEditorConfigSupport": true,
"omnisharp.enableRoslynAnalyzers": true,
"[csharp]": {
  "editor.formatOnSave": true
}
```

### Troubleshooting
- If linting or IntelliSense are completely broken, rebuild the project through the Godot editor and restart VS Code.
- If command-line builds fail before Godot has imported the project, open `C7/project.godot` in the Godot editor once, let it finish the initial build, and try again.

## JetBrains Rider
Rider has strong C# support and good Godot integration. Install Rider, add the Godot Support plugin, and open `C7/C7.sln`. Use a Godot 4 `.NET Executable` run configuration that points at your local Godot executable and the `C7` project directory.

# Build and Test
From the repository root:

```bash
dotnet build C7/C7.sln
dotnet test C7/C7.sln --logger "console;verbosity=detailed"
```

Some tests load Civilization III assets. Set `CIV3_HOME` to the top-level Civilization III install folder if you want to run those locally.
