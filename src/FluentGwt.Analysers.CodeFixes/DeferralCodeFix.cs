using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FluentGwt.Analysers;

namespace FluentGwt.Analysers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DeferralCodeFix)), Shared]
public sealed class DeferralCodeFix : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds { get; } = [Descriptors.Deferral.Id];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root?.FindNode(context.Span).FirstAncestorOrSelf<InvocationExpressionSyntax>() is not { } given)
			return;
		var deferred = SyntaxFactory.InvocationExpression(
			SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, given.WithoutTrailingTrivia(), SyntaxFactory.IdentifierName("Deferred")))
			.WithTrailingTrivia(given.GetTrailingTrivia());
		context.RegisterCodeFix(
			CodeAction.Create(
				"Defer this given",
				_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(given, deferred))),
				nameof(DeferralCodeFix)),
			context.Diagnostics);
	}
}
