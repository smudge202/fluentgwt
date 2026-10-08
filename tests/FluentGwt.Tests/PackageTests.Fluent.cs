using Fixture = FluentGwt.Tests.PackageTests.Fixture;

namespace FluentGwt.Tests;

internal static class PackageTestsFluent
{
	public static Given<Fixture> GivenThePackedSolution(this Fixture fixture, string properties = "")
		=> fixture.Given((x, cancellationToken) => x.Pack(properties, cancellationToken));

	public static Given<Fixture> GivenTheCorePackagePackedAsCiDoes(this Fixture fixture)
		=> fixture.Given((x, cancellationToken) => x.PackCoreAsCiDoes(cancellationToken));
}
