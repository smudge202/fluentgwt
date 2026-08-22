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
		app.MapGet("/journal", (Journal journal) => string.Join(',', journal.Entries));
		app.MapPost("/journal/{entry}", (string entry, Journal journal) => journal.Entries.Enqueue(entry));
		return app;
	}
}
