using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FluentGwt.Analysers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeferralAnalyser : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.Deferral];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			if (start.Compilation.GetTypeByMetadataName("FluentGwt.ServiceFixture") is not { } fixture)
				return;
			var host = start.Compilation.GetTypeByMetadataName("FluentGwt.ApplicationHost");
			start.RegisterSyntaxNodeAction(node => Analyse(node, fixture, host), SyntaxKind.InvocationExpression);
		});
	}

	private static void Analyse(SyntaxNodeAnalysisContext context, INamedTypeSymbol fixture, INamedTypeSymbol? host)
	{
		var outermost = (InvocationExpressionSyntax)context.Node;
		if (outermost.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax })
			return;
		var chain = Chain(outermost);
		var steps = chain
			.Select((invocation, index) => (Invocation: invocation, Index: index, Step: Classify(context, invocation, fixture, host)))
			.Where(x => x.Step is not null)
			.ToList();
		foreach (var (invocation, index, step) in steps)
		{
			if (IsDeferred(chain, index))
				continue;
			if (step!.UsesHost)
				Report(context, invocation, "uses a running host, which starts it before the rest of its arrangement");
			else if (step.Resolves && steps.FirstOrDefault(x => x.Index > index && x.Step!.Registers) is { Invocation: { } later })
				Report(context, invocation, $"resolves from the container before the given on line {later.GetLocation().GetLineSpan().StartLinePosition.Line + 1} registers, which the container will then refuse");
		}
	}

	private static List<InvocationExpressionSyntax> Chain(InvocationExpressionSyntax outermost)
	{
		var chain = new List<InvocationExpressionSyntax>();
		for (ExpressionSyntax? current = outermost; current is InvocationExpressionSyntax invocation; current = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression)
			chain.Insert(0, invocation);
		return chain;
	}

	private static bool IsDeferred(List<InvocationExpressionSyntax> chain, int index) =>
		index + 1 < chain.Count && Name(chain[index + 1]) == "Deferred";

	private static string? Name(InvocationExpressionSyntax invocation) =>
		(invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;

	private static Step? Classify(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, INamedTypeSymbol fixture, INamedTypeSymbol? host)
	{
		if (Name(invocation) != "Given"
			|| invocation.ArgumentList.Arguments.Select(x => x.Expression).OfType<LambdaExpressionSyntax>().FirstOrDefault() is not { } lambda)
			return null;
		var model = context.SemanticModel;
		var calls = lambda.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
			.Select(x => model.GetSymbolInfo(x, context.CancellationToken).Symbol as IMethodSymbol)
			.OfType<IMethodSymbol>()
			.ToList();
		var resolves = calls.Any(x => Is(x.ContainingType, fixture) && x.Name is "Resolve" or "IsResolvable");
		var usesHost = host is not null && calls.Any(x => Is(x.ContainingType, host) && x.Name is "Scope" or "Resolve");
		var registers = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
			.Where(x => x.Identifier.Text == "Services")
			.Any(x => model.GetSymbolInfo(x, context.CancellationToken).Symbol is IPropertySymbol property && Is(property.ContainingType, fixture));
		return new Step(resolves, usesHost, registers);
	}

	private static bool Is(ITypeSymbol? type, INamedTypeSymbol expected) => SymbolEqualityComparer.Default.Equals(type, expected);

	private static void Report(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string reason) =>
		context.ReportDiagnostic(Diagnostic.Create(Descriptors.Deferral, ((MemberAccessExpressionSyntax)invocation.Expression).Name.GetLocation(), reason));

	private sealed class Step(bool resolves, bool usesHost, bool registers)
	{
		public bool Resolves => resolves;

		public bool UsesHost => usesHost;

		public bool Registers => registers;
	}
}
