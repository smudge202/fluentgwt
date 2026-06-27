using System.Reflection;

namespace FluentGwt;

internal static class Runner
{
	private static readonly TestRunnerAttribute? _Current = Assembly.GetEntryAssembly()?.GetCustomAttribute<TestRunnerAttribute>();

	public static CancellationToken Token => _Current?.CancellationToken ?? CancellationToken.None;

	public static string? TestIdentity => _Current?.TestIdentity;
}
