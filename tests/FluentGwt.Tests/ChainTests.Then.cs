using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenThenAssertsResultThenItReceivesTheActResult()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.AnswerLater()).Then(result => x.Asserted = result))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe.Result));

	[Fact]
	public Task WhenThenAssertsFixtureAndResultThenItReceivesBoth()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then((p, result) => x.Asserted = (p, result)))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be((x.Probe, x.Probe.Result)));

	[Fact]
	public Task WhenThenFixtureIsUsedThenItReceivesTheFixtureAfterTheAct()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).ThenFixture(p => x.Asserted = p.Acts))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(1));

	[Fact]
	public Task WhenAssertionFailsThenOriginalAssertionExceptionPropagates()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(p => p.Fail()))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeSameAs(x.Probe.Failure));

	[Fact]
	public Task WhenThenIsReturnedAsTaskThenChainExecutes()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Probe.Acts.Should().Be(1));

	[Fact]
	public Task WhenThenIsAwaitedThenChainExecutes()
		=> Context
			.GivenAwaitableChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }))
			.WhenAwaitingTheChain()
			.Then(x => x.Probe.Acts.Should().Be(1));

	[Fact]
	public Task WhenAsyncAssertionIsUsedThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Act()).Then(async p =>
			{
				await Task.Yield();
				x.Asserted = p;
			}))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Probe));

	[Fact]
	public Task WhenAsyncFixtureAndResultAssertionIsUsedThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then(async (p, result) =>
			{
				await Task.Yield();
				x.Asserted = (p, result);
			}))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be((x.Probe, x.Probe.Result)));

	[Fact]
	public Task WhenFixtureAndResultAssertionReturnsValueTaskThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then((p, result) => x.AssertLater((p, result))))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be((x.Probe, x.Probe.Result)));

	[Fact]
	public Task WhenAsyncFixtureAndResultAssertionFailsThenItsExceptionFaultsTheChain()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).Then(async (p, _) =>
			{
				await Task.Yield();
				p.Fail();
			}))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeSameAs(x.Probe.Failure));

	[Fact]
	public Task WhenAsyncThenFixtureIsUsedThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).ThenFixture(async p =>
			{
				await Task.Yield();
				x.Asserted = p.Acts;
			}))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(1));

	[Fact]
	public Task WhenThenFixtureReturnsValueTaskThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).ThenFixture(p => x.AssertLater(p.Acts)))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(1));

	[Fact]
	public Task WhenAsyncThenFixtureFailsThenItsExceptionFaultsTheChain()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).ThenFixture(async p =>
			{
				await Task.Yield();
				p.Fail();
			}))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeSameAs(x.Probe.Failure));

	[Fact]
	public Task WhenThenIsAwaitedTwiceThenChainExecutesOnce()
		=> Context
			.GivenAwaitableChain(x => x.Probe.Given().When(p => p.Act()).Then(_ => { }))
			.WhenAwaitingTheChainTwice()
			.Then(x => x.Probe.Acts.Should().Be(1));
}
