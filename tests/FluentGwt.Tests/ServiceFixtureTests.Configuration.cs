using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenAppSettingsFileIsPresentThenValuesAreAvailable()
		=> Context
			.Given()
			.When(x => x.Subject.Configuration["FluentGwtTests:FromSettings"])
			.Then(value => value.Should().Be("settings"));

	[Fact]
	public Task WhenEnvironmentVariableIsSetThenItOverridesAppSettings()
		=> Context
			.Given()
			.When(x => x.Subject.Configuration["FluentGwtTests:Overridden"])
			.Then(value => value.Should().Be("environment"));

	[Fact]
	public Task WhenValueIsConfiguredOnFixtureThenItOverridesEverySource()
		=> Context
			.Given(x => x.Subject.Configure("FluentGwtTests:Overridden", "fixture"))
			.When(x => x.Subject.Configuration["FluentGwtTests:Overridden"])
			.Then(value => value.Should().Be("fixture"));

	[Fact]
	public Task WhenFixtureValueIsConfiguredThenOtherFixturesDoNotSeeIt()
		=> Context
			.Given(x => x.Subject.Configure("FluentGwtTests:OnlyHere", "fixture"))
			.When(x => x.Other.Configuration["FluentGwtTests:OnlyHere"])
			.Then(value => value.Should().BeNull());

	[Fact]
	public Task WhenConfigurationIsResolvedThenItIsTheFixtureConfiguration()
		=> Context
			.Given()
			.When(x => x.Subject.Resolve<IConfiguration>())
			.Then((x, configuration) => configuration.Should().BeSameAs(x.Subject.Configuration));

	[Fact]
	public Task WhenValueIsConfiguredAfterResolutionThenResolvedConfigurationSeesIt()
		=> Context
			.Given(x => x.Subject.Resolve<IConfiguration>())
			.Given(x => x.Subject.Configure("FluentGwtTests:Late", "late"))
			.When(x => x.Subject.Resolve<IConfiguration>()["FluentGwtTests:Late"])
			.Then(value => value.Should().Be("late"));

	[Fact]
	public Task WhenTestRegistersItsOwnConfigurationThenItWins()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<IConfiguration>(x.OwnConfiguration))
			.When(x => x.Subject.Resolve<IConfiguration>())
			.Then((x, configuration) => configuration.Should().BeSameAs(x.OwnConfiguration));

	[Fact]
	public Task WhenEnvironmentSettingsFileIsAbsentThenConfigurationStillBuilds()
		=> Context
			.Given()
			.When(x => x.Subject.Configuration["FluentGwtTests:NotAnywhere"])
			.Then(value => value.Should().BeNull());
}
