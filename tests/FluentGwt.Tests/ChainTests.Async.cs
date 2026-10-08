using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ChainTests
{
	[Fact]
	public Task WhenGivenIsAsyncThenItCompletesBeforeTheNextGiven()
		=> Context
			.GivenChain(x => x.Probe
				.Given(async p =>
				{
					await Task.Yield();
					p.Record("first");
				})
				.Given(p => p.Record("second"))
				.When(p => p.Record("act"))
				.Then(p => x.Asserted = p.Log))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be("first,second,act"));

	[Fact]
	public Task WhenGivenReturnsValueTaskThenItIsAwaited()
		=> Context
			.GivenChain(x => x.Probe.Given(p => p.ActLater()).And(p => p.ActLater()).When(p => p.Answer()).ThenFixture(p => x.Asserted = p.Acts))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().Be(3));

	[Fact]
	public Task WhenStepThrowsOperationCancelledThenTestReportsCancellation()
		=> Context
			.GivenChain(x => x.Probe.Given().When(p => p.Cancel()).Then(_ => { }))
			.WhenExecutingTheChainCapturingCancellation()
			.Then(x => x.Cancellation.Should().BeSameAs(x.Probe.Cancellation));
}
