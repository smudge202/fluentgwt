using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	private static string Output => TestContext.Current.TestOutputHelper!.Output;

	[Fact]
	public Task WhenTestFailsThenSeedIsReported()
		=> Context
			.GivenChain(_ => new DeclaredSeedFixture().Given().When(_ => { }).Then(_ => throw new InvalidOperationException()))
			.WhenExecutingTheChainCapturingFailure()
			.Then(_ => Output.Should().Contain("FluentGwt seed: 20261008 (declared; replay with FluentGwtSeed=20261008)"));

	[Fact]
	public Task WhenFreshSeedFailsThenItIsReportedAsFresh()
		=> Context
			.GivenChain(x => x.Subject.Given().When(_ => { }).Then(_ => throw new InvalidOperationException()))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => Output.Should().Contain($"FluentGwt seed: {x.Subject.Seed} (fresh; replay with FluentGwtSeed={x.Subject.Seed})"));

	[Fact]
	public Task WhenForcedSeedFailsThenItIsReportedAsForced()
		=> Context
			.GivenChain(x => x.Subject.Given(s => s.Configure("FluentGwtSeed", "5")).When(_ => { }).Then(_ => throw new InvalidOperationException()))
			.WhenExecutingTheChainCapturingFailure()
			.Then(_ => Output.Should().Contain("FluentGwt seed: 5 (forced; replay with FluentGwtSeed=5)"));

	[Fact]
	public Task WhenTestPassesThenSeedIsNotWritten()
		=> Context
			.GivenChain(x => x.Subject.Given().When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(_ => Output.Should().NotContain("FluentGwt seed"));

	[Fact]
	public Task WhenValidationFailsThenIsResolvableIsFalseAndReasonIsWritten()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Needy>().AddSingleton<Calendar>())
			.When(x => x.Subject.IsResolvable<Calendar>())
			.Then(resolvable => resolvable.Should().BeFalse())
			.And(_ => Output.Should().Contain(nameof(Needy)));

	[Fact]
	public Task WhenTeardownFailsThenTheFailureIsWritten()
		=> Context
			.GivenChain(x => x.Subject.Given(s => s.OnTeardown(_ => throw x.TeardownFailure)).When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => Output.Should().Contain(x.TeardownFailure.Message));

	[Fact]
	public Task WhenLogIsWrittenThenItIsAvailableForAssertion()
		=> Context
			.Given()
			.When(x => x.Subject.Resolve<ILogger<Clock>>().LogInformation("Ticked"))
			.Then(x => x.Subject.Logs.GetSnapshot().Should().ContainSingle(r => r.Message == "Ticked" && r.Level == LogLevel.Information));

	[Fact]
	public Task WhenFixtureLogsWarningThenItAppearsInTestOutput()
		=> Context
			.Given()
			.When(x => x.Subject.Resolve<ILogger<Clock>>().LogWarning("Running slow"))
			.Then(_ => Output.Should().Contain("Running slow"));

	[Fact]
	public Task WhenDebuggerIsDetachedThenInformationIsNotWritten()
		=> Context
			.Given(_ => Assert.SkipWhen(Debugger.IsAttached, "A debugger lowers the output level to Debug."))
			.When(x => x.Subject.Resolve<ILogger<Clock>>().LogInformation("Quietly"))
			.Then(_ => Output.Should().NotContain("Quietly"));

	[Fact]
	public Task WhenOutputLevelIsConfiguredThenOutputFollowsIt()
		=> Context
			.Given(x => x.Subject.Configure("FluentGwt:LogLevel", "Information"))
			.When(x => x.Subject.Resolve<ILogger<Clock>>().LogInformation("Loudly"))
			.Then(_ => Output.Should().Contain("Loudly"));

	[Fact]
	public Task WhenApplicationHostedServicesAreRemovedThenUnknowableFactoriesAreWritten()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<IHostedService>(_ => throw new InvalidOperationException()))
			.When(x => x.Subject.Services.RemoveApplicationHostedServices())
			.Then(_ => Output.Should().Contain("1 hosted service factory registration has no knowable implementation and was kept"));
}
