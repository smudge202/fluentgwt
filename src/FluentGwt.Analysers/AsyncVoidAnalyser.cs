using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FluentGwt.Analysers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidAnalyser : DiagnosticAnalyzer
{
	private const string Library = "FluentGwt";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.AsyncVoid];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(Analyse, OperationKind.DelegateCreation);
	}

	private static void Analyse(OperationAnalysisContext context)
	{
		var creation = (IDelegateCreationOperation)context.Operation;
		if (creation.Type is not INamedTypeSymbol { DelegateInvokeMethod: { ReturnsVoid: true } } delegateType
			|| creation.Parent is not IArgumentOperation { Parameter.ContainingSymbol: IMethodSymbol method }
			|| method.ContainingNamespace?.ToDisplayString() != Library
			|| Kind(creation.Target) is not { } kind)
			return;
		context.ReportDiagnostic(Diagnostic.Create(
			Descriptors.AsyncVoid,
			creation.Syntax.GetLocation(),
			kind,
			$"{method.ContainingType.Name}.{method.Name}",
			delegateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
	}

	private static string? Kind(IOperation target) => target switch
	{
		IAnonymousFunctionOperation { Symbol.IsAsync: true } => "lambda",
		IMethodReferenceOperation { Method.IsAsync: true } => "method",
		_ => null,
	};
}
