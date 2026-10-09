using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenServiceIsOverriddenThenResolveReturnsTheOverride()
		=> Context
			.GivenClockRegistered()
			.Given(x => x.Subject.Services.Override(x.OtherClock))
			.When(x => x.Subject.Resolve<Clock>())
			.Then((x, clock) => clock.Should().BeSameAs(x.OtherClock));

	[Fact]
	public Task WhenServiceIsOverriddenThenEnumerableContainsOnlyTheOverride()
		=> Context
			.GivenClockRegistered()
			.Given(x => x.Subject.Services.AddSingleton(new Clock()))
			.Given(x => x.Subject.Services.Override(x.OtherClock))
			.When(x => x.Subject.Resolve<IEnumerable<Clock>>())
			.Then((x, clocks) => clocks.Should().ContainSingle().Which.Should().BeSameAs(x.OtherClock));

	[Fact]
	public Task WhenOverrideHasNoLifetimeThenOriginalLifetimeIsKept()
		=> Context
			.Given(x => x.Subject.Services.AddScoped<Calendar>())
			.Given(x => x.Subject.Services.Override<Calendar, Calendar>())
			.When(x => x.Subject.Services.Single(d => d.ServiceType == typeof(Calendar)).Lifetime)
			.Then(lifetime => lifetime.Should().Be(ServiceLifetime.Scoped));

	[Fact]
	public Task WhenServiceIsOverriddenByFactoryThenOriginalLifetimeIsKept()
		=> Context
			.Given(x => x.Subject.Services.AddScoped<Calendar>())
			.Given(x => x.Subject.Services.Override(_ => new Calendar()))
			.When(x => x.Subject.Services.Single(d => d.ServiceType == typeof(Calendar)).Lifetime)
			.Then(lifetime => lifetime.Should().Be(ServiceLifetime.Scoped));

	[Fact]
	public Task WhenServiceWasNeverRegisteredThenOverrideIsTransient()
		=> Context
			.Given(x => x.Subject.Services.Override<Calendar, Calendar>())
			.When(x => x.Subject.Services.Single(d => d.ServiceType == typeof(Calendar)).Lifetime)
			.Then(lifetime => lifetime.Should().Be(ServiceLifetime.Transient));

	[Fact]
	public Task WhenKeyedServiceIsOverriddenThenOtherKeysAreUntouched()
		=> Context
			.Given(x => x.Subject.Services.AddKeyedSingleton("first", new Clock()).AddKeyedSingleton("second", x.Clock))
			.Given(x => x.Subject.Services.Override("first", x.OtherClock))
			.When(x => (First: x.Subject.Resolve<Clock>("first"), Second: x.Subject.Resolve<Clock>("second")))
			.Then((x, clocks) => clocks.Should().Be((x.OtherClock, x.Clock)));

	[Fact]
	public Task WhenKeyedServiceIsOverriddenThenUnkeyedRegistrationIsUntouched()
		=> Context
			.GivenClockRegistered()
			.Given(x => x.Subject.Services.AddKeyedSingleton("first", new Clock()))
			.Given(x => x.Subject.Services.Override("first", x.OtherClock))
			.When(x => x.Subject.Resolve<Clock>())
			.Then((x, clock) => clock.Should().BeSameAs(x.Clock));

	[Fact]
	public Task WhenOverrideFollowsResolutionThenInvalidOperationIsThrown()
		=> Context
			.GivenClockRegistered()
			.GivenResolved<Clock>()
			.When(x => x.Subject.Services.Override(x.OtherClock))
			.ThenThrows<InvalidOperationException>();
}
