namespace FluentGwt;

[AttributeUsage(AttributeTargets.Assembly)]
public abstract class TestRunnerAttribute : Attribute
{
	public abstract CancellationToken CancellationToken { get; }
}
