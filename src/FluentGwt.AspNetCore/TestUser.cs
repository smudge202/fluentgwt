using System.Security.Claims;

namespace FluentGwt;

public sealed class TestUser
{
	private readonly Lock _lock = new();
	private readonly List<(string Type, Func<string> Value)> _claims = [];
	private readonly List<string> _roles = [];

	internal TestUser() { }

	public TestUser Claim(string type, string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Claim(type, () => value);
	}

	public TestUser Claim(string type, Func<string> value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(type);
		ArgumentNullException.ThrowIfNull(value);
		lock (_lock)
			_claims.Add((type, value));
		return this;
	}

	public TestUser Role(string role)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(role);
		lock (_lock)
			_roles.Add(role);
		return this;
	}

	internal ClaimsPrincipal Build(string scheme, string nameType, string roleType, string nameIdentifier)
	{
		List<(string Type, Func<string> Value)> claims;
		List<string> roles;
		lock (_lock)
			(claims, roles) = ([.. _claims], [.. _roles]);
		var identity = new ClaimsIdentity(scheme, nameType, roleType);
		if (!claims.Any(x => x.Type == ClaimTypes.NameIdentifier))
			identity.AddClaim(new(ClaimTypes.NameIdentifier, nameIdentifier));
		foreach (var (type, value) in claims)
			identity.AddClaim(new(type, value()));
		foreach (var role in roles)
			identity.AddClaim(new(roleType, role));
		return new(identity);
	}
}
