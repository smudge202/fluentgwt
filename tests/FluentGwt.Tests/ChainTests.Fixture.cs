namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public Probe Probe { get; } = new();
		public Func<Task> Chain { get; set; } = () => Task.CompletedTask;
		public Func<object> Unawaited { get; set; } = () => new();
		public Exception? Failure { get; set; }
		public object? Asserted { get; set; }
	}
}
