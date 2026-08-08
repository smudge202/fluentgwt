using System.Net;
using AwesomeAssertions;
using FluentGwt.Tests.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class TestHostTests
{
	[Fact]
	public Task WhenEntryPointHostIsDeclaredThenRequestsReachTheRealPipeline()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/greeting")
			.Then(reply => reply.Body.Should().Be("Hello"));

	[Fact]
	public Task WhenComposedHostIsDeclaredThenRequestsReachItsEndpoints()
		=> Context
			.Given()
			.WhenGetting(x => x.Composed, "/greeting")
			.Then(reply => reply.Body.Should().Be("Hello"));

	[Fact]
	public Task WhenTwoHostsAreDeclaredThenEachHasItsOwnAddress()
		=> Context
			.Given()
			.When(x => (x.Api.Address, x.Composed.Address))
			.Then(addresses => addresses.Should().Be((new Uri("http://program/"), new Uri("http://composed/"))));

	[Fact]
	public Task WhenTwoTestsDeclareTheSameHostThenEachGetsItsOwnInstance()
		=> Context
			.Given(x => x.Api.Get("/count", TestContext.Current.CancellationToken))
			.When(async (_, cancellationToken) =>
			{
				var other = new Fixture();
				var count = string.Empty;
				await other.Given().When(async (y, token) => count = (await y.Api.Get("/count", token)).Body).Then(_ => { });
				return count;
			})
			.Then(count => count.Should().Be("1"));

	[Fact]
	public Task WhenOverrideIsWrittenAfterHostDeclarationThenItStillApplies()
		=> Context
			.Given(x => x.Api.ConfigureServices(services => services.Override<Greeter>(new ShoutingGreeter())))
			.WhenGetting(x => x.Api, "/greeting")
			.Then(reply => reply.Body.Should().Be("HELLO"));

	[Fact]
	public Task WhenOverrideIsAttemptedAfterStartThenInvalidOperationIsThrown()
		=> Context
			.Given()
			.When(x => x.Api.Configure("Greeting", "late"))
			.ThenThrows<InvalidOperationException>(e => e.Message.Should().Contain("Program"));

	[Fact]
	public Task WhenApplicationHostedServiceFailsToStartThenTestFailsAsArrangement()
		=> Context
			.Given(x => x.Api.Configure("Web:FailOnStart", "true"))
			.When(_ => { })
			.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain("configured to fail on start"));

	[Fact]
	public Task WhenHostFailsToStartThenTestFailsWithTheStartupException()
		=> Context
			.Given(x => x.Composed.Pipeline(_ => throw new InvalidOperationException("Pipeline refused")))
			.When(_ => { })
			.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Be("Pipeline refused"));

	[Fact]
	public Task WhenClientIsCreatedThenRedirectsAreNotFollowed()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/redirect")
			.Then(reply => reply.Status.Should().Be(HttpStatusCode.Redirect));

	[Fact]
	public Task WhenChainEndsThenHostsAreDisposedInReverseOrder()
		=> Context
			.Given()
			.WhenAFreshFixtureRunsItsChain(JournalingBothHosts)
			.Then(journal => journal.Should().Equal("start api", "start composed", "stop composed", "stop api"));

	[Fact]
	public Task WhenAssertionFailsThenHostsAreStillDisposed()
		=> Context
			.Given()
			.WhenAFreshFixtureRunsItsChain(JournalingBothHosts, failing: true)
			.Then(journal => journal.Should().Contain(["stop composed", "stop api"]));

	[Fact]
	public Task WhenHostServiceIsOverriddenThenApplicationEnumerableContainsOnlyTheOverride()
		=> Context
			.Given(x => x.Api.ConfigureServices(services => services.AddSingleton<Greeter>(new ShoutingGreeter()).Override<Greeter>(new ShoutingGreeter())))
			.WhenGetting(x => x.Api, "/greetings")
			.Then(reply => reply.Body.Should().Be("1"));

	[Fact]
	public Task WhenHostConfigurationIsSetThenApplicationReadsIt()
		=> Context
			.Given(x => x.Api.Configure("Greeting", "configured"))
			.WhenGetting(x => x.Api, "/configuration/Greeting")
			.Then(reply => reply.Body.Should().Be("configured"));

	[Fact]
	public Task WhenComposedHostConfigurationIsSetThenApplicationReadsIt()
		=> Context
			.Given(x => x.Composed.Configure("Greeting", "configured"))
			.WhenGetting(x => x.Composed, "/configuration/Greeting")
			.Then(reply => reply.Body.Should().Be("configured"));

	[Fact]
	public Task WhenPipelineMiddlewareIsAddedThenItRunsForEveryRequest()
		=> Context
			.Given(x => x.Api.Pipeline(app => app.Use(async (context, next) =>
			{
				context.Response.Headers["X-Test"] = "seen";
				await next(context);
			})))
			.When(async (x, cancellationToken) => (await x.Api.Get("/greeting", cancellationToken), await x.Api.Get("/count", cancellationToken)))
			.Then(replies => replies.Should().Match<(Reply, Reply)>(r => r.Item1.Headers.Contains("X-Test") && r.Item2.Headers.Contains("X-Test")));

	[Fact]
	public Task WhenComposedPipelineMiddlewareIsAddedThenItRunsBeforeTheEndpoints()
		=> Context
			.Given(x => x.Composed.Pipeline(app => app.Use(async (context, next) =>
			{
				context.Response.Headers["X-Test"] = "seen";
				await next(context);
			})))
			.WhenGetting(x => x.Composed, "/greeting")
			.Then(reply => reply.Headers.Contains("X-Test").Should().BeTrue());

	[Fact]
	public Task WhenEnvironmentIsNotSetThenApplicationIsInDevelopment()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/environment")
			.Then(reply => reply.Body.Should().Be("Development"));

	[Fact]
	public Task WhenEnvironmentIsSetThenApplicationSeesIt()
		=> Context
			.Given(x => x.Api.Environment("Staging"))
			.WhenGetting(x => x.Api, "/environment")
			.Then(reply => reply.Body.Should().Be("Staging"));

	[Fact]
	public Task WhenTwoTestsOverrideTheSameServiceThenNeitherSeesTheOthers()
		=> Context
			.Given(x => x.Api.ConfigureServices(services => services.Override<Greeter>(new ShoutingGreeter())))
			.When(async (_, cancellationToken) =>
			{
				var other = new Fixture();
				var greeting = string.Empty;
				await other.Given().When(async (y, token) => greeting = (await y.Api.Get("/greeting", token)).Body).Then(_ => { });
				return greeting;
			})
			.Then(greeting => greeting.Should().Be("Hello"));

	[Fact]
	public Task WhenHostIsStartedThenItSharesTheFixtureTime()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/time")
			.Then((x, reply) => reply.Body.Should().Be(x.Time.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));

	private static Given<Fixture> JournalingBothHosts(Fixture fixture)
		=> fixture
			.Given(x => x.Api.Configure("Web:Name", "api").ConfigureServices(services => services.Override(x.Journal)))
			.Given(x => x.Composed.Configure("Web:Name", "composed").ConfigureServices(services => services.Override(x.Journal)));
}
