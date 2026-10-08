using Fixture = FluentGwt.Tests.GivenTests.Fixture;

namespace FluentGwt.Tests;

internal static class GivenTestsFluent
{
	public static When<Fixture> WhenExecuting<Target>(this Given<Fixture> given, Func<Fixture, GivenBase<Target>> chain)
		=> given.When(async x => await chain(x).When(_ => { }).Then(_ => { }));
}
