using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentGwt;

public static class HostedServiceCollectionExtensions
{
	public static IServiceCollection RemoveHostedService<Implementation>(this IServiceCollection services)
		where Implementation : IHostedService =>
		services.RemoveHostedService(typeof(Implementation));

	public static IServiceCollection RemoveHostedService(this IServiceCollection services, Type implementation)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(implementation);
		var hosted = HostedServices(services);
		var matching = hosted.Where(x => ImplementationOf(x) == implementation).ToList();
		if (matching.Count == 0)
		{
			var unknowable = hosted.Count(x => ImplementationOf(x) is null);
			throw new InvalidOperationException(
				$"No hosted service is registered with the implementation {implementation.Name}. " +
				$"{unknowable} factory registration{(unknowable == 1 ? " has" : "s have")} no knowable implementation and may include it.");
		}
		foreach (var descriptor in matching)
			services.Remove(descriptor);
		return services;
	}

	public static IServiceCollection RemoveApplicationHostedServices(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		foreach (var descriptor in HostedServices(services).Where(x => ImplementationOf(x) is { } type && !IsFramework(type)))
			services.Remove(descriptor);
		return services;
	}

	private static List<ServiceDescriptor> HostedServices(IServiceCollection services) =>
		[.. services.Where(x => x.ServiceType == typeof(IHostedService) && !x.IsKeyedService)];

	private static Type? ImplementationOf(ServiceDescriptor descriptor) =>
		descriptor.ImplementationType
		?? descriptor.ImplementationInstance?.GetType()
		?? FactoryReturnType(descriptor.ImplementationFactory);

	private static Type? FactoryReturnType(Func<IServiceProvider, object>? factory) =>
		factory?.GetType() is { IsGenericType: true } delegateType
		&& delegateType.GetGenericArguments()[^1] is var returned
		&& returned != typeof(IHostedService)
		&& returned != typeof(object)
			? returned
			: null;

	private static bool IsFramework(Type type) =>
		type.Assembly.GetName().Name is { } name
		&& (name is "Microsoft.AspNetCore" or "Microsoft.Extensions"
			|| name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
			|| name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal));
}
