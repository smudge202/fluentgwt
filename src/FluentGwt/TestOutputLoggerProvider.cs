using Microsoft.Extensions.Logging;

namespace FluentGwt;

internal sealed class TestOutputLoggerProvider(Action<string> write, LogLevel minimum, string? source = null) : ILoggerProvider
{
	private readonly string _prefix = source is null ? string.Empty : $"[{source}] ";

	public ILogger CreateLogger(string categoryName) => new TestOutputLogger(_prefix, categoryName, write, minimum);

	public void Dispose() { }

	private sealed class TestOutputLogger(string prefix, string category, Action<string> write, LogLevel minimum) : ILogger
	{
		public IDisposable? BeginScope<State>(State state) where State : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum && logLevel != LogLevel.None;

		public void Log<State>(LogLevel logLevel, EventId eventId, State state, Exception? exception, Func<State, Exception?, string> formatter)
		{
			ArgumentNullException.ThrowIfNull(formatter);
			if (!IsEnabled(logLevel))
				return;
			write(exception is null
				? $"{prefix}[{logLevel}] {category}: {formatter(state, exception)}"
				: $"{prefix}[{logLevel}] {category}: {formatter(state, exception)}{Environment.NewLine}{exception}");
		}
	}
}
