using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenChainSucceedsThenProviderIsDisposed()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Services.AddSingleton(_ => new Disposable(s.Log)))
				.When(s => s.Resolve<Disposable>())
				.Then(service => x.Asserted = service))
			.WhenExecutingTheChain()
			.Then(x => ((Disposable)x.Asserted!).Disposed.Should().BeTrue());

	[Fact]
	public Task WhenAssertionFailsThenProviderIsStillDisposed()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Services.AddSingleton(_ => new Disposable(s.Log)))
				.When(s => s.Resolve<Disposable>())
				.Then(service =>
				{
					x.Asserted = service;
					throw new InvalidOperationException();
				}))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => ((Disposable)x.Asserted!).Disposed.Should().BeTrue());

	[Fact]
	public Task WhenResultAssertionCompletesThenFixtureIsDisposed()
		=> Context
			.GivenChain(x => x.Subject.Given(s => s.Services.AddSingleton(x.Clock)).WhenResolving<Clock>().Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Disposed.Should().BeTrue());

	[Fact]
	public Task WhenActWithoutResultCompletesThenFixtureIsDisposed()
		=> Context
			.GivenChain(x => x.Subject.Given().When(_ => { }).Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Disposed.Should().BeTrue());

	[Fact]
	public Task WhenExpectedExceptionIsThrownThenFixtureIsDisposed()
		=> Context
			.GivenChain(x => x.Subject.Given().When(_ => throw new InvalidOperationException()).ThenThrows<InvalidOperationException>())
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Disposed.Should().BeTrue());

	[Fact]
	public Task WhenServiceIsAsyncDisposableThenItIsDisposedAsynchronously()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Services.AddSingleton<AsyncOnly>())
				.When(s => s.Resolve<AsyncOnly>())
				.Then(service => x.Asserted = service))
			.WhenExecutingTheChain()
			.Then(x => ((AsyncOnly)x.Asserted!).Disposed.Should().BeTrue());

	[Fact]
	public Task WhenFixtureOverridesDisposeThenItRunsAfterProviderDisposal()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Services.AddSingleton(_ => new Disposable(s.Log)))
				.When(s => s.Resolve<Disposable>())
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Log.Should().Equal("service", "fixture"));
}
