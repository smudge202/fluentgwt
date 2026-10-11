using static FluentGwt.Tests.KeptSchemeTests;

namespace FluentGwt.Tests;

internal static class KeptSchemeTestsFluent
{
	public static Given<Fixture> GivenAStubbedUserKeeping(this Fixture fixture, params string[] schemes)
		=> fixture.Given(x =>
		{
			var authentication = x.Api.Authentication().AuthenticateAs();
			foreach (var scheme in schemes)
				authentication.Keep(scheme);
		});

	public static When<Fixture, Reply> WhenGetting(this Given<Fixture> given, string path, string? apiKey = null)
		=> given.When(async (x, cancellationToken) =>
		{
			using var client = x.Api.CreateClient();
			using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
			if (apiKey is not null)
				request.Headers.Add(ApiKeyHandler.Header, apiKey);
			using var response = await client.SendAsync(request, cancellationToken);
			return new Reply(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
		});
}
