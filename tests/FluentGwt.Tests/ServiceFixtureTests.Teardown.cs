using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenChainEndsThenFixtureCancellationIsSignalled()
		=> Context
			.GivenChain(x => x.Subject.Given().When(s => x.Asserted = s.Cancellation).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => ((CancellationToken)x.Asserted!).IsCancellationRequested.Should().BeTrue());

	[Fact]
	public Task WhenTeardownRunsThenFixtureCancellationIsAlreadySignalled()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(_ =>
				{
					x.Asserted = s.Cancellation.IsCancellationRequested;
					return ValueTask.CompletedTask;
				}))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(true));

	[Fact]
	public Task WhenTeardownCallbacksAreRegisteredThenTheyRunInReverseOrder()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(_ => s.Record("first")))
				.Given(s => s.OnTeardown(_ => s.Record("second")))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().Equal("second", "first", "fixture"));

	[Fact]
	public Task WhenTeardownCallbackThrowsThenLaterCallbacksStillRun()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(_ => s.Record("first")))
				.Given(s => s.OnTeardown(_ => throw x.TeardownFailure))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Subject.Log.Should().Equal("first", "fixture"))
			.And(x => x.Failure.Should().BeSameAs(x.TeardownFailure));

	[Fact]
	public Task WhenAssertionAndTeardownBothFailThenAssertionFailureIsReported()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(_ => throw x.TeardownFailure))
				.When(_ => { })
				.Then(_ => throw x.AssertionFailure))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeOfType<AggregateException>()
				.Which.InnerExceptions.Should().Equal(x.AssertionFailure, x.TeardownFailure));

	[Fact]
	public Task WhenOnlyTheAssertionFailsThenItPropagatesUnchanged()
		=> Context
			.GivenChain(x => x.Subject.Given().When(_ => { }).Then(_ => throw x.AssertionFailure))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeSameAs(x.AssertionFailure));

	[Fact]
	public Task WhenGivenFailsThenTeardownStillRunsForWhatWasArranged()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(_ => s.Record("arranged")))
				.Given(_ => throw x.AssertionFailure)
				.Given(s => s.OnTeardown(_ => s.Record("never arranged")))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Subject.Log.Should().Equal("arranged", "fixture"));

	[Fact]
	public Task WhenTeardownCallbackRunsThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.OnTeardown(token =>
				{
					x.Asserted = token;
					return ValueTask.CompletedTask;
				}))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));
}
