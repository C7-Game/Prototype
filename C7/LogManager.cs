using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;

public class LogManager
{
    static LogManager()
    {
		ExpressionTemplate consoleTemplate = new ExpressionTemplate(
			"{@t:HH:mm:ss.fff} [{@l:u3}]{#if SourceContext is not null} {SourceContext}:{#end} {@m:lj}\n{#if @x is not null}\tException: {@x}{#end}",
			theme: TemplateTheme.Code);

		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(formatter: consoleTemplate)
			.MinimumLevel.Debug()
			.CreateLogger();
	}

	public static ILogger ForContext<T>()
    {
		return Log.ForContext<T>();
    }
}
