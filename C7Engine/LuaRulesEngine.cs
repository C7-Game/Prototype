using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using MoonSharp.Interpreter;
using Serilog;
using System.IO;
using MoonSharp.Interpreter.Loaders;
using System.Collections.Generic;
using C7GameData;

namespace C7Engine;

/// Utility class for converting Lua functions into C# delegates.
public static class LuaDelegateConverter {
	static readonly ILogger log = Log.ForContext(typeof(LuaDelegateConverter));

	public static Delegate CreateDelegate(Script script, Closure luaFunc, Type delegateType) {
		if (!typeof(Delegate).IsAssignableFrom(delegateType))
			throw new ArgumentException("Type must be a delegate.", nameof(delegateType));

		var invoke = delegateType.GetMethod("Invoke")!;
		var parameters = invoke.GetParameters()
			.Select(p => Expression.Parameter(p.ParameterType, p.Name))
			.ToArray();

		var callExpression = CreateLuaCallExpression(script, luaFunc, invoke.ReturnType, parameters);
		var lambda = Expression.Lambda(delegateType, callExpression, parameters);

		return lambda.Compile();
	}

	static Expression CreateLuaCallExpression(Script script, Closure luaFunc, Type returnType, ParameterExpression[] parameters) {
		var genericType = returnType == typeof(void) ? typeof(object) : returnType;
		var argsArray = Expression.NewArrayInit(typeof(object),
			parameters.Select(p => Expression.Convert(p, typeof(object))));

		var callLua = Expression.Call(
			typeof(LuaDelegateConverter),
			nameof(InvokeLuaFunction),
			[genericType],
			Expression.Constant(script),
			Expression.Constant(luaFunc),
			argsArray);

		return returnType == typeof(void) ? callLua : Expression.Convert(callLua, returnType);
	}

	static T InvokeLuaFunction<T>(Script script, Closure luaFunc, object[] args) {
		var dynArgs = args.Select(arg => DynValue.FromObject(script, arg)).ToArray();

		DynValue result;

		try {
			result = luaFunc.Call(dynArgs);
		} catch (ScriptRuntimeException ex) {
			LogLuaError(ex);
			throw;
		}

		if (typeof(T) == typeof(void))
			return default;

		try {
			return result.ToObject<T>();
		} catch (Exception ex) {
			log.Error("Failed to convert Lua result to type '{TargetType}'. Lua result type: {LuaType}", typeof(T), result.Type);
			throw;
		}
	}

	static void LogLuaError(ScriptRuntimeException ex) {
		log.Error("Lua runtime error: {Message}", ex.DecoratedMessage);

		foreach (var frame in ex.CallStack)
			log.Error("Lua frame: Name='{Name}', Location='{Location}'", frame.Name, frame.Location);
	}
}

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
public class LuaRulesEngine {
	static ILogger log = Log.ForContext<LuaRulesEngine>();
	Script script = new();
	Table rules;

	static LuaRulesEngine() {
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

		DynValue res = script.DoFile(fullScriptPath);
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
		Delegate del = LuaDelegateConverter.CreateDelegate(script, closure, typeof(T));
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
