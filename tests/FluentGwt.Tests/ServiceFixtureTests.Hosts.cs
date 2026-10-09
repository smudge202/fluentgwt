using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenHostIsAttachedThenItStartsAfterImmediateGivensAndBeforeDeferredOnes()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Attach(new RecordingHost("host", s.Log)))
				.Given(s => s.Log.Add("deferred"))
				.Deferred()
				.Given(s => s.Log.Add("immediate"))
				.When(s => s.Log.Add("act"))
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().StartWith(["immediate", "start host", "deferred", "act"]));

	[Fact]
	public Task WhenHostsAreAttachedThenTheyStartInOrderAndAreDisposedInReverseAfterTeardownCallbacks()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Attach(new RecordingHost("first", s.Log)))
				.Given(s => s.Attach(new RecordingHost("second", s.Log)))
				.Given(s => s.OnTeardown(_ => s.Record("callback")))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().Equal("start first", "start second", "callback", "dispose second", "dispose first", "fixture"));

	[Fact]
	public Task WhenAssertionFailsThenAttachedHostsAreStillDisposed()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Attach(new RecordingHost("host", s.Log)))
				.When(_ => { })
				.Then(_ => throw x.AssertionFailure))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Subject.Log.Should().Contain("dispose host"));

	[Fact]
	public Task WhenHostIsAttachedAfterHostsHaveStartedThenTheFixtureOnlyDisposesIt()
		=> Context
			.GivenChain(x => x.Subject
				.Given(_ => { })
				.When(s => s.Attach(new RecordingHost("late", s.Log)))
				.Then(s => s.Log.Add("assert")))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().Equal("assert", "dispose late", "fixture"));

	[Fact]
	public Task WhenHostFailsToStartThenTestFailsAsArrangementWithTheStartupException()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Attach(new RecordingHost("broken", s.Log, x.AssertionFailure)))
				.When(s => s.Log.Add("act"))
				.ThenArrangementFails<InvalidOperationException>(e => e.Should().BeSameAs(x.AssertionFailure)))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().NotContain("act"));

	internal sealed class RecordingHost(string name, List<string> log, Exception? failure = null) : FixtureHost
	{
		public ValueTask Start(CancellationToken cancellationToken)
		{
			log.Add($"start {name}");
			return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
		}

		public ValueTask DisposeAsync()
		{
			log.Add($"dispose {name}");
			return ValueTask.CompletedTask;
		}
	}
}
