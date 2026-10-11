using static FluentGwt.Tests.ClientUserTests;

namespace FluentGwt.Tests;

internal static class ClientUserTestsFluent
{
	public static Given<Fixture> GivenStubbedAuthentication(this Fixture fixture)
		=> fixture.Given(x => x.Api.Authentication());

	public static When<Fixture, (string Alice, string Bob)> WhenAliceAndBobEachGet(this Given<Fixture> given, string path)
		=> given.When(async (x, cancellationToken) =>
		{
			using var alice = x.Api.CreateClient(user => user.Claim(Fixture.User, "alice"));
			using var bob = x.Api.CreateClient(user => user.Claim(Fixture.User, "bob"));
			var seen = await Task.WhenAll(
				alice.GetStringAsync(new Uri(path, UriKind.Relative), cancellationToken),
				bob.GetStringAsync(new Uri(path, UriKind.Relative), cancellationToken));
			return (seen[0], seen[1]);
		});
}
