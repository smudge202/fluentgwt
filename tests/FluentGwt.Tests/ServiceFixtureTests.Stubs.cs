using AwesomeAssertions;
using Moq;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class ServiceFixtureTests
{
	[Fact]
	public Task WhenStubIsRequestedThenMockIsRegisteredAndReturned()
		=> Context
			.Given(x => x.Stub = x.Subject.Services.Stub<Warehouse>())
			.When(x => x.Subject.Resolve<Warehouse>())
			.Then((x, warehouse) => warehouse.Should().BeSameAs(x.Stub!.Object));

	[Fact]
	public Task WhenStubReplacesARegistrationThenOnlyTheStubResolves()
		=> Context
			.Given(x => x.Subject.Services.Override<Warehouse>(new OfflineWarehouse()))
			.Given(x => x.Stub = x.Subject.Services.Stub<Warehouse>())
			.When(x => x.Subject.Resolve<IEnumerable<Warehouse>>())
			.Then((x, warehouses) => warehouses.Should().ContainSingle().Which.Should().BeSameAs(x.Stub!.Object));

	[Fact]
	public Task WhenStubIsStrictThenAnUnexpectedCallThrows()
		=> Context
			.Given(x => x.Stub = x.Subject.Services.Stub<Warehouse>(MockBehavior.Strict))
			.When(x => x.Subject.Resolve<Warehouse>().Available())
			.ThenThrows<MockException>();

	[Fact]
	public Task WhenStubBehaviourIsNotGivenThenMoqsDefaultApplies()
		=> Context
			.Given(x => x.Stub = x.Subject.Services.Stub<Warehouse>())
			.When(x => x.Stub!.Behavior)
			.Then(behaviour => behaviour.Should().Be(MockBehavior.Default));

	public interface Warehouse
	{
		int Available();
	}

	internal sealed class OfflineWarehouse : Warehouse
	{
		public int Available() => 0;
	}
}
