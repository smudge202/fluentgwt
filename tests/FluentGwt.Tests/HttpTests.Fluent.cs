using System.Net;
using Microsoft.Extensions.DependencyInjection;
using static FluentGwt.Tests.HttpTests;

namespace FluentGwt.Tests;

internal static class HttpTestsFluent
{
	public static Given<Fixture> GivenNamedClients(this Fixture fixture, params string[] names)
		=> fixture.Given(x =>
		{
			foreach (var name in names)
				x.Services.AddHttpClient(name);
		});

	public static When<Fixture, HttpStatusCode[]> WhenEachNamedClientGets(this Given<Fixture> given, params string[] names)
		=> given.When(async (x, cancellationToken) =>
		{
			var factory = x.Resolve<IHttpClientFactory>();
			var statuses = new List<HttpStatusCode>();
			foreach (var name in names)
			{
				using var response = await factory.CreateClient(name).GetAsync(ForecastClient.Forecast, cancellationToken);
				statuses.Add(response.StatusCode);
			}
			return statuses.ToArray();
		});

	public static When<Fixture, HttpStatusCode> WhenFetchingTheForecast(this Given<Fixture> given)
		=> given.When((x, cancellationToken) => x.Resolve<ForecastClient>().Get(cancellationToken));
}
