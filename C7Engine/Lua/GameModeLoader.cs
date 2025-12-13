using System;
using System.IO;
using System.Collections.Generic;
using Serilog;
using C7GameData.Save;
using MoonSharp.Interpreter;

namespace C7Engine.Lua;

public class GameModeConfig {
	public string baseModePath;
	public List<string> addonPaths = [];

	public GameModeConfig(string baseModePath, List<string> addonPaths = null) {
		this.baseModePath = baseModePath;
		this.addonPaths = addonPaths ?? [];
	}
}

public class GameModeLoader {
	private static ILogger log = Log.ForContext<GameModeLoader>();

	public static SaveGame Load(string gameModesDir, GameModeConfig config) {
		Script lua = new();
		JsonConverter converter = new (lua);

		string baseModePath = config.baseModePath;

		string fullBaseModePath = Path.Combine(gameModesDir, baseModePath);
		log.Information("Loading base game mode file: {baseModePath}", fullBaseModePath);

		DynValue current = Path.GetExtension(fullBaseModePath).ToUpper() switch {
			".JSON" => converter.Decode(File.ReadAllText(fullBaseModePath)),
			".LUA" => lua.SafeDoFile(fullBaseModePath),
			_ => throw new InvalidOperationException("Invalid base mode file format")
		};

		foreach (string addonPath in config.addonPaths) {
			log.Information("Loading addon: {addonPath}", addonPath);

			DynValue addon = lua.SafeDoFile(Path.Combine(gameModesDir, addonPath));

			if (addon.Type != DataType.Function)
				throw new InvalidOperationException(
					$"Addon '{addonPath}' must return a function"
				);

			current = lua.SafeCall(addon.Function, current);
		}

		string json = converter.Encode(current);

		return SaveGame.LoadFromJSON(json);
	}
}
