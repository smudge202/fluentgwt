using Microsoft.CodeAnalysis;

namespace FluentGwt.Analysers;

internal static class Descriptors
{
	private const string Category = "FluentGwt";

	public static DiagnosticDescriptor FixtureLifetime { get; } = new(
		"FG0001",
		"A fixture must be one instance for the whole test",
		"'{0}' is not a getter-only property initialised with new(), so steps can see different fixtures and teardown can miss the one used",
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static DiagnosticDescriptor DiscardedChain { get; } = new(
		"FG0002",
		"A chain that is never awaited never runs",
		"This {0} is discarded, so nothing in its chain runs and the test passes having tested nothing; return or await it",
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static DiagnosticDescriptor Deferral { get; } = new(
		"FG0003",
		"A given that resolves or uses a running host should be deferred",
		"This given {0}; defer it so it runs after the immediate givens",
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);
}
