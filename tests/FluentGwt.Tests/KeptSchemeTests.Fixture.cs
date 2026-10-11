using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentGwt.Tests;

public sealed partial class KeptSchemeTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public const string Cookies = "Cookies";
		public const string Either = "Either";

		public Fixture() => Api = ApplicationHost.Composed(this, "kept", Compose, Pipeline);

		public ApplicationHost Api { get; }

		private static void Compose(WebApplicationBuilder builder)
		{
			builder.Services.AddAuthentication(Cookies)
				.AddCookie(Cookies)
				.AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(ApiKeyHandler.Name, null)
				.AddPolicyScheme(Either, Either, options => options.ForwardDefaultSelector = context =>
					context.Request.Headers.ContainsKey(ApiKeyHandler.Header) ? ApiKeyHandler.Name : Cookies);
			builder.Services.AddAuthorization();
		}

		private static void Pipeline(WebApplication app)
		{
			app.UseAuthentication();
			app.UseAuthorization();
			app.MapGet("/machine", (HttpContext context) => Scheme(context))
				.RequireAuthorization(policy => policy.AddAuthenticationSchemes(ApiKeyHandler.Name).RequireAuthenticatedUser());
			app.MapGet("/human", (HttpContext context) => Scheme(context))
				.RequireAuthorization(policy => policy.AddAuthenticationSchemes(Cookies).RequireAuthenticatedUser());
			app.MapGet("/either", (HttpContext context) => Scheme(context))
				.RequireAuthorization(policy => policy.AddAuthenticationSchemes(Either).RequireAuthenticatedUser());
		}

		private static string Scheme(HttpContext context) => context.User.Identity?.AuthenticationType ?? string.Empty;
	}

	internal sealed class ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		public const string Name = "ApiKey";
		public const string Header = "X-Api-Key";
		public const string Right = "right";

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
			=> Task.FromResult(Request.Headers[Header] == Right
				? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("key", Right)], Name)), Name))
				: AuthenticateResult.NoResult());
	}

	internal sealed record Reply(System.Net.HttpStatusCode Status, string Scheme);
}
