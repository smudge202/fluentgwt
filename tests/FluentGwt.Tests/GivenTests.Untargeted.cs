using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class GivenTests
{
	[Fact]
	public Task WhenBlankUntargetedChainIsCreatedThenItExists()
		=> Context
			.Given()
			.When(_ => new Given())
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainStartsWithStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainStartsWithNamedStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(x.Name, x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainStartsWithKeyedStateThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(x.Key, x.State))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainStartsWithATransitionThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(() => x.Subject.Part = x.First))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainStartsWithAnAsyncTransitionThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(async () => x.Subject.Part = await Subject.NewPartLater()))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedChainIsGivenStateThenItCanBeRead()
		=> Context
			.Given()
			.When(x => Given.With(x.Subject).Get<Subject>())
			.Then((x, state) => state.Should().BeSameAs(x.Subject));

	[Fact]
	public Task WhenUntargetedChainIsGivenStateAgainThenItIsReplaced()
		=> Context
			.Given()
			.When(x => Given.With(x.First).Given(x.Second).Get<Part>())
			.Then((x, state) => state.Should().BeSameAs(x.Second));

	[Fact]
	public Task WhenUntargetedChainIsGivenANullNameThenArgumentNullIsThrown()
		=> Context
			.Given()
			.When(x => Given.With((string)null!, x.State))
			.ThenThrows<ArgumentNullException>();

	[Fact]
	public Task WhenUntargetedChainIsGivenNamedStateThenItCanBeReadByName()
		=> Context
			.Given()
			.When(x => Given.With(x.Name, x.Subject).Get<Subject>(x.Name))
			.Then((x, state) => state.Should().BeSameAs(x.Subject));

	[Fact]
	public Task WhenUntargetedChainIsGivenNamedStateAgainThenItIsReplaced()
		=> Context
			.Given()
			.When(x => Given.With(x.Name, x.First).Given(x.Name, x.Second).Get<Part>(x.Name))
			.Then((x, state) => state.Should().BeSameAs(x.Second));

	[Fact]
	public Task WhenUntargetedChainIsGivenANullKeyThenArgumentNullIsThrown()
		=> Context
			.Given()
			.When(x => Given.With((object)null!, x.State))
			.ThenThrows<ArgumentNullException>();

	[Fact]
	public Task WhenUntargetedChainIsGivenKeyedStateThenItCanBeReadByKey()
		=> Context
			.Given()
			.When(x => Given.With(x.Key, x.Subject).Get<Subject>(x.Key))
			.Then((x, state) => state.Should().BeSameAs(x.Subject));

	[Fact]
	public Task WhenUntargetedChainIsGivenKeyedStateAgainThenItIsReplaced()
		=> Context
			.Given()
			.When(x => Given.With(x.Key, x.First).Given(x.Key, x.Second).Get<Part>(x.Key))
			.Then((x, state) => state.Should().BeSameAs(x.Second));

	[Fact]
	public Task WhenBlankUntargetedChainExecutesThenItCompletes()
		=> Context
			.Given()
			.WhenExecuting(_ => new Given())
			.Then(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenUntargetedChainIsGivenATransitionThenItDoesNotRunUntilExecution()
		=> Context
			.Given()
			.When(x => Given.With(() => x.Subject.Part = x.First))
			.ThenFixture(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenUntargetedChainIsGivenAnAsyncTransitionThenItDoesNotRunUntilExecution()
		=> Context
			.Given()
			.When(x => Given.With(async () => x.Subject.Part = await Subject.NewPartLater()))
			.ThenFixture(x => x.Subject.Part.Should().BeNull());

	[Fact]
	public Task WhenUntargetedChainExecutesThenItsTransitionRuns()
		=> Context
			.Given()
			.WhenExecuting(x => Given.With(() => x.Subject.Part = x.First))
			.Then(x => x.Subject.Part.Should().BeSameAs(x.First));

	[Fact]
	public Task WhenUntargetedTransitionIsExtendedThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(() => x.Subject.Part = x.First).Given(() => x.Subject.Part!.Value = x.Value))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedAsyncTransitionIsExtendedThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(async () => x.Subject.Part = await Subject.NewPartLater()).Given(() => x.Subject.Part!.Value = x.Value))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedTransitionIsExtendedAsynchronouslyThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(() => x.Subject.Part = x.First).Given(async () => x.Subject.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenUntargetedAsyncTransitionIsExtendedAsynchronouslyThenAChainIsReturned()
		=> Context
			.Given()
			.When(x => Given.With(async () => x.Subject.Part = await Subject.NewPartLater()).Given(async () => x.Subject.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(chain => chain.Should().NotBeNull());

	[Fact]
	public Task WhenExtendedUntargetedChainExecutesThenTransitionsRunInOrder()
		=> Context
			.Given()
			.WhenExecuting(x => Given.With(() => x.Subject.Part = x.First).Given(() => x.Subject.Part!.Value = x.Value))
			.Then(x => x.Subject.Part!.Value.Should().Be(x.Value));

	[Fact]
	public Task WhenExtendedAsyncUntargetedChainExecutesThenTransitionsRunInOrder()
		=> Context
			.Given()
			.WhenExecuting(x => Given.With(async () => x.Subject.Part = await Subject.NewPartLater()).Given(async () => x.Subject.Part!.Value = await Subject.ValueLater(x.Value)))
			.Then(x => x.Subject.Part!.Value.Should().Be(x.Value));
}
