using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FluentGwt.Tests.HostedServiceTests;

namespace FluentGwt.Tests;

internal static class HostedServiceTestsFluent
{
	public static Given<Fixture> GivenChain(this Fixture fixture, Func<Fixture, Task> chain)
		=> fixture.Given(x => x.Chain = () => chain(x));

	public static When<Fixture> WhenExecutingTheChain(this Given<Fixture> given)
		=> given.When(x => x.Chain());

	public static When<Fixture> WhenExecutingTheChainCapturingFailure(this Given<Fixture> given)
		=> given.When(async x =>
		{
			var execution = x.Chain();
			await execution.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			x.Failure = execution.Exception?.InnerException;
		});

	public static When<Fixture, string[]> WhenListingHostedServices(this Given<Fixture> given, Action<IServiceCollection> change)
		=> given.When(x =>
		{
			change(x.Collection);
			return x.Collection
				.Where(d => d.ServiceType == typeof(IHostedService))
				.Select(d => d.IsKeyedService ? $"keyed {d.ServiceKey}" : Describe(d))
				.ToArray();
		});

	private static string Describe(ServiceDescriptor descriptor) =>
		descriptor.ImplementationType?.Name
		?? descriptor.ImplementationInstance?.GetType().Name
		?? $"factory {descriptor.ImplementationFactory!.GetType().GetGenericArguments()[1].Name}";
}
