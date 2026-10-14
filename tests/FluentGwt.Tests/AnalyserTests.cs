using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class AnalyserTests
{
	[Fact]
	public Task WhenFixturePropertyIsExpressionBodiedThenFG0001IsReported()
		=> Context
			.GivenSource("public sealed class OrderTests { private OrderFixture Context => new(); }")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0001"));

	[Fact]
	public Task WhenFixturePropertyHasASetterThenFG0001IsReported()
		=> Context
			.GivenSource("public sealed class OrderTests { private OrderFixture Context { get; set; } = new(); }")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0001"));

	[Fact]
	public Task WhenFixturePropertyIsGetterOnlyWithNewInitialiserThenNothingIsReported()
		=> Context
			.GivenSource("public sealed class OrderTests { private OrderFixture Context { get; } = new(); private OrderFixture Other { get; } = new OrderFixture(); }")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenFG0001CodeFixIsAppliedThenPropertyBecomesGetterOnlyWithInitialiser()
		=> Context
			.GivenSource("public sealed class OrderTests { private OrderFixture Context => new(); }")
			.WhenFixing("FG0001")
			.Then(source => source.Should().Be("public sealed class OrderTests { private OrderFixture Context { get; } = new(); }"));

	[Fact]
	public Task WhenChainIsAnExpressionStatementThenFG0002IsReported()
		=> Context
			.GivenSource("""
				public sealed class OrderTests
				{
					private OrderFixture Context { get; } = new();
					public void WhenPlacedThenAccepted() { Context.Given().When(_ => { }).Then(_ => { }); }
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0002"));

	[Fact]
	public Task WhenChainIsTheBodyOfAVoidMemberThenFG0002IsReported()
		=> Context
			.GivenSource("""
				public sealed class OrderTests
				{
					private OrderFixture Context { get; } = new();
					public void WhenPlacedThenAccepted() => Context.Given().When(_ => { }).Then(_ => { });
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0002"));

	[Fact]
	public Task WhenChainIsAssignedAndNeverAwaitedThenFG0002IsReported()
		=> Context
			.GivenSource("""
				public sealed class OrderTests
				{
					private OrderFixture Context { get; } = new();
					public void WhenPlacedThenAccepted() { var chain = Context.Given().When(_ => { }).Then(_ => { }); }
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0002"));

	[Fact]
	public Task WhenChainIsReturnedOrAwaitedThenNothingIsReported()
		=> Context
			.GivenSource("""
				public sealed class OrderTests
				{
					private OrderFixture Context { get; } = new();
					public Task WhenReturned() => Context.Given().When(_ => { }).Then(_ => { });
					public async Task WhenAwaited() { await Context.Given().When(_ => { }).Then(_ => { }); }
					public async Task WhenAwaitedLater() { var chain = Context.Given().When(_ => { }).Then(_ => { }); await chain; }
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenGivenIsAStatementThenNothingIsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenTwice(this Given<OrderFixture> given)
					{
						given.Given(_ => { });
						return given;
					}
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenResolvingGivenPrecedesRegistrationWithoutDeferralThenFG0003IsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenEmptyStore(this OrderFixture fixture)
						=> fixture.Given(x => x.Resolve<object>()).Given(x => x.Services.Clear());
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0003"));

	[Fact]
	public Task WhenResolvingGivenIsDeferredThenNothingIsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenEmptyStore(this OrderFixture fixture)
						=> fixture.Given(x => x.Resolve<object>()).Deferred().Given(x => x.Services.Clear());
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenResolvingGivenFollowsEveryRegistrationThenNothingIsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenEmptyStore(this OrderFixture fixture)
						=> fixture.Given(x => x.Services.Clear()).Given(x => x.Resolve<object>());
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenImmediateGivenUsesRunningHostThenFG0003IsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenSeeded(this OrderFixture fixture)
						=> fixture.Given(x => x.Api.Resolve<object>());
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0003"));

	[Fact]
	public Task WhenFG0003CodeFixIsAppliedThenDeferredIsAppended()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenEmptyStore(this OrderFixture fixture)
						=> fixture.Given(x => x.Resolve<object>()).Given(x => x.Services.Clear());
				}
				""")
			.WhenFixing("FG0003")
			.Then(source => source.Should().Contain("fixture.Given(x => x.Resolve<object>()).Deferred().Given(x => x.Services.Clear())"));

	[Fact]
	public Task WhenAsyncLambdaIsPassedAsActionToALibraryMethodThenFG0004IsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenRegistered(this OrderFixture fixture)
						=> fixture.Given(x => x.Api.ConfigureServices(async services => await Task.Yield()));
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0004"));

	[Fact]
	public Task WhenAsyncAnonymousMethodIsPassedAsActionToALibraryMethodThenFG0004IsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenRegistered(this OrderFixture fixture)
						=> fixture.Given(x => x.Api.ConfigureServices(async delegate { await Task.Yield(); }));
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0004"));

	[Fact]
	public Task WhenAsyncVoidMethodGroupIsPassedAsActionToALibraryMethodThenFG0004IsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenRegistered(this OrderFixture fixture)
						=> fixture.Given(x => x.Api.ConfigureServices(Register));

					private static async void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services) => await Task.Yield();
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().Equal("FG0004"));

	[Fact]
	public Task WhenAsyncLambdaBindsAnAwaitedOverloadThenNothingIsReported()
		=> Context
			.GivenSource("""
				public sealed class OrderTests
				{
					private OrderFixture Context { get; } = new();

					public Task WhenPlacedThenAccepted()
						=> Context
							.Given(async x => await Task.Yield())
							.When(async x => { await Task.Yield(); return 1; })
							.Then(async (x, result) => await Task.Yield())
							.And(async (x, result) => await Task.Yield())
							.AndFixture(async x => await Task.Yield());

					public Task WhenRejectedThenFixtureSeesIt()
						=> Context.Given().When(x => 1).ThenFixture(async x => await Task.Yield());

					public Task WhenFailedThenExceptionIsInspected()
						=> Context.Given().When(x => 1).ThenThrows<InvalidOperationException>(async (x, e) => await Task.Yield());

					public Task WhenSeededThenScopeIsAwaited()
						=> Context.Given().When(x => x.Api.Scope(async services => await Task.Yield())).Then(x => { });
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenSyncLambdaIsPassedAsActionToALibraryMethodThenNothingIsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenRegistered(this OrderFixture fixture)
						=> fixture.Given(x => x.Api.ConfigureServices(services => { }));
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenAsyncLambdaIsPassedAsActionOutsideTheLibraryThenNothingIsReported()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static void Visit(System.Collections.Generic.List<int> orders) => orders.ForEach(async order => await Task.Yield());
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());

	[Fact]
	public Task WhenStepMethodIsCalledFromChainThenItIsNotAnalysedThrough()
		=> Context
			.GivenSource("""
				public static class OrderSteps
				{
					public static Given<OrderFixture> GivenEmptyStore(this OrderFixture fixture)
						=> fixture.Given(x => Load(x)).Given(x => x.Services.Clear());

					private static void Load(OrderFixture fixture) => fixture.Resolve<object>();
				}
				""")
			.WhenAnalysing()
			.Then(ids => ids.Should().BeEmpty());
}
