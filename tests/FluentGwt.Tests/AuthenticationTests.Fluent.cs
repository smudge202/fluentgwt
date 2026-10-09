using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using static FluentGwt.Tests.AuthenticationTests;

namespace FluentGwt.Tests;

internal static class AuthenticationTestsFluent
{
	public static Given<Fixture> GivenStubbedAuthentication(this Fixture fixture)
		=> fixture.Given(x => x.Api.Authentication());

	public static Given<Fixture> GivenTheIdentityProviderIsWatched(this Given<Fixture> given)
		=> given.Given(x => x.Api.ConfigureServices(services =>
			services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => options.BackchannelHttpHandler = x.Backchannel)));

	public static When<Fixture, Reply> WhenGetting(this Given<Fixture> given, string path, string? bearer = null)
		=> given.When((x, cancellationToken) => Get(x.Api, path, bearer, cancellationToken));

	public static async Task<Reply> Get(ApplicationHost host, string path, string? bearer, CancellationToken cancellationToken)
	{
		using var client = host.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
		if (bearer is not null)
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
		using var response = await client.SendAsync(request, cancellationToken);
		return new(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken), response.Headers.Contains("X-Result-Handler"));
	}
}
