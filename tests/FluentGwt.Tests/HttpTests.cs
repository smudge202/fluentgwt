using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class HttpTests
{
	[Fact]
	public Task WhenAllClientsAreRedirectedThenEveryClientReceivesTheStubResponse()
		=> Context
			.GivenNamedClients("first", "second")
			.Given(x => x.Services.RedirectHttp().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.WhenEachNamedClientGets("first", "second")
			.Then(statuses => statuses.Should().Equal(HttpStatusCode.Accepted, HttpStatusCode.Accepted));

	[Fact]
	public Task WhenNamedClientIsRedirectedThenOtherClientsAreUntouched()
		=> Context
			.GivenNamedClients("first")
			.Given(x => x.Services.AddHttpClient("second").ConfigurePrimaryHttpMessageHandler(() => new TeapotHandler()))
			.Given(x => x.Services.RedirectHttp("first").RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.WhenEachNamedClientGets("first", "second")
			.Then(statuses => statuses.Should().Equal(HttpStatusCode.Accepted, (HttpStatusCode)418));

	[Fact]
	public Task WhenTypedClientIsRedirectedThenItsDelegatingHandlersStillRun()
		=> Context
			.Given(x => x.Services.AddHttpClient<ForecastClient>().AddHttpMessageHandler(() => new StampingHandler()))
			.Given(x => x.Services.RedirectHttp<ForecastClient>().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.OK)))
			.WhenFetchingTheForecast()
			.Then((x, _) => x.Http.Requests.Should().ContainSingle().Which.Headers.Contains(StampingHandler.Header).Should().BeTrue());

	[Fact]
	public Task WhenResilienceIsConfiguredThenRedirectionSitsBeneathIt()
		=> Context
			.Given(x => x.Services.AddHttpClient<ForecastClient>().AddStandardResilienceHandler(o => o.Retry.Delay = TimeSpan.Zero))
			.Given(x => x.Services.RedirectHttp<ForecastClient>().RespondingWith(_ => new HttpResponseMessage(x.Upstream.Dequeue())))
			.Given(x => x.Upstream.Enqueue(HttpStatusCode.ServiceUnavailable))
			.Given(x => x.Upstream.Enqueue(HttpStatusCode.OK))
			.WhenFetchingTheForecast()
			.Then(status => status.Should().Be(HttpStatusCode.OK))
			.AndFixture(x => x.Http.Requests.Should().HaveCount(2));

	[Fact]
	public Task WhenRequestIsRedirectedThenItIsRecorded()
		=> Context
			.Given(x => x.Services.AddHttpClient<ForecastClient>())
			.Given(x => x.Services.RedirectHttp().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.OK)))
			.WhenFetchingTheForecast()
			.Then((x, _) => x.Http.Requests.Should().ContainSingle().Which.RequestUri.Should().Be(ForecastClient.Forecast));

	[Fact]
	public Task WhenRedirectedToAStubHandlerThenTheHandlerAnswers()
		=> Context
			.Given(x => x.Services.AddHttpClient<ForecastClient>())
			.Given(x => x.Services.RedirectHttp<ForecastClient>().Via(x.Teapot))
			.WhenFetchingTheForecast()
			.Then(status => status.Should().Be((HttpStatusCode)418));

	[Fact]
	public Task WhenNoResponseMatchesThenTestFailsNamingTheRequest()
		=> Context
			.Given(x => x.Services.AddHttpClient<ForecastClient>())
			.Given(x => x.Services.RedirectHttp().RespondingWith(_ => null))
			.WhenFetchingTheForecast()
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain($"GET {ForecastClient.Forecast}"));

	[Fact]
	public Task WhenAbstractionOfTypedClientIsRedirectedThenTheStubAnswers()
		=> Context
			.Given(x => x.Services.AddHttpClient<Weather, HttpWeather>())
			.Given(x => x.Services.RedirectHttp<Weather>().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.WhenFetchingTheWeather()
			.Then(status => status.Should().Be(HttpStatusCode.Accepted));

	[Fact]
	public Task WhenImplementationOfTypedClientIsRedirectedThenTestFailsNamingTheAbstraction()
		=> Context
			.Given(x => x.Services.AddHttpClient<Weather, HttpWeather>())
			.Given(x => x.Services.RedirectHttp<HttpWeather>().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.WhenFetchingTheWeather()
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain("RedirectHttp<HttpWeather>()").And.Contain("RedirectHttp<Weather>()"))
			.And(x => x.Http.Requests.Should().BeEmpty());

	[Fact]
	public Task WhenRedirectedClientIsNeverRegisteredThenTestFailsNamingIt()
		=> Context
			.GivenNamedClients("first")
			.Given(x => x.Services.RedirectHttp<ForecastClient>().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.WhenEachNamedClientGets("first")
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain("RedirectHttp<ForecastClient>()"));

	[Fact]
	public Task WhenProductSetsItsOwnPrimaryHandlerAfterwardsThenRedirectionStillWins()
		=> Context
			.Given(x => x.Services.RedirectHttp<ForecastClient>().RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.Accepted)))
			.Given(x => x.Services.AddHttpClient<ForecastClient>().ConfigurePrimaryHttpMessageHandler(() => new TeapotHandler()))
			.WhenFetchingTheForecast()
			.Then(status => status.Should().Be(HttpStatusCode.Accepted));
}
