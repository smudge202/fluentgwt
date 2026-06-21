using Xunit;

namespace FluentGwt;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class XunitTestRunnerAttribute : TestRunnerAttribute
{
	public override CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}
