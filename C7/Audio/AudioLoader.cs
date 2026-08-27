using System;
using Godot;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;
using Script = MoonSharp.Interpreter.Script;
using C7Engine.Lua;

public static class AudioLoader {

	private static Script lua;
	private static Table audioConfig;

	private static Dictionary<string, AudioStream> configKeyCache = [];
	private static Dictionary<(string configKey, object obj), AudioStream> objectMappingCache = [];

	static AudioLoader() {
		// We need to register the "Type" type to be able to inspect
		// the types of C# objects in the Lua code
		UserData.RegisterType<Type>();

		// Initialize the TextureLoader when running in the editor
		// In game it is done by GlobalSingleton, but it's not accessible in the editor
		if (Engine.IsEditorHint()) {
			GameMode gameMode = GameMode.Load(GamePaths.GameModesDir, GamePaths.basic);
			var (script, table) = gameMode.audio;
			AudioLoader.SetConfig(script, table);
		}
	}

	public static void SetConfig(Script lua, Table audioConfig) {
		ClearCache();

		AudioLoader.lua = lua;
		AudioLoader.audioConfig = audioConfig;
	}

	/// Returns a texture based on the config key.
	/// The config key should be a string separated by dots, representing the path through the
	/// configuration hierarchy (e.g., "icons.plus").
	public static AudioStream Load(string configKey) {
		if (configKeyCache.TryGetValue(configKey, out AudioStream cachedTexture))
			return cachedTexture;

		object entry = GetEntryByPath(configKey);
		if (entry == null)
			throw new Exception($"Texture config not found for key: {configKey}");

		AudioStream texture = LoadFromLuaObject(entry);

		configKeyCache[configKey] = texture;

		return texture;
	}

	/// Returns an audio stream based on the config key and a C# object.
	///
	/// This overload uses the "map_object_to_audio_stream" function in the
	/// config entry to dynamically determine which audio stream to load
	/// based on the provided object's properties.
	///
	/// This method optionally allows to cache the resulting texture,
	/// using (configKey, obj) as key. Note that caching shouldn't
	/// be used for objects whose properties can change.
	///
	/// Note that the type of the object passed to the method should
	/// be registered as Moonsharp userdata.
	public static AudioStream Load(string configKey, object obj, bool useCache = false) {
		var cacheKey = (configKey, obj);

		if (useCache && objectMappingCache.TryGetValue(cacheKey, out AudioStream cachedTexture))
			return cachedTexture;

		object entry = GetEntryByPath(configKey);
		if (entry is not Table table)
			throw new Exception($"Table expected for key: {configKey}");

		if (table["map_object_to_sprite"] is not Closure func)
			throw new Exception("Custom mapping function expected");

		object result = lua.SafeCall(func, table, DynValue.FromObject(lua, obj)).ToObject();

		AudioStream audioStream = LoadFromLuaObject(result);

		if (useCache)
			objectMappingCache[cacheKey] = audioStream;

		return audioStream;
	}

	private static AudioStream LoadFromLuaObject(object entry) {
		return LoadFromPath(ParsePath(entry));
	}

	private static string ParsePath(object entry) {
		if (entry is string simplePath) {
			return simplePath;
		}

		throw new ArgumentException($"Invalid audio config format: {entry?.GetType().Name ?? "null"}");
	}

	private static AudioStream LoadFromPath(string path) {
		string ext = Path.GetExtension(path).ToLowerInvariant();

		return ext switch {
			".wav" => Util.LoadCiv3WAVFromDisk(path),
			".mp3" => Util.LoadCiv3Mp3FromDisk(path),
			_ => throw new FormatException($"Unknown audio format: {path}"),
		};
	}

	private static object GetEntryByPath(string configKey) {
		string[] parts = configKey.Split('.');
		object current = audioConfig;

		foreach (string part in parts) {
			if (current is Table table && table[part] != null) {
				current = table[part];
			} else {
				return null;
			}
		}

		return current;
	}

	public static void ClearCache() {
		configKeyCache.Clear();
		objectMappingCache.Clear();
	}
}
