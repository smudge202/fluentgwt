using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenAndFollowsGivenThenItRunsAsATransitionInOrder()
		=> Context
			.GivenChain(x => x.Probe
				.Given(p => p.Record("given"))
				.And(p => p.Record("and"))
				.When(p => p.Record("act"))
				.Then(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("given,and,act"));

	[Fact]
	public Task WhenAndFollowsWhenThenItRunsAfterTheAct()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Record("act")).And(p => p.Record("and")).Then(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("act,and"));

	[Fact]
	public Task WhenAndFollowsWhenThenResultIsPreserved()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).And(p => p.Record("and")).Then(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Result));

	[Fact]
	public Task WhenAndResultFollowsWhenThenResultIsReplaced()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).AndResult(p => p.Acts + 41).Then(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(42));

	[Fact]
	public Task WhenAndFollowsThenThenItRunsAsAnAssertion()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(p => p.Record("then")).And(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("then"));

	[Fact]
	public Task WhenAndFollowsThenOnAResultThenItReceivesTheResult()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then(_ => { }).And(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Result));

	[Fact]
	public Task WhenAndFixtureFollowsThenThenItReceivesTheFixture()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then(_ => { }).AndFixture(p => x.Asserted = p))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe));

	[Fact]
	public Task WhenFirstAssertionFailsThenLaterAssertionsDoNotRun()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(p => p.Fail()).And(p => x.Asserted = p))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Asserted.Should().BeNull());

	[Fact]
	public Task WhenSeveralAssertionsFollowThenActRunsOnce()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }).And(_ => { }).And(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Probe.Acts.Should().Be(1));

	[Fact]
	public Task WhenAndFollowsThenInExpressionBodiedFactThenChainExecutes()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }).And(p => x.Asserted = p.Acts))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(1));

	[Fact]
	public Task WhenAsyncAndIsUsedThenEachPositionAwaitsIt()
		=> Context
			.GivenChain(x => x.Probe
				.Given(p => p.Record("given"))
				.And(async p =>
				{
					await Task.Yield();
					p.Record("and-given");
				})
				.When(p => p.Record("act"))
				.And(async p =>
				{
					await Task.Yield();
					p.Record("and-act");
				})
				.Then(_ => { })
				.And(async p =>
				{
					await Task.Yield();
					x.Asserted = p.Log;
				}))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("given,and-given,act,and-act"));
}
