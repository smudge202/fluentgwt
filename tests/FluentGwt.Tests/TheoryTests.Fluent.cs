using Fixture = FluentGwt.Tests.TheoryTests.Fixture;
using Part = FluentGwt.Tests.TheoryTests.Part;

namespace FluentGwt.Tests;

internal static class TheoryTestsFluent
{
	public static When<Fixture, Part> WhenGivingTheRowAndReadingItsState(this Given<Fixture> given, FixtureRow<Part> row)
		=> given.When(async x =>
		{
			var chain = x.Given(row);
			await chain.When(_ => { }).Then(_ => { });
			return chain.Get<Part>();
		});
}
