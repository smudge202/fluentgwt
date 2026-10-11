using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt.Tests;

public sealed partial class SignInTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public const string Cookies = CookieAuthenticationDefaults.AuthenticationScheme;
		public const string Subject = "sub";
		public const string SignedInSubject = "signed-in";
		public const string ReturnTo = "/home";

		public Fixture() => Api = ApplicationHost.Composed(this, "signing", Compose, Pipeline);

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
			app.MapGet("/sign-in", async (HttpContext context) =>
			{
				await context.SignInAsync(
					Cookies,
					new ClaimsPrincipal(new ClaimsIdentity([new Claim(Subject, SignedInSubject)], Cookies)),
					new AuthenticationProperties { RedirectUri = ReturnTo });
				return Results.Ok();
			});
			app.MapGet("/sign-out", async (HttpContext context) =>
			{
				await context.SignOutAsync(Cookies, new AuthenticationProperties { RedirectUri = ReturnTo });
				return Results.Ok();
			});
			app.MapGet("/whoami", (ClaimsPrincipal user) => user.FindFirst(Subject)?.Value ?? "nobody").RequireAuthorization();
		}
	}

	internal sealed record Reply(HttpStatusCode Status, string Body);
}
