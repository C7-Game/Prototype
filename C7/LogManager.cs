using Godot;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Serilog.Formatting;
using System;
using System.IO;

public class LogManager : Node
{
	public override void _Ready()
	{
		// Format looks like:
		// timestamp [level] context: message
		//		Exception: exception
		// Example: 22:25:32.528 [DBG] MainMenu: enter MainMenu._Ready
		ExpressionTemplate consoleTemplate = new ExpressionTemplate(
			"{@t:HH:mm:ss.fff} [{@l:u3}]{#if SourceContext is not null} {SourceContext}:{#end} {@m:lj}{#if @x is not null}\tException: {@x}{#end}");

		Log.Logger = new LoggerConfiguration()
			.WriteTo.GodotSink(formatter: consoleTemplate)
			.MinimumLevel.Debug()
			.CreateLogger();

		GD.Print("Hello logger!");
		Log.ForContext<LogManager>().Debug("Hello!");
	}

	public override void _Notification(int what)
	{
		if (what == MainLoop.NotificationWmQuitRequest)
		{
			GD.Print("Goodbye logger!");
			Log.ForContext<LogManager>().Debug("Goodbye!");
			Log.CloseAndFlush();
			GetTree().Quit();
		}
	}

	public static ILogger ForContext<T>()
	{
		return Log.ForContext<T>();
	}
}
