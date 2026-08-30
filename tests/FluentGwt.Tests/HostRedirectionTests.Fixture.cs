using System.Net;
using FluentGwt.Tests.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt.Tests;

public sealed partial class HostRedirectionTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Fixture()
		{
			Subscriber = ApplicationHost.For<Program>(this).Configure("Greeting", "from subscriber");
			Publisher = ApplicationHost.Composed(
				this,
				"publisher",
				builder => builder.Services.AddHttpClient("webhooks"),
				app => app.MapPost("/publish/{entry}", async (string entry, IHttpClientFactory clients, CancellationToken cancellationToken) =>
				{
					using var response = await clients.CreateClient("webhooks").PostAsync(new Uri($"https://subscriber.test/journal/{entry}"), null, cancellationToken);
					return (int)response.StatusCode;
				}));
		}

		public ApplicationHost Subscriber { get; }
		public ApplicationHost Publisher { get; }
	}

	internal sealed class JournalClient(HttpClient http)
	{
		public async Task<HttpStatusCode> Write(string entry, CancellationToken cancellationToken)
		{
			using var response = await http.PostAsync(new Uri($"https://subscriber.test/journal/{entry}"), null, cancellationToken);
			return response.StatusCode;
		}

		public Task<string> Read(string pathAndQuery, CancellationToken cancellationToken) =>
			http.GetStringAsync(new Uri($"https://subscriber.test{pathAndQuery}"), cancellationToken);
	}
}
