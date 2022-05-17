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

public class LogManager
{
	static LogManager()
	{
		ExpressionTemplate consoleTemplate = new ExpressionTemplate(
			"{@t:HH:mm:ss.fff} [{@l:u3}]{#if SourceContext is not null} {SourceContext}:{#end} {@m:lj}\n{#if @x is not null}\tException: {@x}{#end}",
			theme: TemplateTheme.Code);

		Log.Logger = new LoggerConfiguration()
			//.WriteTo.Console(formatter: consoleTemplate)
			.WriteTo.GodotSink(formatter: consoleTemplate)
			.MinimumLevel.Debug()
			.CreateLogger();
	}

	public static ILogger ForContext<T>()
	{
		return Log.ForContext<T>();
	}
}

public class GodotSink : ILogEventSink
{
	private readonly ITextFormatter _formatter;

	public GodotSink(ITextFormatter formatter)
	{
		_formatter = formatter;
	}

	public void Emit(LogEvent logEvent)
	{
		var message = string.Empty;
		if (_formatter is null)
		{
			message = logEvent.RenderMessage();
		}
		else
		{
			var writer = new StringWriter();
			_formatter.Format(logEvent, writer);
			message = writer.ToString();
		}

		GD.Print(message);
	}
}

public static class GodotSinkExtensions
{
	public static LoggerConfiguration GodotSink(
			this LoggerSinkConfiguration loggerConfiguration,
			ITextFormatter formatter = null)
	{
		return loggerConfiguration.Sink(new GodotSink(formatter));
	}
}
