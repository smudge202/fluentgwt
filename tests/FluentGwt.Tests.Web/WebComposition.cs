using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace FluentGwt.Tests.Web;

public static class WebComposition
{
	public static IServiceCollection AddWeb(this IServiceCollection services)
	{
		services.AddSingleton<Greeter, FriendlyGreeter>();
		services.AddSingleton<RequestCounter>();
		services.AddSingleton<Journal>();
		services.AddSingleton(TimeProvider.System);
		services.AddHostedService<StartupCheck>();
		services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Authority = "https://identity.invalid/tenant/v2.0";
				options.Audience = "api://web";
				options.TokenValidationParameters.RoleClaimType = "roles";
			})
			.AddJwtBearer("Partner", options => options.Authority = "https://partner.invalid/")
			.AddCookie("Cookies");
		services.AddAuthorizationBuilder().AddPolicy("Reader", policy => policy.RequireClaim("scope", "read"));
		services.AddSingleton<IAuthorizationMiddlewareResultHandler, StampingResultHandler>();
		return services;
	}

	public static WebApplication MapWeb(this WebApplication app)
	{
		app.MapGet("/greeting", (Greeter greeter) => greeter.Greet());
		app.MapGet("/greetings", (IEnumerable<Greeter> greeters) => greeters.Count());
		app.MapGet("/configuration/{key}", (string key, IConfiguration configuration) => configuration[key] ?? string.Empty);
		app.MapGet("/environment", (IWebHostEnvironment environment) => environment.EnvironmentName);
		app.MapGet("/redirect", () => Results.Redirect("/greeting"));
		app.MapGet("/count", (RequestCounter counter) => counter.Next());
		app.MapGet("/time", (TimeProvider time) => time.GetUtcNow().ToUnixTimeSeconds());
		app.MapGet("/me", (ClaimsPrincipal user) => string.Join(';', user.Claims.Select(x => $"{x.Type}={x.Value}"))).RequireAuthorization();
		app.MapGet("/read", () => "read").RequireAuthorization("Reader");
		app.MapGet("/admin", () => "admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
		app.MapGet("/partner", () => "partner").RequireAuthorization(policy => policy.AddAuthenticationSchemes("Partner").RequireAuthenticatedUser());
		app.MapGet("/cookie", () => "cookie").RequireAuthorization(policy => policy.AddAuthenticationSchemes("Cookies").RequireAuthenticatedUser());
		app.MapGet("/journal", (Journal journal) => string.Join(',', journal.Entries));
		app.MapPost("/journal/{entry}", (string entry, Journal journal) => journal.Entries.Enqueue(entry));
		return app;
	}
}
