using System.Collections.Concurrent;

namespace FluentGwt;

public sealed class TestAuthorisation
{
	private readonly ConcurrentQueue<AuthorisationDecision> _decisions = new();

	internal TestAuthorisation()
	{
	}

	public IReadOnlyList<AuthorisationDecision> Decisions => [.. _decisions];

	internal void Record(AuthorisationDecision decision) => _decisions.Enqueue(decision);
}
