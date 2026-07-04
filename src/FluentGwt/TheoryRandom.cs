using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace FluentGwt;

public static class TheoryRandom
{
	public static Random Create(int seed) =>
		Create(seed, new TestConfiguration(Assembly.GetEntryAssembly() ?? typeof(TheoryRandom).Assembly).Root);

	[SuppressMessage("Security", "CA5394", Justification = "Theory rows need randomness that a seed reproduces, which is what a cryptographic source cannot give.")]
	public static Random Create(int seed, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		return new(ForcedSeed.From(configuration) ?? seed);
	}
}
