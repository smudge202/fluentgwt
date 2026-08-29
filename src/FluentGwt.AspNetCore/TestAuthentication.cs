using System.Collections.Concurrent;
using System.Security.Claims;

namespace FluentGwt;

public sealed class TestAuthentication
{
	public const string DefaultScheme = "FluentGwt";

	private readonly Lock _lock = new();
	private readonly ServiceFixture _fixture;
	private readonly List<(string Type, Func<string> Value)> _claims = [];
	private readonly List<string> _roles = [];
	private readonly ConcurrentQueue<IssuedAuthentication> _issued = new();
	private bool _authenticated;
	private string? _onlyScheme;

	internal TestAuthentication(ServiceFixture fixture) => _fixture = fixture;

	public ClaimsPrincipal Principal
	{
		get
		{
			lock (_lock)
				return _authenticated
					? Build(DefaultScheme, ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType)
					: new ClaimsPrincipal(new ClaimsIdentity());
		}
	}

	public IReadOnlyList<IssuedAuthentication> Issued => [.. _issued];

	public TestAuthentication AuthenticateAs(string? scheme = null)
	{
		lock (_lock)
		{
			_authenticated = true;
			_onlyScheme = scheme;
		}
		return this;
	}

	public TestAuthentication Claim(string type, string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Claim(type, () => value);
	}

	public TestAuthentication Claim(string type, Func<string> value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(type);
		ArgumentNullException.ThrowIfNull(value);
		lock (_lock)
		{
			_claims.Add((type, value));
			_authenticated = true;
		}
		return this;
	}

	public TestAuthentication Role(string role)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(role);
		lock (_lock)
		{
			_roles.Add(role);
			_authenticated = true;
		}
		return this;
	}

	public TestAuthentication Anonymous()
	{
		lock (_lock)
		{
			_authenticated = false;
			_onlyScheme = null;
			_claims.Clear();
			_roles.Clear();
		}
		return this;
	}

	internal ClaimsPrincipal? Authenticate(string scheme, string nameType, string roleType, string path)
	{
		ClaimsPrincipal principal;
		lock (_lock)
		{
			if (!_authenticated || (_onlyScheme is not null && _onlyScheme != scheme))
				return null;
			principal = Build(scheme, nameType, roleType);
		}
		_issued.Enqueue(new(scheme, principal, path));
		return principal;
	}

	private ClaimsPrincipal Build(string scheme, string nameType, string roleType)
	{
		var identity = new ClaimsIdentity(scheme, nameType, roleType);
		identity.AddClaim(new(ClaimTypes.NameIdentifier, $"user-{_fixture.TestId}"));
		foreach (var (type, value) in _claims)
			identity.AddClaim(new(type, value()));
		foreach (var role in _roles)
			identity.AddClaim(new(roleType, role));
		return new(identity);
	}
}
