using AwesomeAssertions;

namespace FluentGwt.Tests;

public sealed partial class BuildTargetTests
{
	private const IntegrationJustification BuildsSamples = IntegrationJustification.DiskIo | IntegrationJustification.MultipleThreads;
	private const string BuildsSamplesReason = "Builds and runs a sample project with the .NET SDK";

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenIntegrationPropertyIsSetThenIntegrationSymbolIsDefined()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning("-p:FluentGwtIntegration=true")
			.Then(report => report.Should().Contain("INTEGRATION"));

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenIntegrationPropertyIsSetThenMarkerIsEmitted()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning("-p:FluentGwtIntegration=true")
			.Then(report => report.Should().Contain("MARKER=True"));

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenIntegrationPropertyIsSetThenOtherConstantsArePreserved()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning("-p:FluentGwtIntegration=true")
			.Then(report => report.Should().Contain("DEBUG").And.Contain("TRACE").And.Contain("NET10_0"));

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenIntegrationPropertyIsUnsetThenNoSymbolAndNoMarker()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning()
			.Then(report => report.Should().NotContain("INTEGRATION").And.Contain("MARKER=False"));

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenIntegrationPropertyIsNotTrueThenNoSymbolAndNoMarker()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning("-p:FluentGwtIntegration=yes")
			.Then(report => report.Should().NotContain("INTEGRATION").And.Contain("MARKER=False"));

	[IntegrationFact(BuildsSamples, BuildsSamplesReason)]
	public Task WhenTargetsAreImportedThenTheTestRunnerIsStamped()
		=> Context
			.GivenSampleImportingTheTargets()
			.WhenBuildingAndRunning()
			.Then(report => report.Should().Contain("RUNNER=True"));

	[IntegrationFact(BuildsSamples | IntegrationJustification.NetworkIo, "Packs the library, restores it from nuget.org into an isolated cache, then builds and runs a sample")]
	public Task WhenXunitPackageIsReferencedTransitivelyThenTargetsStillApply()
		=> Context
			.GivenPackedLibraryReferencedThroughAHelperLibrary()
			.WhenBuildingAndRunning("-p:FluentGwtIntegration=true")
			.Then(report => report.Should().Contain("INTEGRATION").And.Contain("DEBUG").And.Contain("MARKER=True").And.Contain("RUNNER=True"));
}
