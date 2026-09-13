using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FluentGwt.Tests;

public sealed partial class AnalyserTests
{
	private const string Preamble = """
		using System;
		using System.Threading.Tasks;
		using FluentGwt;

		public sealed class OrderFixture : ServiceFixture
		{
			public ApplicationHost Api => null!;
		}

		""";

	private Fixture Context { get; } = new();

	internal sealed class Fixture
	{
		private static readonly ImmutableArray<DiagnosticAnalyzer> _Analysers = [.. Create<DiagnosticAnalyzer>()];
		private static readonly ImmutableArray<CodeFixProvider> _Fixes = [.. Create<CodeFixProvider>(typeof(Analysers.CodeFixes.DeferralCodeFix))];
		private static readonly ImmutableArray<MetadataReference> _References = [.. References()];

		public string Source { get; set; } = string.Empty;

		public async Task<string[]> Diagnose(CancellationToken cancellationToken)
		{
			var compilation = Compile(Preamble + Source);
			var diagnostics = await compilation.WithAnalyzers(_Analysers).GetAnalyzerDiagnosticsAsync(cancellationToken);
			return [.. diagnostics.Select(x => x.Id).Order(StringComparer.Ordinal)];
		}

		public async Task<string> Fix(string id, CancellationToken cancellationToken)
		{
			using var workspace = new AdhocWorkspace();
			var document = workspace.CurrentSolution
				.AddProject("Analysed", "Analysed", LanguageNames.CSharp)
				.WithCompilationOptions(Options)
				.WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
				.AddMetadataReferences(_References)
				.AddDocument("Analysed.cs", Preamble + Source);
			var compilation = await document.Project.GetCompilationAsync(cancellationToken)
				?? throw new InvalidOperationException("The sample did not compile.");
			var diagnostic = (await compilation.WithAnalyzers(_Analysers).GetAnalyzerDiagnosticsAsync(cancellationToken)).Single(x => x.Id == id);
			var actions = new List<CodeAction>();
			foreach (var fix in _Fixes.Where(x => x.FixableDiagnosticIds.Contains(id)))
				await fix.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken));
			var operations = await actions.Single().GetOperationsAsync(cancellationToken);
			var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
			var text = (await changed.GetTextAsync(cancellationToken)).ToString();
			return text[Preamble.Length..];
		}

		private static CSharpCompilationOptions Options { get; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

		private static CSharpCompilation Compile(string source)
		{
			var compilation = CSharpCompilation.Create(
				"Analysed",
				[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
				_References,
				Options);
			var errors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
			return errors.Count == 0
				? compilation
				: throw new InvalidOperationException($"The sample does not compile: {string.Join("; ", errors)}");
		}

		private static IEnumerable<MetadataReference> References() =>
			((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
				.Split(Path.PathSeparator)
				.Append(typeof(ServiceFixture).Assembly.Location)
				.Append(typeof(ApplicationHost).Assembly.Location)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Select(x => MetadataReference.CreateFromFile(x));

		private static IEnumerable<Component> Create<Component>(Type? anchor = null) =>
			(anchor ?? typeof(Analysers.Marker)).Assembly.GetTypes()
				.Where(x => typeof(Component).IsAssignableFrom(x) && !x.IsAbstract)
				.Select(x => (Component)Activator.CreateInstance(x)!);
	}
}
