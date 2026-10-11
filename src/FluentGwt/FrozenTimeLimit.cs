using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace FluentGwt;

internal static class FrozenTimeLimit
{
	public const string Key = "FluentGwt:FrozenTimeLimit";

	private static readonly TimeSpan _Default = TimeSpan.FromSeconds(5);

	public static TimeSpan? From(IConfiguration configuration)
	{
		var configured = configuration[Key];
		if (string.IsNullOrWhiteSpace(configured))
			return _Default;
		if (!double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
			throw new InvalidOperationException($"{Key} is a number of seconds, or 0 to turn the check off, but was '{configured}'.");
		return seconds == 0 ? null : TimeSpan.FromSeconds(seconds);
	}

	public static string Describe(TimeSpan limit, TimeSpan due) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"The test waited {limit.TotalSeconds:0.###}s on x.Time, which only moves when the test moves it: a timer set in this step for {due} has not fired. "
			+ $"Move time on with x.Time.Advance(...), or give the product the wall clock with x.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System). "
			+ $"{Key} sets the wait in seconds; 0 turns this check off.");
}
