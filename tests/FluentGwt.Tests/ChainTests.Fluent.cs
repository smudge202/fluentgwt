using Fixture = FluentGwt.Tests.ChainTests.Fixture;

namespace FluentGwt.Tests;

internal static class ChainTestsFluent
{
	public static Given<Fixture> GivenChain(this Fixture fixture, Func<Fixture, Task> chain)
		=> fixture.Given(x => x.Chain = () => chain(x));

	public static Given<Fixture> GivenUnawaitedChain(this Fixture fixture, Func<Fixture, object> chain)
		=> fixture.Given(x => x.Unawaited = () => chain(x));

	public static Given<Fixture> GivenAwaitableChain(this Fixture fixture, Func<Fixture, Then<Probe>> chain)
		=> fixture.Given(x => x.Awaitable = () => chain(x));

	public static When<Fixture> WhenAwaitingTheChain(this Given<Fixture> given)
		=> given.When(async x => await x.Awaitable!());

	public static When<Fixture> WhenAwaitingTheChainTwice(this Given<Fixture> given)
		=> given.When(async x =>
		{
			var chain = x.Awaitable!();
			await chain;
			await chain;
		});

	public static When<Fixture> WhenExecutingTheChain(this Given<Fixture> given)
		=> given.When(x => x.Chain());

	public static When<Fixture> WhenExecutingTheChainCapturingFailure(this Given<Fixture> given)
		=> given.When(async x =>
		{
			try
			{
				await x.Chain();
			}
			catch (InvalidOperationException failure)
			{
				x.Failure = failure;
			}
		});

	public static When<Fixture> WhenExecutingTheChainCapturingCancellation(this Given<Fixture> given)
		=> given.When(async x =>
		{
			try
			{
				await x.Chain();
			}
			catch (OperationCanceledException cancellation)
			{
				x.Cancellation = cancellation;
			}
		});

	public static When<Fixture> WhenBuildingTheChainWithoutAwaitingIt(this Given<Fixture> given)
		=> given.When(x => { x.Unawaited(); });
}
