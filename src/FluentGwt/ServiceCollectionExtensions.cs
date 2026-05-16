using Microsoft.Extensions.DependencyInjection;

namespace FluentGwt;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection Override<Service>(this IServiceCollection services, Service instance)
		where Service : class
	{
		ArgumentNullException.ThrowIfNull(instance);
		return Replacing(services, typeof(Service), null, _ => ServiceDescriptor.Singleton(instance));
	}

	public static IServiceCollection Override<Service, Implementation>(this IServiceCollection services)
		where Service : class
		where Implementation : class, Service =>
		Replacing(services, typeof(Service), null, lifetime => ServiceDescriptor.Describe(typeof(Service), typeof(Implementation), lifetime));

	public static IServiceCollection Override<Service>(this IServiceCollection services, Func<IServiceProvider, Service> factory)
		where Service : class
	{
		ArgumentNullException.ThrowIfNull(factory);
		return Replacing(services, typeof(Service), null, lifetime => ServiceDescriptor.Describe(typeof(Service), factory, lifetime));
	}

	public static IServiceCollection Override<Service>(this IServiceCollection services, object key, Service instance)
		where Service : class
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(instance);
		return Replacing(services, typeof(Service), key, _ => ServiceDescriptor.KeyedSingleton(key, instance));
	}

	private static IServiceCollection Replacing(IServiceCollection services, Type service, object? key, Func<ServiceLifetime, ServiceDescriptor> replacement)
	{
		ArgumentNullException.ThrowIfNull(services);
		var existing = services.Where(x => x.ServiceType == service && Equals(x.ServiceKey, key)).ToList();
		var lifetime = existing.Count > 0 ? existing[0].Lifetime : ServiceLifetime.Transient;
		foreach (var descriptor in existing)
			services.Remove(descriptor);
		services.Add(replacement(lifetime));
		return services;
	}
}
