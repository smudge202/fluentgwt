using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenGivenIsDeferredThenItRunsAfterImmediateGivens()
		=> Context
			.GivenChain(x => x.Probe
				.Given(p => p.Record("deferred"))
				.Deferred()
				.Given(p => p.Record("immediate"))
				.When(p => p.Record("act"))
				.Then(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("immediate,deferred,act"));

	[Fact]
	public Task WhenSeveralGivensAreDeferredThenTheyRunInDeferralOrder()
		=> Context
			.GivenChain(x => x.Probe
				.Given(p => p.Record("first deferred"))
				.Deferred()
				.Given(p => p.Record("immediate"))
				.Given(p => p.Record("second deferred"))
				.Deferred()
				.When(p => p.Record("act"))
				.Then(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("immediate,first deferred,second deferred,act"));

	[Fact]
	public Task WhenDeferredIsCalledFirstThenInvalidOperationIsThrown()
		=> Context
			.Given()
			.When(x => x.Probe.Given().Deferred())
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain(nameof(Given<Probe>.Deferred)));

	[Fact]
	public Task WhenImmediateGivenFailsThenNoDeferredGivenRuns()
		=> Context
			.GivenChain(x => x.Probe
				.Given(p => p.Record("deferred"))
				.Deferred()
				.Given(p => p.Fail())
				.When(p => p.Act())
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Probe.Events.Should().BeEmpty());
}
