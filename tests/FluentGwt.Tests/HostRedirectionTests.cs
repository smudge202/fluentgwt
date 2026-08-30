using System.Net;
using AwesomeAssertions;
using FluentGwt.Tests.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class HostRedirectionTests
{
	[Fact]
	public Task WhenHostClientIsRedirectedToHostThenRequestReachesTheOtherHost()
		=> Context
			.Given(x => x.Publisher.ConfigureServices(services => services.RedirectHttp("webhooks").To(x.Subscriber)))
			.WhenPublishing("published")
			.Then(status => status.Should().Be(HttpStatusCode.OK))
			.AndFixture(x => x.Subscriber.Resolve<Journal>().Entries.Should().Contain("published"));

	[Fact]
	public Task WhenFixtureClientIsRedirectedToHostThenRequestReachesTheHost()
		=> Context
			.Given(x => x.Services.AddHttpClient<JournalClient>())
			.Given(x => x.Services.RedirectHttp<JournalClient>().To(x.Subscriber))
			.When((x, cancellationToken) => x.Resolve<JournalClient>().Write("direct", cancellationToken))
			.Then(status => status.Should().Be(HttpStatusCode.OK))
			.AndFixture(x => x.Subscriber.Resolve<Journal>().Entries.Should().Contain("direct"));

	[Fact]
	public Task WhenRedirectedToHostThenRequestKeepsItsPathAndQuery()
		=> Context
			.Given(x => x.Services.AddHttpClient<JournalClient>())
			.Given(x => x.Services.RedirectHttp<JournalClient>().To(x.Subscriber))
			.When((x, cancellationToken) => x.Resolve<JournalClient>().Read("/configuration/Greeting?ignored=true", cancellationToken))
			.Then(body => body.Should().Be("from subscriber"));

	[Fact]
	public Task WhenRedirectedToHostThenTheRequestIsRecorded()
		=> Context
			.Given(x => x.Services.AddHttpClient<JournalClient>())
			.Given(x => x.Services.RedirectHttp<JournalClient>().To(x.Subscriber))
			.When((x, cancellationToken) => x.Resolve<JournalClient>().Write("recorded", cancellationToken))
			.Then((x, _) => x.Http.Requests.Should().ContainSingle().Which.RequestUri!.AbsolutePath.Should().Be("/journal/recorded"));
}
