using C7Engine.Lua;

public static class GamePaths {
	// This is the 'static map' used in lieu of terrain generation
	public static GameModeConfig GameMode {
		get => C7Engine.C7Settings.UseStandaloneMode() ? standalone : basic;
	}

	public static GameModeConfig basic = new("base-ruleset.json");
	public static GameModeConfig standalone = new("base-ruleset.json", ["standalone.lua"]);

	public const string LuaRulesDir = "./Lua/rules/";
	public const string TextureConfigsDir = "./Lua/texture_configs/";
	public const string GameModesDir = "./Lua/game_modes/";

	public const string ModernGraphicsConfig = "c7.lua";
	public const string ClassicGraphicsConfig = "civ3.lua";

	// For now this needs to get passed to QueryCiv3 when importing.
	public static string DefaultBicPath { get => Util.GetCiv3Path() + "/Conquests/conquests.biq"; }
}
