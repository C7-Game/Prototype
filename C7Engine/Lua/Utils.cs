using System;
using System.Linq;
using System.Linq.Expressions;
using MoonSharp.Interpreter;
using Serilog;

namespace C7Engine.Lua;

/// Utility class for converting Lua functions into C# delegates.
public static class DelegateConverter {
	static readonly ILogger log = Log.ForContext(typeof(DelegateConverter));

	public static Delegate CreateDelegate(Script script, Closure luaFunc, Type delegateType) {
		if (!typeof(Delegate).IsAssignableFrom(delegateType))
			throw new ArgumentException("Type must be a delegate.", nameof(delegateType));

		System.Reflection.MethodInfo invoke = delegateType.GetMethod("Invoke")!;
		ParameterExpression[] parameters = invoke.GetParameters()
			.Select(p => Expression.Parameter(p.ParameterType, p.Name))
			.ToArray();

		Expression callExpression = CreateLuaCallExpression(script, luaFunc, invoke.ReturnType, parameters);
		LambdaExpression lambda = Expression.Lambda(delegateType, callExpression, parameters);

		return lambda.Compile();
	}

	static Expression CreateLuaCallExpression(Script script, Closure luaFunc, Type returnType, ParameterExpression[] parameters) {
		Type genericType = returnType == typeof(void) ? typeof(object) : returnType;
		NewArrayExpression argsArray = Expression.NewArrayInit(typeof(object),
			parameters.Select(p => Expression.Convert(p, typeof(object))));

		MethodCallExpression callLua = Expression.Call(
			typeof(DelegateConverter),
			nameof(InvokeLuaFunction),
			[genericType],
			Expression.Constant(script),
			Expression.Constant(luaFunc),
			argsArray);

		return returnType == typeof(void) ? callLua : Expression.Convert(callLua, returnType);
	}

	static T InvokeLuaFunction<T>(Script script, Closure luaFunc, object[] args) {
		DynValue[] dynArgs = args.Select(arg => DynValue.FromObject(script, arg)).ToArray();

		DynValue result = script.SafeCall(luaFunc, dynArgs);

		if (typeof(T) == typeof(void))
			return default;

		try {
			return result.ToObject<T>();
		} catch (Exception) {
			log.Error("Failed to convert Lua result to type '{TargetType}'. Lua result type: {LuaType}", typeof(T), result.Type);
			throw;
		}
	}
}

// Wrappers for Moonsharp Script methods
// Provide logging for Lua errors
public static class ScriptExtensions {
	static readonly ILogger log = Log.ForContext(typeof(ScriptExtensions));

	public static DynValue SafeCall(
		this Script script,
		Closure function,
		params object[] args
	) {
		try {
			return script.Call(function, args);
		} catch (ScriptRuntimeException ex) {
			LogRuntimeError(ex);
			throw;
		}
	}

	public static DynValue SafeDoFile(
		this Script script,
		string path
	) {
		try {
			return script.DoFile(path);
		} catch (SyntaxErrorException ex) {
			LogSyntaxError(ex);
			throw;
		} catch (ScriptRuntimeException ex) {
			LogRuntimeError(ex);
			throw;
		}
	}

	private static void LogRuntimeError(
		ScriptRuntimeException ex
	) {
		log.Error(
			ex,
			"Lua runtime error: {Message}",
			ex.DecoratedMessage
		);

		foreach (var frame in ex.CallStack) {
			log.Error(
				"Lua frame: Name='{Name}', Location='{Location}'",
				frame.Name,
				frame.Location
			);
		}
	}

	private static void LogSyntaxError(
		SyntaxErrorException ex
	) {
		log.Error(
			ex,
			"Lua syntax error: {Message}",
			ex.DecoratedMessage
		);
	}
}
