using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class HostedServiceTests
{
	[Fact]
	public Task WhenHostedServiceIsRemovedByTypeThenItDoesNotStart()
		=> Context
			.Given(x => x.Collection.AddHostedService<Background>().AddHostedService<OtherBackground>())
			.WhenListingHostedServices(x => x.RemoveHostedService<Background>())
			.Then(services => services.Should().Equal(nameof(OtherBackground)));

	[Fact]
	public Task WhenHostedServiceIsRegisteredAsInstanceThenRemovalByTypeRemovesIt()
		=> Context
			.Given(x => x.Collection.AddSingleton<IHostedService>(new Background()))
			.WhenListingHostedServices(x => x.RemoveHostedService(BackgroundType))
			.Then(services => services.Should().BeEmpty());

	[Fact]
	public Task WhenHostedServiceIsRegisteredByTypedFactoryThenRemovalByTypeRemovesIt()
		=> Context
			.Given(x => x.Collection.AddHostedService(_ => new Background()))
			.WhenListingHostedServices(x => x.RemoveHostedService<Background>())
			.Then(services => services.Should().BeEmpty());

	[Fact]
	public Task WhenFactoryReturnTypeIsNotKnowableThenRemovalByTypeDoesNotMatchIt()
		=> Context
			.Given(x => x.Collection.AddSingleton<IHostedService>(_ => new Background()).AddHostedService<Background>())
			.WhenListingHostedServices(x => x.RemoveHostedService<Background>())
			.Then(services => services.Should().Equal($"factory {nameof(IHostedService)}"));

	[Fact]
	public Task WhenKeyedHostedServiceIsRegisteredThenRemovalIgnoresIt()
		=> Context
			.Given(x => x.Collection.AddKeyedSingleton<IHostedService, Background>("keyed").AddHostedService<Background>())
			.WhenListingHostedServices(x => x.RemoveHostedService<Background>())
			.Then(services => services.Should().Equal("keyed keyed"));

	[Fact]
	public Task WhenRemovedHostedServiceIsNotRegisteredThenArrangementFailsNamingTheType()
		=> Context
			.Given(x => x.Collection.AddHostedService<OtherBackground>())
			.Given(x => x.Collection.RemoveHostedService<Background>())
			.When(_ => { })
			.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain(nameof(Background)));

	[Fact]
	public Task WhenRemovalMatchesNothingThenMessageCountsUnknowableFactories()
		=> Context
			.Given(x => x.Collection.AddSingleton<IHostedService>(_ => new Background()).AddSingleton<IHostedService>(_ => new OtherBackground()))
			.When(x => x.Collection.RemoveHostedService<Background>())
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain("2 factory registrations"));

	[Fact]
	public Task WhenApplicationHostedServicesAreRemovedThenFrameworkHostedServicesAreKept()
		=> Context
			.Given(x => x.Collection.AddHealthChecks())
			.Given(x => x.Collection.AddHostedService<Background>())
			.WhenListingHostedServices(x => x.RemoveApplicationHostedServices())
			.Then(services => services.Should().Equal("HealthCheckPublisherHostedService"));

	[Fact]
	public Task WhenApplicationHostedServicesAreRemovedThenUnknowableFactoriesAreKept()
		=> Context
			.Given(x => x.Collection.AddSingleton<IHostedService>(_ => new Background()).AddHostedService<OtherBackground>())
			.WhenListingHostedServices(x => x.RemoveApplicationHostedServices())
			.Then(services => services.Should().Equal($"factory {nameof(IHostedService)}"));

	[Fact]
	public Task WhenTestAddsHostedServiceAfterRemovalThenItIsKept()
		=> Context
			.Given(x => x.Collection.AddHostedService<Background>())
			.WhenListingHostedServices(x => x.RemoveApplicationHostedServices().AddHostedService<OtherBackground>())
			.Then(services => services.Should().Equal(nameof(OtherBackground)));
}
