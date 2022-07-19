using System;
using Godot;
using Serilog;
using Serilog.Templates;

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

		// You can filter this several ways with the expression in Filter.ByIncludingOnly
		//   "SourceContext like 'C7Engine.AI.%'"	<-- filters on the source context, i.e. namespace + class name.  In this case, only shows messages from the C7Engine.AI namespace.
		//   "@m like '%citizen%'"					<-- filters on the message, in this case only returning messages containing the phrase 'citizen'
		//   "@l = 'Information'"					<-- filters on the level.
		// Filtering on the level can be used in conjunction with other filters, e.g.:
		//   "@l = 'Information' OR SourceContext like 'C7Engine.AI.%'"
		// Includes all logs of an 'Information' level regardless of namespace, and all logs of
		// the C7Engine.AI namespace regardless of log level.
		Log.Logger = new LoggerConfiguration()
			// .WriteTo.GodotSink(formatter: consoleTemplate)	//Writing to console can slow the game down considerably (see #278).  Thus it is disabled by default.
			.WriteTo.File("log.txt", buffered: true, flushToDiskInterval: TimeSpan.FromMilliseconds(250), fileSizeLimitBytes: 52428800,	//50 MB
			              outputTemplate: "[{Level:u3}] {Timestamp:HH:mm:ss} {SourceContext}: {Message:lj} {NewLine}{Exception}")

			.Filter.ByIncludingOnly("(@l = 'Fatal' OR @l = 'Error' OR @l = 'Warning' OR @l = 'Information') OR SourceContext like 'C7Engine.CityProductionAI%'")	//suggested:  OR SourceContext like 'C7Engine.AI.%' (insert the namespace you need to debug)
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
