using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenActThrowsExpectedTypeThenExpectationPasses()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<InvalidOperationException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull());

	[Fact]
	public Task WhenActThrowsDerivedTypeThenThrowsPasses()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<SystemException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull());

	[Fact]
	public Task WhenActThrowsDerivedTypeThenThrowsExactlyFails()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrowsExactly<SystemException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure!.Message.Should().Contain(nameof(SystemException)));

	[Fact]
	public Task WhenActDoesNotThrowThenFailureNamesExpectedType()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).ThenThrows<InvalidOperationException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure!.Message.Should().Contain(nameof(InvalidOperationException)));

	[Fact]
	public Task WhenActThrowsUnrelatedTypeThenFailureCarriesActualException()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<ArgumentException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure!.Message.Should().Contain(nameof(InvalidOperationException)).And.Contain(x.Probe.Failure.Message));

	[Fact]
	public Task WhenGivenThrowsExpectedTypeThenExpectationDoesNotPass()
		=> Context
			.GivenChain(x => x.Probe.Given(p => p.Fail()).When(p => p.Act()).ThenThrows<InvalidOperationException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeSameAs(x.Probe.Failure));

	[Fact]
	public Task WhenAssertionOnTheExceptionFailsThenItsMessagePropagates()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<InvalidOperationException>(e => e.Message.Should().Be("a different message")))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure!.Message.Should().Contain("a different message"));

	[Fact]
	public Task WhenThrowsAssertsFixtureAndExceptionThenItReceivesBoth()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<InvalidOperationException>((p, e) => x.Asserted = (p, (Exception)e)))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be((x.Probe, x.Probe.Failure)));

	[Fact]
	public Task WhenActWithResultThrowsThenExpectationReceivesTheException()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.AnswerOrFail()).ThenThrows<InvalidOperationException>(e => x.Asserted = e))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Failure));

	[Fact]
	public Task WhenArrangementFailsAsExpectedThenThenArrangementFailsPasses()
		=> Context
			.GivenChain(x => x.Probe.Given(p => p.Fail()).When(p => p.Act()).ThenArrangementFails<InvalidOperationException>())
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull())
			.And(x => x.Probe.Acts.Should().Be(0));

	[Fact]
	public Task WhenAndFollowsThrowsThenItReceivesTheFixture()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Fail()).ThenThrows<InvalidOperationException>().And(p => x.Asserted = p))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe));
}
