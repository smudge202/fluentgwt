using AwesomeAssertions;
using FluentGwt.Tests.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class TestHostTests
{
	[Fact]
	public Task WhenScopeIsUsedAfterStartThenItResolvesApplicationServices()
		=> Context
			.Given()
			.When(x => x.Api.Scope((services, _) => Task.FromResult(services.GetRequiredService<Greeter>().Greet())))
			.Then(greeting => greeting.Should().Be("Hello"));

	[Fact]
	public Task WhenScopeIsUsedBeforeStartThenTheHostStartsOnFirstUse()
		=> Context
			.Given(x => x.Seen = x.Api.Resolve<Greeter>().Greet())
			.When(x => x.Seen)
			.Then(greeting => greeting.Should().Be("Hello"));

	[Fact]
	public Task WhenScopeCompletesThenScopedServicesAreDisposed()
		=> Context
			.Given(x => x.Api.ConfigureServices(services => services.AddScoped<ScopedProbe>()))
			.When(x => x.Api.Scope((services, _) => Task.FromResult(services.GetRequiredService<ScopedProbe>())))
			.Then(probe => probe.Disposed.Should().BeTrue());

	[Fact]
	public Task WhenScopeIsGivenWorkThenItReceivesTheFixtureCancellation()
		=> Context
			.Given()
			.When(x => x.Api.Scope((_, cancellationToken) => Task.FromResult(cancellationToken)))
			.Then((x, token) => token.Should().Be(x.Cancellation));

	[Fact]
	public Task WhenDeferredGivenSeedsThroughHostThenActSeesTheData()
		=> Context
			.Given(x => x.Api.Scope(services => services.GetRequiredService<Journal>().Entries.Enqueue("seeded")))
			.Deferred()
			.WhenGetting(x => x.Api, "/journal")
			.Then(reply => reply.Body.Split(',').Should().Contain("seeded"));

	[Fact]
	public Task WhenAssertionReadsThroughHostThenItSeesActWrites()
		=> Context
			.Given()
			.When(async (x, cancellationToken) =>
			{
				using var client = x.Api.CreateClient();
				using var response = await client.PostAsync(new Uri("/journal/written", UriKind.Relative), null, cancellationToken);
			})
			.Then(x => x.Api.Resolve<Journal>().Entries.Should().Contain("written"));

	internal sealed class ScopedProbe : IDisposable
	{
		public bool Disposed { get; private set; }

		public void Dispose() => Disposed = true;
	}
}
