# Game Mode Loading

> Notes on the directory-based OpenCiv3 game mode loading scheme.

## Overview

OpenCiv3 builds a playable scenario ("game mode") out of three things:

- a **ruleset** — the main game data: units, buildings, civs, etc.
- **textures** — the art/texture configuration
- **behaviors** — Lua functions that implement or override gameplay rules

Each one of these components was previously wired up independently, with specific file paths and dedicated loaders.
This early scheme was replaced with the concept of a **game mode directory**.

In this scheme, a game mode is built by taking a base directory and layering zero or more "addon" directories on top of it.
This same layering logic is applied uniformly to the ruleset, textures, and behaviors.

## `GameMode.Config`

```csharp
public class Config {
    // The directory with the base scenario definition
    public string baseModeDir;

    // Directories with addon definitions
    public List<string> addonPaths = [];
}
```

A game mode `Config` is little more than a base directory name plus an ordered list of addon directory names. For example:

```csharp
GameMode.Config basic      = new("civ3");
GameMode.Config standalone = new("civ3", ["standalone"]);
```

`GameMode.Load(gameModesDir, config)` resolves these names against `gameModesDir` (i.e. `Lua/`) and returns a `GameMode` with a fully-composed `ruleset`, `behaviors`, and `textures`.

## Directory layout

A mode directory can contain one to three top-level scripts, plus whatever supporting files those scripts `require`:

| File | Purpose                                          |
|---|--------------------------------------------------|
| `ruleset.json` *or* `ruleset.lua` | game/scenario config, or a transform of the base |
| `textures.lua` | texture/art config                               |
| `behaviors.lua` | gameplay behavior functions                      |

```
Lua/
├── civ3/                      <- base mode
│   ├── ruleset.json
│   ├── textures.lua
│   ├── textures/              <- modules required by textures.lua
│   │   ├── terrain.lua
│   │   ├── cities.lua
│   │   └── ...
│   ├── behaviors.lua
│   └── behaviors/
│       ├── buildings.lua
│       ├── gameplay.lua
│       └── ...
└── standalone/                <- addon, layered on top of civ3/
    ├── ruleset.lua
    ├── textures.lua
    └── utils.lua
```

## How base + addons compose

For each of the three script types, `GameMode.Load` does the same thing:

1. **Load the base.** For the ruleset specifically, a JSON file is tried first (`ruleset.json`); if it isn't there, a `ruleset.lua` is loaded instead and is expected to evaluate directly to a table (not a function). Textures/behaviors always come from `textures.lua` / `behaviors.lua` in the base dir if present.
2. **Layer on each addon, in order.** For every directory in `addonPaths`, if a matching script exists, it's loaded — but as an addon it's expected to return a *function* `table -> table`. That function is called with the result so far, and its return value becomes the new "current" table. Missing addon scripts are simply skipped (e.g. an addon only needs to ship a `ruleset.lua` if it doesn't also need to touch textures).
3. The final composed ruleset table is converted to JSON and deserialized into a `SaveGame`. The final composed textures/behaviors tables are kept as Lua tables and wrapped for the texture loader and `BehaviorEngine` respectively.

This means an addon is just an incremental, composable transform — the same shape whether it's modifying units, art, or behavior:

```lua
-- standalone/ruleset.lua
return function(civ3_ruleset)
  -- ...mutate civ3_ruleset...
  return civ3_ruleset
end
```

## Requiring helper modules

Scripts inside a mode directory resolve `require` relative to that directory:

- `require "utils"` → `<modeDir>/utils.lua`
- `require "textures.terrain"` → `<modeDir>/textures/terrain.lua`
- `require "behaviors.buildings"` → `<modeDir>/behaviors/buildings.lua`

So `textures.lua` and `behaviors.lua` can be split into a same-named subfolder for organization, and any flat helper module (like `utils.lua`) just sits alongside them.

## Save games remember their mode

`SaveGame` has a `GameModeConfig` property. It's set whenever a game is created and persisted to disk along with the rest of the save. When a save is reloaded, the loader callback (`Func<GameMode.Config, BehaviorEngine>`, threaded through `GameParams`) re-runs `GameMode.Load` with that *same* config, so the exact ruleset/texture/behavior stack used to create the game is reconstructed.

## Adding a new mod/mode

> TODO: Implement a code-change free mod loading mechanism

1. Create a new directory under `Lua/`, e.g. `Lua/my_mod/`.
2. Add whichever of `ruleset.lua`, `textures.lua`, `behaviors.lua` you need — each should return a function that takes the table built so far and returns a modified copy.
3. Reference it as an addon on top of an existing base:

   ```csharp
   GameMode.Config myMode = new("civ3", ["my_mod"]);
   // OR
   GameMode.Config myMode = new("civ3", ["standalone", "my_mod"]);
   ```

4. Load it the same way the engine loads `basic`/`standalone`:

   ```csharp
   GameMode mode = GameMode.Load(GamePaths.GameModesDir, myMode);
   ```

Addons are applied in the order listed in `addonPaths`, and each one only needs to provide the scripts it actually wants to change.
