using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt;

public static class AuthenticationGivens
{
	public static Given<Fixture> GivenAuthenticatedUser<Fixture>(this Given<Fixture> given, string? scheme = null)
		where Fixture : ServiceFixture =>
		given.Given(x => { ApplicationHost.Only(x).Authentication().AuthenticateAs(scheme); });

	public static Given<Fixture> GivenAnonymousUser<Fixture>(this Given<Fixture> given)
		where Fixture : ServiceFixture =>
		given.Given(x => { ApplicationHost.Only(x).Authentication().Anonymous(); });

	public static Given<Fixture> GivenClaim<Fixture>(this Given<Fixture> given, string type, string value)
		where Fixture : ServiceFixture =>
		given.Given(x => { ApplicationHost.Only(x).Authentication().Claim(type, value); });

	public static Given<Fixture> GivenClaim<Fixture>(this Given<Fixture> given, string type, Func<Fixture, string> value)
		where Fixture : ServiceFixture
	{
		ArgumentNullException.ThrowIfNull(value);
		return given.Given(x => { ApplicationHost.Only(x).Authentication().Claim(type, () => value(x)); });
	}

	public static Given<Fixture> GivenRole<Fixture>(this Given<Fixture> given, string role)
		where Fixture : ServiceFixture =>
		given.Given(x => { ApplicationHost.Only(x).Authentication().Role(role); });

	public static Given<Fixture> GivenPolicy<Fixture>(this Given<Fixture> given, string name, Action<AuthorizationPolicyBuilder> configure)
		where Fixture : ServiceFixture =>
		given.Given(x => { ApplicationHost.Only(x).ConfigureServices(services => services.PostConfigure<AuthorizationOptions>(options => options.AddPolicy(name, configure))); });
}
