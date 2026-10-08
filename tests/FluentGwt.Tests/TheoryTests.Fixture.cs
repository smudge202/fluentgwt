using Microsoft.Extensions.Configuration;

namespace FluentGwt.Tests;

public sealed partial class TheoryTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		public Part First { get; } = new();
		public Part Second { get; } = new();
		public IConfiguration ForcingSeed99 { get; } = new ConfigurationBuilder()
			.AddInMemoryCollection([new("FluentGwtSeed", "99")])
			.Build();
		public object? Received { get; set; }
		public Part? Evaluated { get; set; }
	}

	public sealed class Part;
}
