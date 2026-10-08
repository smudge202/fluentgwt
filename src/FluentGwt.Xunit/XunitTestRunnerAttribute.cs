using Xunit;

namespace FluentGwt;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class XunitTestRunnerAttribute : TestRunnerAttribute
{
	public override CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	public override string? TestIdentity => TestContext.Current.Test?.TestDisplayName;

	public override Action<string>? Output => TestContext.Current.TestOutputHelper is { } output ? output.WriteLine : null;
}
