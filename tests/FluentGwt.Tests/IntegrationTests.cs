using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed class IntegrationTests
{
	private const IntegrationJustification Justification = IntegrationJustification.NetworkIo | IntegrationJustification.MultipleThreads;
	private const string Reason = "Publishes to the real message bus";

	[Fact]
	public Task WhenIntegrationFactIsDeclaredThenItSkipsUnlessTheGateIsOpen()
		=> new IntegrationFactAttribute(Justification, Reason)
			.Given()
			.When(x => (x.SkipUnless, x.SkipType))
			.Then(skip => skip.Should().Be((nameof(IntegrationGate.IsOpen), typeof(IntegrationGate))));

	[Fact]
	public Task WhenMarkerIsAbsentThenIntegrationFactIsSkippedWithJustificationAndReason()
		=> new IntegrationFactAttribute(Justification, Reason)
			.Given()
			.When(x => x.Skip)
			.Then(message => message.Should().Be(
				"Integration disabled (built without FluentGwtIntegration=true). Justification: NetworkIo, MultipleThreads. Reason: Publishes to the real message bus"));

	[Fact]
	public Task WhenIntegrationTheoryIsDeclaredThenEveryRowSkipsWithTheMessageUnlessTheGateIsOpen()
		=> new IntegrationTheoryAttribute(Justification, Reason)
			.Given()
			.When(x => (x.Skip, x.SkipUnless, x.SkipType))
			.Then(skip => skip.Should().Be((
				"Integration disabled (built without FluentGwtIntegration=true). Justification: NetworkIo, MultipleThreads. Reason: Publishes to the real message bus",
				nameof(IntegrationGate.IsOpen),
				typeof(IntegrationGate))));

	[Fact]
	public Task WhenIntegrationFactHasNoJustificationThenItIsRefused()
		=> Given
			.With(Reason)
			.When(_ => new IntegrationFactAttribute(default, Reason))
			.ThenThrows<ArgumentException>(e => e.Message.Should().Contain("justification"));

	[Fact]
	public Task WhenIntegrationFactHasNoReasonThenItIsRefused()
		=> Given
			.With(Reason)
			.When(_ => new IntegrationFactAttribute(Justification, " "))
			.ThenThrows<ArgumentException>(e => e.Message.Should().Contain("reason"));

#if !INTEGRATION
	[Fact]
	public Task WhenMarkerIsAbsentThenTheGateIsClosed()
		=> Given
			.With(Reason)
			.When(_ => IntegrationGate.IsOpen)
			.Then(open => open.Should().BeFalse());
#endif

	[IntegrationFact(IntegrationJustification.DiskIo, "Proves an integration build runs integration facts")]
	public Task WhenMarkerIsPresentThenIntegrationFactRuns()
		=> Given
			.With(Reason)
			.When(_ => IntegrationGate.IsOpen)
			.Then(open => open.Should().BeTrue());

	[IntegrationTheory(IntegrationJustification.DiskIo, "Proves an integration build runs every integration theory row")]
	[InlineData(1)]
	[InlineData(2)]
	public Task WhenMarkerIsPresentThenIntegrationTheoryRuns(int row)
		=> Given
			.With(row)
			.When(_ => IntegrationGate.IsOpen)
			.Then(open => open.Should().BeTrue());
}
