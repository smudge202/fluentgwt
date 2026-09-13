using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FluentGwt.Analysers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FixtureLifetimeAnalyser : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.FixtureLifetime];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			if (start.Compilation.GetTypeByMetadataName("FluentGwt.ServiceFixture") is { } fixture)
				start.RegisterSyntaxNodeAction(node => Analyse(node, fixture), SyntaxKind.PropertyDeclaration);
		});
	}

	private static void Analyse(SyntaxNodeAnalysisContext context, INamedTypeSymbol fixture)
	{
		var property = (PropertyDeclarationSyntax)context.Node;
		if (context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) is not { } symbol
			|| !DerivesFrom(symbol.Type, fixture)
			|| IsOneInstance(property))
			return;
		context.ReportDiagnostic(Diagnostic.Create(Descriptors.FixtureLifetime, property.Identifier.GetLocation(), symbol.Name));
	}

	private static bool IsOneInstance(PropertyDeclarationSyntax property) =>
		property.ExpressionBody is null
		&& property.AccessorList is { Accessors.Count: 1 } accessors
		&& accessors.Accessors[0] is { Body: null, ExpressionBody: null } accessor
		&& accessor.IsKind(SyntaxKind.GetAccessorDeclaration)
		&& property.Initializer?.Value is BaseObjectCreationExpressionSyntax;

	private static bool DerivesFrom(ITypeSymbol type, INamedTypeSymbol fixture)
	{
		for (var current = type; current is not null; current = current.BaseType)
			if (SymbolEqualityComparer.Default.Equals(current, fixture))
				return true;
		return false;
	}
}
