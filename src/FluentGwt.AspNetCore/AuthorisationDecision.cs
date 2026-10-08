using Microsoft.AspNetCore.Authorization;

namespace FluentGwt;

public sealed record AuthorisationDecision(
	string Endpoint,
	IReadOnlyList<string> Policies,
	bool Succeeded,
	IReadOnlyList<IAuthorizationRequirement> FailedRequirements);
