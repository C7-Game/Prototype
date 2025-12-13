using System;
using System.Reflection;
using System.Linq;
using MoonSharp.Interpreter;
using Serilog;
using System.IO;
using MoonSharp.Interpreter.Loaders;

namespace C7Engine.Lua;

/// A rules engine that loads game behaviors defined in a Lua script.
///
/// Works with Lua scripts that return a nested table where the leaves
/// are functions.
///
/// The intended use case is during the game initialization (e.g.,
/// when loading a JSON save), to transform given Lua function paths
/// into C# delegates.
///
/// When initializing the Lua state, this class registers all public
/// types in the C7GameData namespace as Moonsharp userdata, which
/// exposes them to the scripting engine.
///
/// The engine registers the following Lua globals:
/// - GAME_DATA(): Function returning the current game data instance.
/// - ENUMS:  Table containing definitions of public enums from the C7GameData namespace.
///   Note that nested enums are registered under a name concatenating the outer class and the enum names (e.g., Tile_YieldType)
public class RulesEngine {
	static ILogger log = Log.ForContext<RulesEngine>();
	Script script = new();
	Table rules;

	static RulesEngine() {
		RegisterTypes();
	}

	/// Initializes the Lua state.
	///
	/// Accepts the path to the directory containing Lua scripts,
	/// and the name of the script that should return a rules table.
	public void Initialize(string luaRulesDir, string rulesScript) {
		if (rules != null)
			throw new InvalidOperationException("Engine already initialized");

		RegisterEnums();
		RegisterGlobals();

		script.Options.ScriptLoader = new FileSystemScriptLoader {
			ModulePaths = [
				Path.Combine(luaRulesDir, "?.lua"),
				Path.Combine(luaRulesDir, "*", "?.lua")
			]
		};

		string fullScriptPath = Path.Combine(luaRulesDir, rulesScript);
		log.Information("Loading Lua rules from file: {filePath}", fullScriptPath);

		DynValue res = script.SafeDoFile(fullScriptPath);
		rules = res.Table;
	}

	/// Loads a Lua function from a rules table by a given path and
	/// transforms it into C# delegate of the given generic type.
	///
	/// The path to the function should be a dot-separated string
	/// representing the path through the rules table
	/// (e.g. "building.production_rules.must_be_coastal")
	public T ImportFunc<T>(string functionPath) where T : Delegate {
		if (script == null || rules == null)
			throw new InvalidOperationException("Engine is not initialized");
		if (functionPath == null)
			throw new InvalidOperationException("Non-null function path expected");

		Closure closure = ResolveFunctionPath(functionPath);
		Delegate del = DelegateConverter.CreateDelegate(script, closure, typeof(T));
		return (T)(object)del;
	}

	Closure ResolveFunctionPath(string functionPath) {
		string[] parts = functionPath.Split('.');
		Table current = rules;

		for (int i = 0; i < parts.Length; i++) {
			string part = parts[i];
			DynValue value = current.Get(part);

			if (value.Type == DataType.Function && i == parts.Length - 1)
				return value.Function;

			if (value.Type == DataType.Table)
				current = value.Table;
			else
				throw new ArgumentException($"Unexpected type at '{part}': '{value.Type}'");
		}

		throw new ArgumentException($"Function path '{functionPath}' did not resolve to a function.");
	}

	static void RegisterTypes() {
		var types = Assembly.GetExecutingAssembly()
							.GetTypes()
							.Where(t =>
									(t.IsPublic || t.IsNestedPublic)
									&& t.Namespace == "C7GameData");

		foreach (var type in types) {
			log.Debug("Registering type: {typeName}", type.FullName);
			UserData.RegisterType(type);
		}
	}

	void RegisterEnums() {
		var enumTypes = Assembly.GetExecutingAssembly()
							.GetTypes()
							.Where(t =>
								t.IsEnum &&
								(t.IsPublic || t.IsNestedPublic) &&
								t.Namespace == "C7GameData");

		Table enumTable = new(script);

		foreach (var enumType in enumTypes) {
			// For nested enums, concatenate the outer class name with the enum name
			string registrationName = enumType.DeclaringType != null
			? $"{enumType.DeclaringType.Name}_{enumType.Name}"  // e.g., Tile_YieldType
            : enumType.Name;

			log.Debug("Registering enum: {EnumType} as {RegistrationName}", enumType.FullName, registrationName);

			var enumUserData = UserData.CreateStatic(enumType);
			enumTable[registrationName] = enumUserData;
		}

		script.Globals["ENUMS"] = enumTable;
	}

	void RegisterGlobals() {
		script.Globals["GAME_DATA"] = () => DynValue.FromObject(script, EngineStorage.gameData);
	}
}
