using static FluentGwt.Tests.SignInTests;

namespace FluentGwt.Tests;

internal static class SignInTestsFluent
{
	public static Given<Fixture> GivenStubbedAuthentication(this Fixture fixture)
		=> fixture.Given(x => x.Api.Authentication());

	public static When<Fixture, Reply> WhenGetting(this Given<Fixture> given, params string[] paths)
		=> given.When(async (x, cancellationToken) =>
		{
			using var client = x.Api.CreateClient();
			Reply? last = null;
			foreach (var path in paths)
			{
				using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
				last = new(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
			}
			return last!;
		});
}
