using Xunit;

namespace FluentGwt.Tests;

public sealed partial class TheoryTests
{
	public static TheoryData<FixtureRow<Part>> Parts => new FixtureData<Fixture, Part>
	{
		{ "first", x => x.First },
		{ "second", x => x.Second },
	};

	private static IEnumerable<int> GeneratedRows()
	{
		var random = TheoryRandom.Create(20261008);
		for (var row = 0; row < 5; row++)
			yield return random.Next();
	}
}
