using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace FluentGwt;

internal static class ForcedSeed
{
	public static int? From(IConfiguration configuration)
	{
		var forced = configuration["FluentGwtSeed"];
		if (string.IsNullOrWhiteSpace(forced))
			return null;
		return int.TryParse(forced, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)
			? seed
			: throw new InvalidOperationException($"FluentGwtSeed forces the seed and must be an integer, but was '{forced}'.");
	}
}
