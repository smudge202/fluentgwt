namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public SubjectFixture Subject { get; } = new();
		public SubjectFixture Other { get; } = new();
		public Clock Clock { get; } = new();
		public Clock OtherClock { get; } = new();
		public TaskCompletionSource Entered { get; } = new();
		public TaskCompletionSource Signal { get; } = new();
		public Func<Task> Chain { get; set; } = () => Task.CompletedTask;
		public Exception? Failure { get; set; }
		public object? Asserted { get; set; }
	}

	internal sealed class SubjectFixture : ServiceFixture
	{
		public List<string> Log { get; } = [];
		public bool Disposed { get; private set; }

		protected override ValueTask DisposeFixture()
		{
			Log.Add("fixture");
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}

	internal sealed class Clock;

	internal sealed class Calendar;

	internal sealed class Counter;

	internal sealed class Needy(Clock clock)
	{
		public Clock Clock => clock;
	}

	internal sealed class Disposable(List<string> log) : IDisposable
	{
		public bool Disposed { get; private set; }

		public void Dispose()
		{
			log.Add("service");
			Disposed = true;
		}
	}

	internal sealed class AsyncOnly : IAsyncDisposable
	{
		public bool Disposed { get; private set; }

		public ValueTask DisposeAsync()
		{
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}

	internal sealed class Gate
	{
		public Gate(TaskCompletionSource entered, Task signal)
		{
			entered.SetResult();
			Opened = signal.Wait(TimeSpan.FromSeconds(5));
		}

		public bool Opened { get; }
	}
}
