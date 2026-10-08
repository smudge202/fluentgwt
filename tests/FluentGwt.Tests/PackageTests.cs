using System.IO.Compression;
using System.Xml.Linq;
using AwesomeAssertions;

namespace FluentGwt.Tests;

public sealed partial class PackageTests
{
	private const IntegrationJustification PacksTheSolution = IntegrationJustification.DiskIo | IntegrationJustification.MultipleThreads;
	private const string PacksTheSolutionReason = "Packs the whole solution with the .NET SDK and reads the packages";

	private static readonly string[] _Packages = ["FluentGwt", "FluentGwt.AspNetCore", "FluentGwt.Bogus", "FluentGwt.Http", "FluentGwt.Moq", "FluentGwt.Xunit"];

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenSolutionIsPackedThenOnlyTheLibraryPackagesAreProduced()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.Select(p => p.Id).Order(StringComparer.Ordinal).ToArray())
			.Then(ids => ids.Should().Equal(_Packages));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenLibraryIsPackedThenEveryPackageCarriesLicenceReadmeRepositoryAndSymbols()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages)
			.Then(packages => packages.Should().AllSatisfy(package =>
			{
				package.Licence.Should().Be("Apache-2.0");
				package.Readme.Should().Be("README.md");
				package.Files.Should().Contain("README.md");
				package.RepositoryUrl.Should().Be("https://github.com/smudge202/fluentgwt");
				package.RepositoryCommit.Should().MatchRegex("^[0-9a-f]{40}$");
				package.HasSymbols.Should().BeTrue();
			}));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenCorePackageIsPackedThenItCarriesTheAnalysersAndTheirFixes()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.Single(p => p.Id == "FluentGwt").Files)
			.Then(files => files.Should().Contain(["analyzers/dotnet/cs/FluentGwt.Analysers.dll", "analyzers/dotnet/cs/FluentGwt.Analysers.CodeFixes.dll"]));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenCoreIsPackedInReleaseInTheDefaultLayoutThenItCarriesTheAnalysers()
		=> Context
			.GivenTheCorePackagePackedAsCiDoes()
			.When(x => x.Packages.Single(p => p.Id == "FluentGwt").Files)
			.Then(files => files.Should().Contain(["analyzers/dotnet/cs/FluentGwt.Analysers.dll", "analyzers/dotnet/cs/FluentGwt.Analysers.CodeFixes.dll"]));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenSolutionIsPackedWithoutAReleaseTagThenItIsAnAlphaPrerelease()
		=> Context
			.GivenThePackedSolution()
			.When(x => x.Packages.Select(p => p.Version).Distinct().ToArray())
			.Then(versions => versions.Should().ContainSingle().Which.Should().Contain("-alpha."));

	internal sealed record Package(string Id, string Version, string? Licence, string? Readme, string? RepositoryUrl, string? RepositoryCommit, IReadOnlyList<string> Files, bool HasSymbols)
	{
		public static Package Read(string path)
		{
			using var archive = ZipFile.OpenRead(path);
			var nuspec = archive.Entries.Single(x => x.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
			using var stream = nuspec.Open();
			var metadata = XDocument.Load(stream).Root!.Elements().Single(x => x.Name.LocalName == "metadata");
			string? Value(string name) => metadata.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
			var repository = metadata.Elements().FirstOrDefault(x => x.Name.LocalName == "repository");
			return new(
				Value("id")!,
				Value("version")!,
				Value("license"),
				Value("readme"),
				repository?.Attribute("url")?.Value,
				repository?.Attribute("commit")?.Value,
				[.. archive.Entries.Select(x => x.FullName)],
				File.Exists(Path.ChangeExtension(path, ".snupkg")));
		}
	}
}
