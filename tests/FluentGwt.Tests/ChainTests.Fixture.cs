namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public Probe Probe { get; } = new();
		public Func<Task> Chain { get; set; } = () => Task.CompletedTask;
		public Func<object> Unawaited { get; set; } = () => new();
		public Func<Then<Probe>>? Awaitable { get; set; }
		public Exception? Failure { get; set; }
		public OperationCanceledException? Cancellation { get; set; }
		public object? Asserted { get; set; }
	}
}
