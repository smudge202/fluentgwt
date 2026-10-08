using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenActHasNoResultThenThenReceivesTheFixture()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(p => x.Asserted = p))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe));

	[Fact]
	public Task WhenActHasResultThenThenReceivesTheResult()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Result));

	[Fact]
	public Task WhenActIsValueTaskThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.ActLater()).Then(p => x.Asserted = p.Acts))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(1));

	[Fact]
	public Task WhenActReturnsTaskOfResultThenThenReceivesTheResult()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.AnswerEventually()).Then(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Result));

	[Fact]
	public Task WhenChainIsNeverAwaitedThenActNeverRuns()
		=> Context
			.GivenUnawaitedChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }))
			.WhenBuildingTheChainWithoutAwaitingIt()
			.Then(x => x.Probe.Acts.Should().Be(0));

	[Fact]
	public Task WhenGivensFailThenActDoesNotRun()
		=> Context
			.GivenChain(x => x.Probe.Given(p => p.Fail()).When(p => p.Act()).Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Probe.Acts.Should().Be(0));
}
