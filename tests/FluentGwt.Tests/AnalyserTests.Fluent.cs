using Fixture = FluentGwt.Tests.AnalyserTests.Fixture;

namespace FluentGwt.Tests;

internal static class AnalyserTestsFluent
{
	public static Given<Fixture> GivenSource(this Fixture fixture, string source)
		=> fixture.Given(x => x.Source = source);

	public static When<Fixture, string[]> WhenAnalysing(this Given<Fixture> given)
		=> given.When((x, cancellationToken) => x.Diagnose(cancellationToken));

	public static When<Fixture, string> WhenFixing(this Given<Fixture> given, string id)
		=> given.When((x, cancellationToken) => x.Fix(id, cancellationToken));
}
