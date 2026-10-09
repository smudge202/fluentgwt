using System.Net;

namespace FluentGwt.Tests;

public sealed partial class HttpTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Queue<HttpStatusCode> Upstream { get; } = [];
		public TeapotHandler Teapot { get; } = new();

		protected override ValueTask DisposeFixture()
		{
			Teapot.Dispose();
			return ValueTask.CompletedTask;
		}
	}

	internal sealed class ForecastClient(HttpClient http)
	{
		public static Uri Forecast { get; } = new("https://upstream.test/forecast");

		public async Task<HttpStatusCode> Get(CancellationToken cancellationToken)
		{
			using var response = await http.GetAsync(Forecast, cancellationToken);
			return response.StatusCode;
		}
	}

	internal interface Weather
	{
		Task<HttpStatusCode> Today(CancellationToken cancellationToken);
	}

	internal sealed class HttpWeather(HttpClient http) : Weather
	{
		public async Task<HttpStatusCode> Today(CancellationToken cancellationToken)
		{
			using var response = await http.GetAsync(ForecastClient.Forecast, cancellationToken);
			return response.StatusCode;
		}
	}

	internal sealed class StampingHandler : DelegatingHandler
	{
		public const string Header = "X-Stamp";

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			request.Headers.Add(Header, "stamped");
			return base.SendAsync(request, cancellationToken);
		}
	}

	internal sealed class TeapotHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage((HttpStatusCode)418));
	}
}
