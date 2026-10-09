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
	public Task WhenSatellitePackagesDependOnCoreThenItsAnalysersFlowThrough()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.Where(p => p.Id != "FluentGwt").ToDictionary(p => p.Id, p => p.Dependencies["FluentGwt"]))
			.Then(excludes => excludes.Should().HaveCount(5).And.AllSatisfy(x => (x.Value ?? string.Empty).Should().NotContain("Analyzers")));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenLibraryIsPackedThenItsAuthorIsTommyLong()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.Select(p => p.Authors).Distinct().ToArray())
			.Then(authors => authors.Should().Equal("Tommy Long"));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenLibraryIsPackedThenEachPackageCarriesTagsOfItsOwn()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.ToDictionary(p => p.Id, p => p.Tags))
			.Then(tags => tags.Should().Satisfy(
				x => x.Key == "FluentGwt" && !x.Value.Contains("xunit") && !x.Value.Contains("aspnetcore"),
				x => x.Key == "FluentGwt.AspNetCore" && x.Value.Contains("aspnetcore"),
				x => x.Key == "FluentGwt.Bogus" && x.Value.Contains("bogus"),
				x => x.Key == "FluentGwt.Http" && x.Value.Contains("httpclient"),
				x => x.Key == "FluentGwt.Moq" && x.Value.Contains("moq"),
				x => x.Key == "FluentGwt.Xunit" && x.Value.Contains("xunit")));

	[IntegrationFact(PacksTheSolution, PacksTheSolutionReason)]
	public Task WhenLibraryIsPackedThenItsReadmeDoesNotSayItIsUnpublished()
		=> Context
			.GivenThePackedSolution("-p:MinVerVersionOverride=1.2.3")
			.When(x => x.Packages.Single(p => p.Id == "FluentGwt").ReadmeText)
			.Then(readme => readme.Should().NotContain("Not yet published"));

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

	internal sealed record Package(
		string Id,
		string Version,
		string? Authors,
		string[] Tags,
		string? Licence,
		string? Readme,
		string ReadmeText,
		string? RepositoryUrl,
		string? RepositoryCommit,
		IReadOnlyDictionary<string, string?> Dependencies,
		IReadOnlyList<string> Files,
		bool HasSymbols)
	{
		public static Package Read(string path)
		{
			using var archive = ZipFile.OpenRead(path);
			var nuspec = archive.Entries.Single(x => x.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
			using var stream = nuspec.Open();
			var metadata = XDocument.Load(stream).Root!.Elements().Single(x => x.Name.LocalName == "metadata");
			string? Value(string name) => metadata.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
			var repository = metadata.Elements().FirstOrDefault(x => x.Name.LocalName == "repository");
			var dependencies = metadata.Descendants().Where(x => x.Name.LocalName == "dependency")
				.GroupBy(x => x.Attribute("id")!.Value)
				.ToDictionary(x => x.Key, x => x.First().Attribute("exclude")?.Value);
			var readme = archive.GetEntry("README.md");
			using var readmeReader = readme is null ? null : new StreamReader(readme.Open());
			return new(
				Value("id")!,
				Value("version")!,
				Value("authors"),
				(Value("tags") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries),
				Value("license"),
				Value("readme"),
				readmeReader?.ReadToEnd() ?? string.Empty,
				repository?.Attribute("url")?.Value,
				repository?.Attribute("commit")?.Value,
				dependencies,
				[.. archive.Entries.Select(x => x.FullName)],
				File.Exists(Path.ChangeExtension(path, ".snupkg")));
		}
	}
}
