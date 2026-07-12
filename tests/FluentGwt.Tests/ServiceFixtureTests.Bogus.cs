using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenTwoFixturesUseTheSameSeedThenGeneratedValuesAreIdentical()
		=> Context
			.Given()
			.When(_ => (Generate(new DeclaredSeedFixture()), Generate(new DeclaredSeedFixture())))
			.Then(values => values.Item1.Should().Be(values.Item2));

	[Fact]
	public Task WhenTestsRunInAnyOrderThenEachTestsValuesDependOnlyOnItsSeed()
		=> Context
			.Given(x => Generate(x.Subject))
			.Given(x => Generate(x.Other))
			.When(_ => (Generate(new DeclaredSeedFixture()), Generate(new DeclaredSeedFixture())))
			.Then(values => values.Item1.Should().Be(values.Item2));

	[Fact]
	public Task WhenSeedsDifferThenGeneratedValuesDiffer()
		=> Context
			.Given()
			.When(_ => (Generate(new DeclaredSeedFixture()), Generate(new FirstSharedSeedFixture())))
			.Then(values => values.Item1.Should().NotBe(values.Item2));

	[Fact]
	public Task WhenRandomIsUsedThenItIsTheFakersRandomizer()
		=> Context
			.Given()
			.When(x => (x.Subject.Random, x.Subject.Fake.Random))
			.Then(randomizers => randomizers.Item1.Should().BeSameAs(randomizers.Item2));

	[Fact]
	public Task WhenFakeIsReadTwiceThenItIsTheSameInstance()
		=> Context
			.Given()
			.When(x => (x.Subject.Fake, x.Subject.Fake))
			.Then(fakes => fakes.Item1.Should().BeSameAs(fakes.Item2));

	[Fact]
	public Task WhenLocaleIsNotConfiguredThenFakerIsBritishEnglish()
		=> Context
			.Given()
			.When(x => x.Subject.Fake.Locale)
			.Then(locale => locale.Should().Be("en_GB"));

	[Fact]
	public Task WhenLocaleIsConfiguredThenFakerUsesIt()
		=> Context
			.Given(x => x.Subject.Configure("FluentGwt:Locale", "fr"))
			.When(x => x.Subject.Fake.Locale)
			.Then(locale => locale.Should().Be("fr"));

	private static string Generate(ServiceFixture fixture) =>
		$"{fixture.Random.Int()} {fixture.Fake.Name.FullName()} {fixture.Fake.Commerce.Ean13()}";
}
