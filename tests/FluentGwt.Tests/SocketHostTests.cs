using System.Net;
using AwesomeAssertions;
using FluentGwt.Tests.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class SocketHostTests
{
	[Fact]
	public Task WhenHostRunsOnSocketsThenAddressIsLoopbackWithBoundPort()
		=> Context
			.Given()
			.When(x => x.Relay.Address)
			.Then(address => address.Should().Match<Uri>(a => a.Scheme == "https" && a.Host == "127.0.0.1" && a.Port > 0));

	[Fact]
	public Task WhenHostRunsOnSocketsThenItsClientIsServed()
		=> Context
			.Given()
			.When((x, cancellationToken) => x.Relay.CreateClient().GetStringAsync(new Uri("/greeting", UriKind.Relative), cancellationToken))
			.Then(greeting => greeting.Should().Be("Hello"));

	[Fact]
	public Task WhenHostRunsOnSocketsThenClientTrustsOnlyItsCertificate()
		=> Context
			.Given()
			.When((x, cancellationToken) => x.Relay.CreateClient().GetStringAsync(new Uri(x.Other.Address, "/greeting"), cancellationToken))
			.ThenThrows<HttpRequestException>();

	[Fact]
	public Task WhenAnUnpinnedClientCallsASocketHostThenItsCertificateIsRefused()
		=> Context
			.Given()
			.When(async (x, cancellationToken) =>
			{
				using var plain = new HttpClient();
				return await plain.GetStringAsync(new Uri(x.Relay.Address, "/greeting"), cancellationToken);
			})
			.ThenThrows<HttpRequestException>();

	[Fact]
	public Task WhenHttpTwoIsSelectedThenRequestsUseHttpTwo()
		=> Context
			.Given(x => x.Relay.WithProtocols(HttpProtocols.Http1AndHttp2))
			.When(async (x, cancellationToken) =>
			{
				using var response = await x.Relay.CreateClient().GetAsync(new Uri("/greeting", UriKind.Relative), cancellationToken);
				return response.Version;
			})
			.Then(version => version.Should().Be(HttpVersion.Version20));

	[Fact]
	public Task WhenWebSocketIsOpenedOnSocketsThenItConnectsToTheHost()
		=> Context
			.Given()
			.When((x, cancellationToken) => SocketHostTestsFluent.Echo(x.Relay, "over sockets", cancellationToken))
			.Then(echo => echo.Should().Be("over sockets"));

	[Fact]
	public Task WhenWebSocketIsOpenedInMemoryThenItConnectsToTheHost()
		=> Context
			.Given()
			.When((x, cancellationToken) => SocketHostTestsFluent.Echo(x.InMemory, "in memory", cancellationToken))
			.Then(echo => echo.Should().Be("in memory"));

	[Fact]
	public Task WhenTwoSocketHostsRunInParallelThenPortsDiffer()
		=> Context
			.Given()
			.When(x => (x.Relay.Address.Port, x.Other.Address.Port))
			.Then(ports => ports.Item1.Should().NotBe(ports.Item2));

	[Fact]
	public Task WhenHostRunsOnSocketsThenItIsServedByTheFactorysKestrelMode()
		=> Context
			.Given()
			.When(x => x.Relay.Resolve<IServer>().GetType().Name)
			.Then(server => server.Should().Contain("Kestrel"));

	[Fact]
	public Task WhenComposedHostIsPutOnSocketsThenArrangementFailsExplainingWhy()
		=> Context
			.Given(x => x.Composed.OnSockets())
			.When(_ => { })
			.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain("composed").And.Contain("entry point"));

	[Fact]
	public Task WhenFixtureClientIsRedirectedToASocketHostThenRequestReachesIt()
		=> Context
			.Given(x => x.Services.AddHttpClient("relay"))
			.Given(x => x.Services.RedirectHttp("relay").To(x.Relay))
			.When((x, cancellationToken) => x.Resolve<IHttpClientFactory>().CreateClient("relay").GetStringAsync(new Uri("https://relay.test/greeting"), cancellationToken))
			.Then(greeting => greeting.Should().Be("Hello"));
}
