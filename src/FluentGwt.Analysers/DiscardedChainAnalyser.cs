using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FluentGwt.Analysers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedChainAnalyser : DiagnosticAnalyzer
{
	private static readonly string[] _Executable = ["FluentGwt.When`1", "FluentGwt.When`2", "FluentGwt.Then`1", "FluentGwt.Then`2", "FluentGwt.ThenThrows`2"];

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.DiscardedChain];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			var chains = _Executable
				.Select(start.Compilation.GetTypeByMetadataName)
				.OfType<INamedTypeSymbol>()
				.ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
			if (chains.IsEmpty)
				return;
			start.RegisterOperationAction(operation => Statement(operation, chains), OperationKind.ExpressionStatement);
			start.RegisterOperationAction(operation => Local(operation, chains), OperationKind.VariableDeclarator);
		});
	}

	private static bool IsChain(ITypeSymbol? type, ImmutableHashSet<INamedTypeSymbol> chains) =>
		type is INamedTypeSymbol { IsGenericType: true } named && chains.Contains(named.OriginalDefinition);

	private static void Statement(OperationAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> chains)
	{
		var statement = (IExpressionStatementOperation)context.Operation;
		if (IsChain(statement.Operation.Type, chains))
			context.ReportDiagnostic(Diagnostic.Create(Descriptors.DiscardedChain, statement.Syntax.GetLocation(), statement.Operation.Type!.Name));
	}

	private static void Local(OperationAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> chains)
	{
		var declarator = (IVariableDeclaratorOperation)context.Operation;
		if (!IsChain(declarator.Symbol.Type, chains) || declarator.Syntax is not VariableDeclaratorSyntax syntax)
			return;
		var body = syntax.FirstAncestorOrSelf<BlockSyntax>();
		var model = declarator.SemanticModel;
		if (body is null || model is null)
			return;
		var used = body.DescendantNodes()
			.OfType<IdentifierNameSyntax>()
			.Any(x => x.Identifier.Text == declarator.Symbol.Name
				&& SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(x, context.CancellationToken).Symbol, declarator.Symbol));
		if (!used)
			context.ReportDiagnostic(Diagnostic.Create(Descriptors.DiscardedChain, syntax.Identifier.GetLocation(), declarator.Symbol.Type.Name));
	}
}
