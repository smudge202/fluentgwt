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

	[Fact(Timeout = 10_000)]
	public Task WhenStepWaitsOnFixtureTimeThenTestFailsNamingTheWayOut()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Configure(FrozenTimeLimit, "0.2"))
				.Given(s => s.Services.AddSingleton<Poller>())
				.When(s => s.Resolve<Poller>().Poll(TestContext.Current.CancellationToken))
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeOfType<TimeoutException>()
				.Which.Message.Should().Contain("x.Time.Advance").And.Contain("TimeProvider.System").And.Contain(FrozenTimeLimit));

	[Fact]
	public Task WhenStepAdvancesFixtureTimeThenTheWaitCompletes()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Configure(FrozenTimeLimit, "0.2"))
				.Given(s => s.Services.AddSingleton<Poller>())
				.When(async (s, cancellationToken) =>
				{
					var poll = s.Resolve<Poller>().Poll(cancellationToken);
					s.Time.Advance(Poller.Interval);
					return await poll;
				})
				.Then(result => result.Should().Be(1)))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull());

	[Fact]
	public Task WhenTimerFromAnEarlierStepIsPendingThenARealWaitIsNotFailed()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Configure(FrozenTimeLimit, "0.1"))
				.Given(s => x.Fired = s.Time.CreateTimer(_ => { }, null, TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan))
				.When((_, cancellationToken) => Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken))
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull());

	[Fact]
	public Task WhenFrozenTimeLimitIsZeroThenAStepWaitingWithAPendingTimerIsNotFailed()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Configure(FrozenTimeLimit, "0"))
				.When(async (s, cancellationToken) =>
				{
					using var timer = s.Time.CreateTimer(_ => { }, null, TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
					await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
				})
				.Then(_ => { }))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Failure.Should().BeNull());
}
