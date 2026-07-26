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

	public static HttpRedirection RedirectHttp<Client>(this IServiceCollection services) where Client : class =>
		Redirect(services, new ServiceCollection().AddHttpClient<Client>().Name);

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
