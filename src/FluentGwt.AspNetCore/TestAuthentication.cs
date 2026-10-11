using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;

namespace FluentGwt;

public sealed class TestAuthentication
{
	public const string DefaultScheme = "FluentGwt";

	internal const string UserHeader = "X-FluentGwt-User";

	private readonly Lock _lock = new();
	private readonly ServiceFixture _fixture;
	private readonly HashSet<string> _kept = new(StringComparer.Ordinal);
	private readonly ConcurrentQueue<IssuedAuthentication> _issued = new();
	private readonly ConcurrentQueue<RecordedSignIn> _signIns = new();
	private readonly ConcurrentQueue<RecordedSignOut> _signOuts = new();
	private readonly ConcurrentDictionary<string, TestUser> _bound = new(StringComparer.Ordinal);
	private TestUser _user = new();
	private int _boundUsers;
	private bool _authenticated;
	private string? _onlyScheme;

	internal TestAuthentication(ServiceFixture fixture) => _fixture = fixture;

	public ClaimsPrincipal Principal
	{
		get
		{
			lock (_lock)
				return _authenticated
					? _user.Build(DefaultScheme, ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType, HostNameIdentifier)
					: new ClaimsPrincipal(new ClaimsIdentity());
		}
	}

	public IReadOnlyList<IssuedAuthentication> Issued => [.. _issued];

	public IReadOnlyList<RecordedSignIn> SignIns => [.. _signIns];

	public IReadOnlyList<RecordedSignOut> SignOuts => [.. _signOuts];

	internal IReadOnlyList<string> Kept
	{
		get
		{
			lock (_lock)
				return [.. _kept];
		}
	}

	private string HostNameIdentifier => $"user-{_fixture.TestId}";

	public TestAuthentication Keep(string scheme)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
		lock (_lock)
			_kept.Add(scheme);
		return this;
	}

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
		lock (_lock)
		{
			_user.Claim(type, value);
			_authenticated = true;
		}
		return this;
	}

	public TestAuthentication Role(string role)
	{
		lock (_lock)
		{
			_user.Role(role);
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
			_user = new();
		}
		return this;
	}

	internal void Record(RecordedSignIn signIn) => _signIns.Enqueue(signIn);

	internal void Record(RecordedSignOut signOut) => _signOuts.Enqueue(signOut);

	internal string Bind(Action<TestUser> configure)
	{
		var user = new TestUser();
		configure(user);
		var id = Interlocked.Increment(ref _boundUsers).ToString(CultureInfo.InvariantCulture);
		_bound[id] = user;
		return id;
	}

	internal ClaimsPrincipal? Authenticate(string scheme, string nameType, string roleType, string path, string? boundUser)
	{
		ClaimsPrincipal principal;
		if (boundUser is not null && _bound.TryGetValue(boundUser, out var bound))
			principal = bound.Build(scheme, nameType, roleType, $"{HostNameIdentifier}-{boundUser}");
		else
		{
			lock (_lock)
			{
				if (!_authenticated || (_onlyScheme is not null && _onlyScheme != scheme))
					return null;
				principal = _user.Build(scheme, nameType, roleType, HostNameIdentifier);
			}
		}
		_issued.Enqueue(new(scheme, principal, path));
		return principal;
	}
}
