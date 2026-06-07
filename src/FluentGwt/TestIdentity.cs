using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FluentGwt;

internal static class TestIdentity
{
	private const string _Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

	public static string Derive(int seed, string identity)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{seed}|{identity}")));
		var bits = BinaryPrimitives.ReadUInt64BigEndian(hash);
		return string.Create(12, bits, (characters, value) =>
		{
			for (var index = 0; index < characters.Length; index++)
				characters[index] = _Alphabet[(int)((value >> (59 - (5 * index))) & 31)];
		});
	}
}
