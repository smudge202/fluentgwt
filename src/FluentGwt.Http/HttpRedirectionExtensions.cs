using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace FluentGwt;

[SuppressMessage("Design", "CA1034", Justification = "A C# 14 extension block compiles to a nested type; the rule predates extension members.")]
public static class HttpRedirectionExtensions
{
	extension(ServiceFixture fixture)
	{
		public HttpTraffic Http => fixture.Resolve<HttpTraffic>();
	}

	public static HttpRedirection RedirectHttp(this IServiceCollection services) => Redirect(services, null);

	public static HttpRedirection RedirectHttp(this IServiceCollection services, string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		return Redirect(services, name);
	}

	public static HttpRedirection RedirectHttp<Client>(this IServiceCollection services) where Client : class
	{
		var redirection = Redirect(services, new ServiceCollection().AddHttpClient<Client>().Name);
		services.PostConfigureAll<HttpClientFactoryOptions>(_ => EnsureRegistered(services, typeof(Client)));
		return redirection;
	}

	private static void EnsureRegistered(IServiceCollection services, Type client)
	{
		if (services.Any(x => x.ServiceType == client))
			return;
		var abstractions = services
			.Select(x => x.ServiceType)
			.Where(x => x != typeof(object) && x != client && x.IsAssignableFrom(client))
			.Distinct()
			.Select(x => $"RedirectHttp<{x.Name}>()")
			.ToList();
		var missing = $"RedirectHttp<{client.Name}>() redirects a typed client registered as AddHttpClient<{client.Name}>(), and none is registered.";
		throw new InvalidOperationException(abstractions.Count == 0
			? missing
			: $"{missing} A client registered as AddHttpClient<Abstraction, Implementation>() is named after its abstraction: use {string.Join(" or ", abstractions)}.");
	}

	private static HttpRedirection Redirect(IServiceCollection services, string? name)
	{
		ArgumentNullException.ThrowIfNull(services);
		var redirection = new HttpRedirection();
		services.TryAddSingleton<HttpTraffic>();
		void Redirecting(HttpClientFactoryOptions options) =>
			options.HttpMessageHandlerBuilderActions.Add(builder =>
				builder.PrimaryHandler = redirection.Handler(builder.Services.GetRequiredService<HttpTraffic>()));
		if (name is null)
			services.PostConfigureAll<HttpClientFactoryOptions>(Redirecting);
		else
			services.PostConfigure<HttpClientFactoryOptions>(name, Redirecting);
		return redirection;
	}
}
