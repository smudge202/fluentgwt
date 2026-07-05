using System.Reflection;

namespace FluentGwt;

internal static class Runner
{
	private static readonly TestRunnerAttribute? _Current = Assembly.GetEntryAssembly()?.GetCustomAttribute<TestRunnerAttribute>();

	public static CancellationToken Token => _Current?.CancellationToken ?? CancellationToken.None;

	public static string? TestIdentity => _Current?.TestIdentity;

	public static Action<string>? Output => _Current?.Output;

	public static void Write(Action<string>? output, string line)
	{
		// xunit refuses output once its test has finished, and work a test started can outlive it;
		// a line written that late has no test to belong to, so it is dropped rather than fail the work.
		try
		{
			output?.Invoke(line);
		}
		catch (InvalidOperationException) { }
	}
}
