using Fixture = FluentGwt.Tests.BuildTargetTests.Fixture;

namespace FluentGwt.Tests;

internal static class BuildTargetTestsFluent
{
	private static readonly string[] _Packed = ["FluentGwt", "FluentGwt.Xunit"];

	private const string Program = """
		using System.Reflection;

		var assembly = Assembly.GetEntryAssembly()!;
		Console.WriteLine(string.Join(' ', new[]
		{
		#if INTEGRATION
			"INTEGRATION",
		#endif
		#if DEBUG
			"DEBUG",
		#endif
		#if TRACE
			"TRACE",
		#endif
		#if NET10_0
			"NET10_0",
		#endif
			$"MARKER={assembly.IsDefined(typeof(FluentGwt.IntegrationEnabledAttribute))}",
			$"RUNNER={assembly.IsDefined(typeof(FluentGwt.XunitTestRunnerAttribute))}",
		}));
		""";

	public static Given<Fixture> GivenSampleImportingTheTargets(this Fixture fixture)
		=> fixture.Given(async (x, cancellationToken) =>
		{
			x.Sample = x.Scratch();
			var xunit = Path.Combine(x.Repository, "src", "FluentGwt.Xunit");
			await File.WriteAllTextAsync(Path.Combine(x.Sample, "Program.cs"), Program, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(x.Sample, "Sample.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
						<ImplicitUsings>enable</ImplicitUsings>
					</PropertyGroup>
					<ItemGroup>
						<ProjectReference Include="{Path.Combine(xunit, "FluentGwt.Xunit.csproj")}" />
					</ItemGroup>
					<Import Project="{Path.Combine(xunit, "buildTransitive", "FluentGwt.Xunit.targets")}" />
				</Project>
				""", cancellationToken);
		});

	public static Given<Fixture> GivenPackedLibraryReferencedThroughAHelperLibrary(this Fixture fixture)
		=> fixture.Given(async (x, cancellationToken) =>
		{
			var root = x.Scratch();
			var feed = Path.Combine(root, "feed");
			var version = $"0.0.0-build.{x.TestId}";
			x.Environment["NUGET_PACKAGES"] = Path.Combine(root, "packages");
			foreach (var project in _Packed)
				await x.Dotnet(x.Repository, $"pack src/{project}/{project}.csproj -o \"{feed}\" -p:Version={version} -p:TreatWarningsAsErrors=false", cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(root, "nuget.config"), $"""
				<configuration>
					<packageSources>
						<clear />
						<add key="local" value="{feed}" />
						<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
					</packageSources>
				</configuration>
				""", cancellationToken);
			var helpers = Path.Combine(root, "Helpers");
			Directory.CreateDirectory(helpers);
			await File.WriteAllTextAsync(Path.Combine(helpers, "Helpers.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<TargetFramework>net10.0</TargetFramework>
					</PropertyGroup>
					<ItemGroup>
						<PackageReference Include="FluentGwt.Xunit" Version="{version}" />
					</ItemGroup>
				</Project>
				""", cancellationToken);
			x.Sample = Path.Combine(root, "App");
			Directory.CreateDirectory(x.Sample);
			await File.WriteAllTextAsync(Path.Combine(x.Sample, "Program.cs"), Program, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(x.Sample, "App.csproj"), """
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<OutputType>Exe</OutputType>
						<TargetFramework>net10.0</TargetFramework>
						<ImplicitUsings>enable</ImplicitUsings>
					</PropertyGroup>
					<ItemGroup>
						<ProjectReference Include="../Helpers/Helpers.csproj" />
					</ItemGroup>
				</Project>
				""", cancellationToken);
		});

	public static When<Fixture, string> WhenBuildingAndRunning(this Given<Fixture> given, string properties = "")
		=> given.When((x, cancellationToken) => x.Dotnet(x.Sample, $"run {properties}", cancellationToken));
}
