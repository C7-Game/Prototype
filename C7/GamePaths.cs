public static class GamePaths {
	// This is the 'static map' used in lieu of terrain generation
	public static string DefaultGamePath {
		get =>
			C7Engine.C7Settings.UseStandaloneMode() ? "./Text/c7-static-map-save-standalone.json" : "./Text/c7-static-map-save.json";
	}

	public const string LuaRulesDir = "./Lua/rules/";
	public const string TextureConfigsDir = "./Lua/texture_configs/";

	public const string ModernGraphicsConfig = "c7.lua";
	public const string ClassicGraphicsConfig = "civ3.lua";

	// For now this needs to get passed to QueryCiv3 when importing.
	public static string DefaultBicPath { get => Util.GetCiv3Path() + "/Conquests/conquests.biq"; }
}
