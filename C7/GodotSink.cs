using Godot;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Serilog.Formatting;
using System.IO;

public partial class GodotSink : ILogEventSink {
	private readonly ITextFormatter _formatter;

	public GodotSink(ITextFormatter formatter) {
		_formatter = formatter;
	}

	public void Emit(LogEvent logEvent) {
		var message = string.Empty;
		if (_formatter is null) {
			message = logEvent.RenderMessage();
		} else {
			var writer = new StringWriter();
			_formatter.Format(logEvent, writer);
			message = writer.ToString();
		}
		GD.Print(message);
	}
}

public static class GodotSinkExtensions {
	public static LoggerConfiguration GodotSink(
			this LoggerSinkConfiguration loggerConfiguration,
			ITextFormatter formatter = null) {
		return loggerConfiguration.Sink(new GodotSink(formatter));
	}
}
