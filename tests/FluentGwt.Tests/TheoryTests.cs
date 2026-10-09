using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class TheoryTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public Task WhenTheoryParameterIsUsedInStepThenEachRowSeesItsValue(int row)
		=> Context
			.Given(x => x.Received = row)
			.When(x => x.Received)
			.Then(received => received.Should().Be(row));

	[Theory]
	[MemberData(nameof(Parts))]
	public Task WhenFixtureRowIsGivenThenItIsEvaluatedAgainstThisTestsFixture(FixtureRow<Part> part)
		=> Context
			.Given(part, (x, value) => x.Received = value)
			.When(x => x.Received)
			.Then((x, received) => received.Should().BeOneOf(x.First, x.Second));

	[Fact]
	public Task WhenFixtureRowIsGivenThenEvaluatedValueIsAvailableAsState()
		=> Context
			.Given()
			.WhenGivingTheRowAndReadingItsState(FixtureRow<Part>.For<Fixture>("first", x => x.First))
			.Then((x, state) => state.Should().BeSameAs(x.First));

	[Fact]
	public Task WhenFixtureRowIsGivenThenItIsEvaluatedAfterEarlierGivens()
		=> Context
			.Given(x => x.Received = x.Second)
			.Given(FixtureRow<Part>.For<Fixture>("received", x => (Part)x.Received!), (x, value) => x.Evaluated = value)
			.When(x => x.Evaluated)
			.Then((x, evaluated) => evaluated.Should().BeSameAs(x.Second));

	[Theory]
	[MemberData(nameof(Parts))]
	public Task WhenFixtureDataHasLabelsThenRowsDisplayByLabel(FixtureRow<Part> part)
		=> Context
			.Given()
			.When(_ => TestContext.Current.Test!.TestDisplayName)
			.Then(name => name.Should().EndWith($"(part: {part.Label})"));

	[Fact]
	public Task WhenTheoryDataIsEnumeratedTwiceThenRowsAreIdentical()
		=> Context
			.Given()
			.When(_ => (GeneratedRows().ToArray(), GeneratedRows().ToArray()))
			.Then(rows => rows.Item1.Should().Equal(rows.Item2));

	[Fact]
	public Task WhenProviderSeedIsOverriddenByConfigurationThenRowsFollowIt()
		=> Context
			.Given()
			.When(x => TheoryRandom.Create(7, x.ForcingSeed99).Next())
			.Then(value => value.Should().Be(new Random(99).Next()));

	[Fact]
	public Task WhenRowWrittenForAnotherFixtureIsGivenThenArrangementFailsNamingBoth()
		=> Context
			.Given(FixtureRow<Part>.For<string>("elsewhere", _ => new Part()), (x, value) => x.Evaluated = value)
			.When(_ => { })
			.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain("String").And.Contain(nameof(Fixture)));
}
