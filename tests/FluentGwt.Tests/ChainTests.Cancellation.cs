using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenTargetedGivenTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given((_, token) => x.Token(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenTargetedGivenTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given((_, token) => x.TokenLater(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenTransitionTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().Given((_, token) => x.Token(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenTransitionTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().Given((_, token) => x.TokenLater(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterGivenTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given(_ => { }).And((_, token) => x.Token(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterGivenTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given(_ => { }).And((_, token) => x.TokenLater(token)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenActTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When((_, token) => x.Token(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenActTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When((_, token) => x.TokenLater(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenActWithResultTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When((_, token) => Task.FromResult(token)).Then(token => x.Asserted = token))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenActWithResultTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When((_, token) => ValueTask.FromResult(token)).Then(token => x.Asserted = token))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterWhenTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When(_ => { }).And((_, token) => x.Token(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterWhenTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When(_ => { }).And((_, token) => x.TokenLater(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterWhenWithResultTakesCancellationThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).And((_, token) => x.Token(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));

	[Fact]
	public Task WhenAndAfterWhenWithResultTakesCancellationAsValueTaskThenItReceivesTheTestToken()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Answer()).And((_, token) => x.TokenLater(token)).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(TestContext.Current.CancellationToken));
}
