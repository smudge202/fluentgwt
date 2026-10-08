namespace FluentGwt.Tests;

public sealed partial class PackageTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		private readonly BuildTargetTests.Fixture _dotnet = new();

		public IReadOnlyList<Package> Packages { get; private set; } = [];

		public async Task Pack(string properties, CancellationToken cancellationToken)
		{
			var output = _dotnet.Scratch();
			await _dotnet.Dotnet(_dotnet.Repository, $"pack FluentGwt.slnx -o \"{output}\" --artifacts-path \"{Path.Combine(output, "artifacts")}\" -p:TreatWarningsAsErrors=false {properties}", cancellationToken);
			Packages = [.. Directory.GetFiles(output, "*.nupkg").Select(Package.Read)];
		}

		public async Task PackCoreAsCiDoes(CancellationToken cancellationToken)
		{
			var output = _dotnet.Scratch();
			await _dotnet.Dotnet(_dotnet.Repository, $"pack src/FluentGwt/FluentGwt.csproj -c Release -o \"{output}\" -p:TreatWarningsAsErrors=false", cancellationToken);
			Packages = [.. Directory.GetFiles(output, "*.nupkg").Select(Package.Read)];
		}

		protected override ValueTask DisposeFixture() => _dotnet.DisposeAsync();
	}
}
