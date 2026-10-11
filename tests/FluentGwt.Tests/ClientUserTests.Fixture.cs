using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt.Tests;

public sealed partial class ClientUserTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public const string User = "user";
		public const string Cookies = "Cookies";
		public const string Reviewer = "Reviewer";

		public Fixture() => Api = ApplicationHost.Composed(this, "people", Compose, Pipeline);

		public ApplicationHost Api { get; }

		private static void Compose(WebApplicationBuilder builder)
		{
			builder.Services.AddAuthentication(Cookies).AddCookie(Cookies);
			builder.Services.AddAuthorization();
		}

		private static void Pipeline(WebApplication app)
		{
			app.UseAuthentication();
			app.UseAuthorization();
			app.MapGet("/whoami", (ClaimsPrincipal user) => user.FindFirst(User)?.Value ?? "nobody").RequireAuthorization();
			app.MapGet("/review", (ClaimsPrincipal user) => user.FindFirst(User)?.Value ?? "nobody").RequireAuthorization(policy => policy.RequireRole(Reviewer));
			app.MapGet("/id", (ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "none").RequireAuthorization();
			app.MapGet("/headers", (HttpContext context) => string.Join(",", context.Request.Headers.Keys.Where(x => x.StartsWith("X-FluentGwt", StringComparison.OrdinalIgnoreCase))));
		}
	}
}
