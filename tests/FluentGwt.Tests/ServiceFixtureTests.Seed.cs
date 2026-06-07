using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenSeedIsNotDeclaredThenEachRunUsesANewSeed()
		=> Context
			.Given()
			.When(x => (x.Subject.Seed, x.Other.Seed))
			.Then(seeds => seeds.Item1.Should().NotBe(seeds.Item2));

	[Fact]
	public Task WhenSeedIsDeclaredThenEveryRunUsesIt()
		=> Context
			.Given()
			.When(_ => (new DeclaredSeedFixture().Seed, new DeclaredSeedFixture().Seed))
			.Then(seeds => seeds.Should().Be((DeclaredSeedFixture.DeclaredSeed, DeclaredSeedFixture.DeclaredSeed)));

	[Fact]
	public Task WhenSeedIsDeclaredOnACommonBaseThenEveryDerivedFixtureUsesIt()
		=> Context
			.Given()
			.When(_ => (new FirstSharedSeedFixture().Seed, new SecondSharedSeedFixture().Seed))
			.Then(seeds => seeds.Should().Be((SharedSeedFixture.SharedSeed, SharedSeedFixture.SharedSeed)));

	[Fact]
	public Task WhenSeedIsForcedByConfigurationThenTestReplays()
		=> Context
			.Given(x => x.Subject.Configure("FluentGwtSeed", "1234567"))
			.When(x => x.Subject.Seed)
			.Then(seed => seed.Should().Be(1234567));

	[Fact]
	public Task WhenSeedIsForcedByConfigurationThenItOverridesADeclaredSeed()
		=> Context
			.Given()
			.When(_ =>
			{
				var fixture = new DeclaredSeedFixture();
				fixture.Configure("FluentGwtSeed", "1234567");
				return fixture.Seed;
			})
			.Then(seed => seed.Should().Be(1234567));

	[Fact]
	public Task WhenForcedSeedIsNotAnIntegerThenArrangementFailsNamingTheValue()
		=> Context
			.GivenChain(x => x.Subject
				.Given(s => s.Configure("FluentGwtSeed", "not-a-seed"))
				.When(_ => { })
				.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain("not-a-seed")))
			.WhenExecutingTheChain()
			.Then(x => x.Subject.Disposed.Should().BeTrue());

	[Fact]
	public Task WhenTestIdIsDerivedFromSameSeedAndTestThenItIsIdentical()
		=> Context
			.Given()
			.When(_ => (new DeclaredSeedFixture().TestId, new DeclaredSeedFixture().TestId))
			.Then(ids => ids.Item1.Should().Be(ids.Item2));

	[Fact]
	public Task WhenTwoTestsShareASeedThenTheirTestIdsDiffer()
		=> Context
			.Given()
			.When(_ => (new FirstSharedSeedFixture().TestId, new SecondSharedSeedFixture().TestId))
			.Then(ids => ids.Item1.Should().NotBe(ids.Item2));

	[Fact]
	public Task WhenTestIdIsDerivedInSeparateProcessesThenItIsTheSame()
		=> Context
			.Given()
			.When(_ => new DeclaredSeedFixture().TestId)
			.Then(id => id.Should().Be(DeclaredSeedFixture.PinnedTestId));

	[Fact]
	public Task WhenTestIdIsDerivedThenItIsTwelveLowercaseBase32Characters()
		=> Context
			.Given()
			.When(x => x.Subject.TestId)
			.Then(id => id.Should().MatchRegex("^[a-z2-7]{12}$"));
}
