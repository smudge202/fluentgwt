using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class GivenTests
{
	[Fact]
	public Task WhenTargetIsGivenThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given())
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenNamedStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Name, x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenKeyedStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Key, x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenATransitionThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(s => s.Part = x.First))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenAnAsyncTransitionThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(async s => s.Part = await Subject.NewPartLater()))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenThenItIsTheDefaultState()
		=> Context
			.Given()
			.When(x => x.Subject.Given().Get<Subject>())
			.Then((x, target) => target.Should().BeSameAs(x.Subject));

	[Fact]
	public Task WhenTargetIsGivenStateThenItCanBeRead()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.State).Get<object>())
			.Then((x, state) => state.Should().BeSameAs(x.State));

	[Fact]
	public Task WhenTargetIsGivenStateOfItsOwnTypeThenInvalidOperationIsThrown()
		=> Context
			.Given()
			.When(x => x.First.Given(x.Second))
			.ThenThrows<InvalidOperationException>();

	[Fact]
	public Task WhenTargetIsGivenANullNameThenArgumentNullIsThrown()
		=> Context
			.Given()
			.When(x => x.Subject.Given((string)null!, x.State))
			.ThenThrows<ArgumentNullException>();

	[Fact]
	public Task WhenTargetIsGivenNamedStateThenItCanBeReadByName()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Name, x.State).Get<object>(x.Name))
			.Then((x, state) => state.Should().BeSameAs(x.State));

	[Fact]
	public Task WhenTargetIsGivenNamedStateAgainThenItIsReplaced()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Name, x.First).Given(x.Name, x.Second).Get<Part>(x.Name))
			.Then((x, state) => state.Should().BeSameAs(x.Second));

	[Fact]
	public Task WhenTargetIsGivenANullKeyThenArgumentNullIsThrown()
		=> Context
			.Given()
			.When(x => x.Subject.Given((object)null!, x.State))
			.ThenThrows<ArgumentNullException>();

	[Fact]
	public Task WhenTargetIsGivenKeyedStateThenItCanBeReadByKey()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Key, x.State).Get<object>(x.Key))
			.Then((x, state) => state.Should().BeSameAs(x.State));

	[Fact]
	public Task WhenTargetIsGivenKeyedStateAgainThenItIsReplaced()
		=> Context
			.Given()
			.When(x => x.Subject.Given(x.Key, x.First).Given(x.Key, x.Second).Get<Part>(x.Key))
			.Then((x, state) => state.Should().BeSameAs(x.Second));

	[Fact]
	public Task WhenTargetedChainWithoutTransitionsExecutesThenItCompletes()
		=> Context
			.Given()
			.WhenExecuting(x => x.Subject.Given())
			.Then(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenTargetIsGivenATransitionThenItDoesNotRunUntilExecution()
		=> Context
			.Given()
			.When(x => x.Subject.Given(s => s.Part = x.First))
			.ThenFixture(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenTargetIsGivenAnAsyncTransitionThenItDoesNotRunUntilExecution()
		=> Context
			.Given()
			.When(x => x.Subject.Given(async s => s.Part = await Subject.NewPartLater()))
			.ThenFixture(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenTargetedChainExecutesThenItsTransitionRuns()
		=> Context
			.Given()
			.WhenExecuting(x => x.Subject.Given(s => s.Part = x.First))
			.Then(x => x.Subject.Part.Should().BeSameAs(x.First));

	[Fact]
	public Task WhenTargetedTransitionIsExtendedThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(s => s.Part = x.First).Given(s => s.Part!.Value = x.Value))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetedAsyncTransitionIsExtendedThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(async s => s.Part = await Subject.NewPartLater()).Given(s => s.Part!.Value = x.Value))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetedTransitionIsExtendedAsynchronouslyThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(s => s.Part = x.First).Given(async s => s.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetedAsyncTransitionIsExtendedAsynchronouslyThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => x.Subject.Given(async s => s.Part = await Subject.NewPartLater()).Given(async s => s.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenTargetIsGivenStateAfterATransitionThenTheTransitionStillRuns()
		=> Context
			.Given()
			.WhenExecuting(x => x.Subject.Given(s => s.Part = x.First).Given(x.State))
			.Then(x => x.Subject.Part.Should().BeSameAs(x.First));

	[Fact]
	public Task WhenTargetIsGivenStateAfterATransitionThenTheChainKeepsItsTarget()
		=> Context
			.Given()
			.When(x => x.Subject.Given(s => s.Part = x.First).Given(x.Name, x.State).Get<Subject>())
			.Then((x, target) => target.Should().BeSameAs(x.Subject));

	[Fact]
	public Task WhenExtendedTargetedChainExecutesThenTransitionsRunInOrder()
		=> Context
			.Given()
			.WhenExecuting(x => x.Subject.Given(s => s.Part = x.First).Given(s => s.Part!.Value = x.Value))
			.Then(x => x.Subject.Part!.Value.Should().Be(x.Value));

	[Fact]
	public Task WhenExtendedAsyncTargetedChainExecutesThenTransitionsRunInOrder()
		=> Context
			.Given()
			.WhenExecuting(x => x.Subject.Given(async s => s.Part = await Subject.NewPartLater()).Given(async s => s.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(x => x.Subject.Part!.Value.Should().Be(x.Value));
}
