using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenServiceRegisteredBeforeResolutionThenResolveReturnsIt()
		=> Context
			.GivenClockRegistered()
			.When(x => x.Subject.Resolve<Clock>())
			.Then((x, clock) => clock.Should().BeSameAs(x.Clock));

	[Fact]
	public Task WhenServicesAccessedAfterResolutionThenInvalidOperationIsThrown()
		=> Context
			.GivenClockRegistered()
			.GivenResolved<Clock>()
			.When(x => x.Subject.Services)
			.ThenThrows<InvalidOperationException>();

	[Fact]
	public Task WhenRegistrationIsRefusedThenMessageNamesEveryResolvedService()
		=> Context
			.GivenClockRegistered()
			.Given(x => x.Subject.Services.AddSingleton<Calendar>())
			.GivenResolved<Clock>()
			.GivenResolved<Calendar>()
			.When(x => x.Subject.Services)
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain(nameof(Clock)).And.Contain(nameof(Calendar)));

	[Fact]
	public Task WhenResolvedTwiceThenOneProviderIsBuilt()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Counter>())
			.When(x => (x.Subject.Resolve<Counter>(), x.Subject.Resolve<Counter>()))
			.Then(counters => counters.Item1.Should().BeSameAs(counters.Item2));

	[Fact]
	public Task WhenResolvedConcurrentlyThenOneProviderIsBuilt()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Counter>())
			.When(x => Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => x.Subject.Resolve<Counter>()))))
			.Then(counters => counters.Distinct().Should().ContainSingle());

	[Fact]
	public Task WhenTwoFixturesResolveConcurrentlyThenNeitherWaitsForTheOther()
		=> Context
			.Given(x => x.Other.Services.AddSingleton(x.OtherClock))
			.GivenSubjectResolutionWillWaitAtAGate()
			.WhenOtherFixtureResolvesWhileSubjectIsResolving()
			.Then(opened => opened.Should().BeTrue());

	[Fact]
	public Task WhenServiceIsComposableThenIsResolvableIsTrue()
		=> Context
			.GivenClockRegistered()
			.Given(x => x.Subject.Services.AddSingleton<Needy>())
			.When(x => x.Subject.IsResolvable<Needy>())
			.Then(resolvable => resolvable.Should().BeTrue());

	[Fact]
	public Task WhenDependencyIsMissingThenIsResolvableIsFalse()
		=> Context
			.Given(x => x.Subject.ValidateOnBuild = false)
			.Given(x => x.Subject.Services.AddSingleton<Needy>())
			.When(x => x.Subject.IsResolvable<Needy>())
			.Then(resolvable => resolvable.Should().BeFalse());

	[Fact]
	public Task WhenRegistrationIsUnconstructableThenFirstResolutionFailsAsArrangement()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Needy>().AddSingleton<Calendar>())
			.GivenResolved<Calendar>()
			.When(_ => { })
			.ThenArrangementFails<AggregateException>(e => e.Message.Should().Contain(nameof(Needy)));

	[Fact]
	public Task WhenScopedServiceIsResolvedFromRootThenResolutionFails()
		=> Context
			.Given(x => x.Subject.Services.AddScoped<Calendar>())
			.When(x => x.Subject.Resolve<Calendar>())
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain(nameof(Calendar)));

	[Fact]
	public Task WhenValidationFailsThenIsResolvableIsFalse()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Needy>().AddSingleton<Calendar>())
			.When(x => x.Subject.IsResolvable<Calendar>())
			.Then(resolvable => resolvable.Should().BeFalse());

	[Fact]
	public Task WhenValidateOnBuildIsTurnedOffThenPartialGraphResolves()
		=> Context
			.Given(x => x.Subject.ValidateOnBuild = false)
			.Given(x => x.Subject.Services.AddSingleton<Needy>().AddSingleton<Calendar>())
			.When(x => x.Subject.Resolve<Calendar>())
			.Then(calendar => calendar.Should().NotBeNull());

	[Fact]
	public Task WhenValidationIsChangedAfterResolutionThenInvalidOperationIsThrown()
		=> Context
			.GivenClockRegistered()
			.GivenResolved<Clock>()
			.When(x => x.Subject.ValidateScopes = false)
			.ThenThrows<InvalidOperationException>();

	[Fact]
	public Task WhenDeferredGivenResolvesThenLaterImmediateRegistrationsStillApply()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Services.AddSingleton(x.Clock))
				.Given(s => s.Resolve<Clock>())
				.Deferred()
				.Given(s => s.Services.Override(x.OtherClock))
				.WhenResolving<Clock>()
				.Then(clock => x.Asserted = clock))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.OtherClock));

	[Fact]
	public Task WhenResolvingOnAChainThatIsNotAFixtureThenTheFailureNamesTheTarget()
		=> Context
			.Given()
			.WhenResolving<Clock>()
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain(nameof(ServiceFixture)).And.Contain(nameof(Fixture)));

	[Fact]
	public Task WhenResolvingServiceThenWhenResultIsTheInstance()
		=> Context
			.GivenChain(x => x.Subject.Given(s => s.Services.AddSingleton(x.Clock)).WhenResolving<Clock>().Then(clock => x.Asserted = clock))
			.WhenExecutingTheChain()
			.Then(x => x.Asserted.Should().BeSameAs(x.Clock));
}
