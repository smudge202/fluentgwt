using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenFixtureIsCreatedThenTimeStartsAtSeededInstant()
		=> Context
			.Given()
			.When(_ => new DeclaredSeedFixture().Time.GetUtcNow())
			.Then(now => now.Should().Be(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(DeclaredSeedFixture.DeclaredSeed)));

	[Fact]
	public Task WhenProductInjectsTimeProviderThenItReceivesTheFakeProvider()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<Stamper>())
			.When(x => x.Subject.Resolve<Stamper>().Time)
			.Then((x, time) => time.Should().BeSameAs(x.Subject.Time));

	[Fact]
	public Task WhenProductRegistersSystemTimeThenItReceivesTheFakeProvider()
		=> Context
			.Given(x => x.Subject.Services.TryAddSingleton(TimeProvider.System))
			.Given(x => x.Subject.Services.AddSingleton<Stamper>())
			.When(x => x.Subject.Resolve<Stamper>().Time)
			.Then((x, time) => time.Should().BeSameAs(x.Subject.Time));

	[Fact]
	public Task WhenTestRegistersItsOwnTimeProviderThenItWins()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<TimeProvider>(x.OwnTime).AddSingleton<Stamper>())
			.When(x => x.Subject.Resolve<Stamper>().Time)
			.Then((x, time) => time.Should().BeSameAs(x.OwnTime));

	[Fact]
	public Task WhenTestRegistersSystemTimeThroughAFactoryThenItWins()
		=> Context
			.Given(x => x.Subject.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System).AddSingleton<Stamper>())
			.When(x => x.Subject.Resolve<Stamper>().Time)
			.Then(time => time.Should().BeSameAs(TimeProvider.System));

	[Fact]
	public Task WhenTimeIsAdvancedThenTimersFire()
		=> Context
			.Given(x => x.Fired = x.Subject.Time.CreateTimer(_ => x.Asserted = true, null, TimeSpan.FromMinutes(15), Timeout.InfiniteTimeSpan))
			.When(x => x.Subject.Time.Advance(TimeSpan.FromMinutes(16)))
			.Then(x => x.Asserted.Should().Be(true));
}
