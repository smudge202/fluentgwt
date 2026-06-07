using Microsoft.Extensions.Configuration;

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
		public IConfiguration OwnConfiguration { get; } = new ConfigurationBuilder().Build();
		public TaskCompletionSource Entered { get; } = new();
		public TaskCompletionSource Signal { get; } = new();
		public Func<Task> Chain { get; set; } = () => Task.CompletedTask;
		public Exception? Failure { get; set; }
		public Exception AssertionFailure { get; } = new InvalidOperationException("Assertion failure");
		public Exception TeardownFailure { get; } = new InvalidOperationException("Teardown failure");
		public object? Asserted { get; set; }
	}

	internal sealed class SubjectFixture : ServiceFixture
	{
		public List<string> Log { get; } = [];
		public bool Disposed { get; private set; }

		public ValueTask Record(string step)
		{
			Log.Add(step);
			return ValueTask.CompletedTask;
		}

		protected override ValueTask DisposeFixture()
		{
			Log.Add("fixture");
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}

	internal sealed class DeclaredSeedFixture : ServiceFixture
	{
		public const int DeclaredSeed = 20261008;
		public const string PinnedTestId = "p3resjvtm2uq";

		protected override int? FixedSeed => DeclaredSeed;
	}

	internal abstract class SharedSeedFixture : ServiceFixture
	{
		public const int SharedSeed = 42;

		protected override int? FixedSeed => SharedSeed;
	}

	internal sealed class FirstSharedSeedFixture : SharedSeedFixture;

	internal sealed class SecondSharedSeedFixture : SharedSeedFixture;

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
