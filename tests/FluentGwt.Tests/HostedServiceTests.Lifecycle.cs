using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class HostedServiceTests
{
	[Fact]
	public Task WhenMarkerIsPresentThenFixtureReportsIntegrationEnabled()
		=> Context
			.Given()
			.When(x => x.Integrated.IntegrationEnabled)
			.Then(enabled => enabled.Should().BeTrue());

	[Fact]
	public Task WhenMarkerIsAbsentThenFixtureReportsIntegrationDisabled()
		=> Context
			.Given()
			.When(x => x.Plain.IntegrationEnabled)
			.Then(enabled => enabled.Should().BeFalse());

	[Fact]
	public Task WhenMarkerIsPresentThenHostedServicesStartBeforeTheAct()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.When(f => f.Log.Add("act"))
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().StartWith(["start a", "act"]));

	[Fact]
	public Task WhenMarkerIsPresentThenHostedServicesStopAfterAssertions()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.When(f => f.Log.Add("act"))
				.Then(f => f.Log.Add("assert")))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().Equal("start a", "act", "assert", "stop a"));

	[Fact]
	public Task WhenMarkerIsPresentThenHostedServicesStopInReverseOrder()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(new Recording("a", f.Log)))
				.Given(f => f.Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(new Recording("b", f.Log)))
				.When(_ => { })
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().Equal("start a", "start b", "stop b", "stop a"));

	[Fact]
	public Task WhenMarkerIsAbsentThenHostedServicesAreNotStarted()
		=> Context
			.GivenChain(x => x.Plain
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.When(f => f.Log.Add("act"))
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Plain.Log.Should().Equal("act"));

	[Fact]
	public Task WhenAssertionFailsThenHostedServicesAreStillStopped()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.When(_ => { })
				.Then(_ => throw x.StartFailure))
			.WhenExecutingTheChainCapturingFailure()
			.Then(x => x.Integrated.Log.Should().Equal("start a", "stop a"));

	[Fact]
	public Task WhenHostedServiceFailsToStartThenTestFailsAsArrangement()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new FailingToStart(x.StartFailure)))
				.When(f => f.Log.Add("act"))
				.ThenArrangementFails<InvalidOperationException>(e => e.Should().BeSameAs(x.StartFailure)))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().BeEmpty());

	[Fact]
	public Task WhenLaterHostedServiceFailsToStartThenEarlierOnesAreStopped()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.Given(f => f.Services.AddHostedService(_ => new FailingToStart(x.StartFailure)))
				.When(_ => { })
				.ThenArrangementFails<InvalidOperationException>())
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().Equal("start a", "stop a"));

	[Fact]
	public Task WhenLifecycleServiceIsRegisteredThenAllLifecycleCallsAreMade()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Lifecycle(f.Log)))
				.When(f => f.Log.Add("act"))
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().Equal("starting", "start", "started", "act", "stopping", "stop", "stopped"));

	[Fact]
	public Task WhenHostedServiceIsRemovedFromFixtureServicesThenPhaseFourDoesNotStartIt()
		=> Context
			.GivenChain(x => x.Integrated
				.Given(f => f.Services.AddHostedService(_ => new Recording("a", f.Log)))
				.Given(f => f.Services.RemoveHostedService<Recording>())
				.When(f => f.Log.Add("act"))
				.Then(_ => { }))
			.WhenExecutingTheChain()
			.Then(x => x.Integrated.Log.Should().Equal("act"));
}
