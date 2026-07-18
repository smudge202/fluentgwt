using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FluentGwt;

public static class StubServiceCollectionExtensions
{
	public static Mock<Service> Stub<Service>(this IServiceCollection services, MockBehavior behavior = MockBehavior.Default)
		where Service : class
	{
		var stub = new Mock<Service>(behavior);
		services.Override(stub.Object);
		return stub;
	}
}
