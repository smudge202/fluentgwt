using Microsoft.Extensions.DependencyInjection;
using static FluentGwt.Tests.ServiceFixtureTests;

namespace FluentGwt.Tests;

internal static class ServiceFixtureTestsFluent
{
	public static Given<Fixture> GivenClockRegistered(this Fixture fixture)
		=> fixture.Given(x => x.Subject.Services.AddSingleton(x.Clock));

	public static Given<Fixture> GivenClockRegistered(this Given<Fixture> given)
		=> given.Given(x => x.Subject.Services.AddSingleton(x.Clock));

	public static Given<Fixture> GivenResolved<Service>(this Given<Fixture> given) where Service : notnull
		=> given.Given(x => x.Subject.Resolve<Service>());

	public static Given<Fixture> GivenChain(this Fixture fixture, Func<Fixture, Task> chain)
		=> fixture.Given(x => x.Chain = () => chain(x));

	public static When<Fixture> WhenExecutingTheChain(this Given<Fixture> given)
		=> given.When(x => x.Chain());

	public static When<Fixture> WhenExecutingTheChainCapturingFailure(this Given<Fixture> given)
		=> given.When(async x =>
		{
			var execution = x.Chain();
			await execution.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			x.Failure = execution.Exception?.InnerException;
		});

	public static Given<Fixture> GivenSubjectResolutionWillWaitAtAGate(this Given<Fixture> given)
		=> given.Given(x => x.Subject.Services.AddSingleton(_ => new Gate(x.Entered, x.Signal.Task)));

	public static When<Fixture, bool> WhenOtherFixtureResolvesWhileSubjectIsResolving(this Given<Fixture> given)
		=> given.When(async x =>
		{
			var gated = Task.Run(() => x.Subject.Resolve<Gate>());
			await x.Entered.Task;
			await Task.Run(() => x.Other.Resolve<Clock>());
			x.Signal.SetResult();
			return (await gated).Opened;
		});
}
