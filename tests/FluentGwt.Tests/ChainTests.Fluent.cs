using Fixture = FluentGwt.Tests.ChainTests.Fixture;

namespace FluentGwt.Tests;

internal static class ChainTestsFluent
{
	public static Given<Fixture> GivenChain(this Fixture fixture, Func<Fixture, Task> chain)
		=> fixture.Given(x => x.Chain = () => chain(x));

	public static Given<Fixture> GivenUnawaitedChain(this Fixture fixture, Func<Fixture, object> chain)
		=> fixture.Given(x => x.Unawaited = () => chain(x));

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

	public static When<Fixture> WhenBuildingTheChainWithoutAwaitingIt(this Given<Fixture> given)
		=> given.When(x => { x.Unawaited(); });
}
