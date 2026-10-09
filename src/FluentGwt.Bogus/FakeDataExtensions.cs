using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Bogus;

namespace FluentGwt;

[SuppressMessage("Design", "CA1034", Justification = "A C# 14 extension block compiles to a nested type; the rule predates extension members.")]
public static class FakeDataExtensions
{
	private static readonly ConditionalWeakTable<ServiceFixture, Faker> _Fakers = new();

	extension(ServiceFixture fixture)
	{
		public Faker Fake => _Fakers.GetValue(fixture, Create);

		public Randomizer Random => fixture.Fake.Random;
	}

	private static Faker Create(ServiceFixture fixture) =>
		new(fixture.Configuration["FluentGwt:Locale"] ?? "en_GB") { Random = new Randomizer(fixture.Seed) };
}
