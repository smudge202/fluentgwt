using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentGwt;

internal sealed class StartFailure : ILoggerProvider, ILogger
{
	internal const string Category = "Microsoft.Extensions.Hosting.Internal.Host";

	private Exception? _exception;

	public Exception? Exception => Volatile.Read(ref _exception);

	public ILogger CreateLogger(string categoryName) => categoryName == Category ? this : NullLogger.Instance;

	public IDisposable? BeginScope<State>(State state) where State : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

	public void Log<State>(LogLevel logLevel, EventId eventId, State state, Exception? exception, Func<State, Exception?, string> formatter)
	{
		if (IsEnabled(logLevel) && exception is not null)
			Interlocked.CompareExchange(ref _exception, exception, null);
	}

	public void Dispose() { }
}
