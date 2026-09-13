using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FluentGwt.Analysers;

namespace FluentGwt.Analysers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixtureLifetimeCodeFix)), Shared]
public sealed class FixtureLifetimeCodeFix : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds { get; } = [Descriptors.FixtureLifetime.Id];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root?.FindNode(context.Span).FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not { } property)
			return;
		context.RegisterCodeFix(
			CodeAction.Create(
				"Make it one instance: { get; } = new();",
				_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(property, OneInstance(property)))),
				nameof(FixtureLifetimeCodeFix)),
			context.Diagnostics);
	}

	private static MemberDeclarationSyntax OneInstance(PropertyDeclarationSyntax property)
	{
		var parts = new[] { property.AttributeLists.ToString(), property.Modifiers.ToString(), property.Type.ToString(), property.Identifier.Text }
			.Where(x => x.Length > 0);
		var rewritten = SyntaxFactory.ParseMemberDeclaration($"{string.Join(" ", parts)} {{ get; }} = new();")!;
		return rewritten.WithTriviaFrom(property);
	}
}
