# FluentGwt

Given/When/Then for .NET tests, written as one expression per fact.

```csharp
[Fact]
public Task WhenStockIsAvailableThenOrderIsAccepted()
	=> Context
		.GivenOrdering()
		.GivenStock(5)
		.WhenPlacingOrder(x => x.Quantity = 2)
		.Then(result => result.Status.Should().Be(PlacementStatus.Accepted));
```

Not yet published to NuGet. Targets .NET 10.

## The pattern

A test class is a `sealed partial class` split by aspect:

| File | Holds |
|---|---|
| `OrderTests.cs` | the facts, as expression-bodied one-liners |
| `OrderTests.Fixture.cs` | an `internal sealed class Fixture`, exposed as `private Fixture Context { get; } = new();` |
| `OrderTests.Fluent.cs` | the Given and When steps, as extension methods |

```csharp
public sealed partial class OrderTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Mock<Warehouse> Warehouse { get; } = new(MockBehavior.Strict);
		public PlaceOrder Order { get; } = new("SKU-1", 1);
	}
}

internal static class OrderTestsFluent
{
	public static Given<Fixture> GivenOrdering(this Fixture fixture)
		=> fixture
			.Given(x => x.Services.AddOrdering())
			.Given(x => x.Services.Override(x.Warehouse.Object));

	public static Given<Fixture> GivenStock(this Given<Fixture> given, int available)
		=> given.Given(x => x.Warehouse
			.Setup(m => m.Available(x.Order.Sku, It.IsAny<CancellationToken>()))
			.ReturnsAsync(available));

	public static When<Fixture, PlacementResult> WhenPlacingOrder(this Given<Fixture> given, Action<PlaceOrder>? change = null)
		=> given.When(x =>
		{
			change?.Invoke(x.Order);
			return x.Resolve<PlaceOrderHandler>().Place(x.Order);
		});
}
```

## Given

- `target.Given(x => ...)` declares a step. Nothing runs until the chain is awaited, then every
  step runs once, in the order written. Steps can be sync, `Task` or `ValueTask`.
- `.Given(value)`, `.Given(name, value)`, `.Given(key, value)` store state on the chain, read back
  with `.Get<Value>()`, `.Get<Value>(name)` or `.Get<Value>(key)`. Storing under the same key
  replaces the value.
- `.And(x => ...)` after a Given is another step, for reading.
- A simple test needs no fixture: `Given.With(...)` starts a chain on nothing.

## When

- `.When(x => ...)` is the act. It runs exactly once, after every Given.
- An act that returns a value — sync, `Task<Result>` or `ValueTask<Result>` — passes that value to
  the assertions.
- `.And(x => ...)` after a When is a further act step that keeps the result; `.AndResult(x => ...)`
  replaces it.
- `.WhenResolving<Service>()` resolves a service from a `ServiceFixture`.

## Then

- `.Then(result => ...)` asserts on the act's result, or on the fixture when the act has none.
- `.Then((x, result) => ...)` gets both; `.ThenFixture(x => ...)` gets the fixture after an act
  with a result.
- `.And(...)` after a Then is a further assertion; `.AndFixture(...)` asserts on the fixture.
  Assertions run in order and the first failure stops the rest.
- The chain is awaitable and converts to `Task`, so a fact returns it directly.

### Expecting an exception

```csharp
.WhenDecoding(x => x.Frame[..^1])
.ThenThrows<MalformedFrameException>(e => e.Offset.Should().BeGreaterThan(0))
.And(x => x.Metrics.Verify(m => m.Rejected(), Times.Once));
```

- `ThenThrows<Failure>()` passes for `Failure` or a derived type; `ThenThrowsExactly<Failure>()`
  for that type alone.
- Only the act is inside the expectation. A Given that throws fails the test as itself, however
  well its type matches. `ThenArrangementFails<Failure>()` is the deliberate way to expect
  arrangement to fail.

## ServiceFixture

Derive the fixture from `ServiceFixture` to compose and resolve through a real container.

- Register freely on `Services` during arrangement. The first resolution builds the provider and
  locks the collection: registering afterwards throws, naming every type resolved so far, so a
  Given that resolved too early is easy to find.
- The provider validates on build and validates scopes by default. Turn `ValidateOnBuild` off to
  compose a deliberately partial graph.
- `IsResolvable<Service>()` answers through the real provider, for resolvable-first tests.
- `Services.Override(...)` replaces **every** registration of a service, not just the last — so
  `IEnumerable<Service>` holds only the override. Type and factory overrides keep the original's
  lifetime; keyed overrides touch only their key.
- At the end of the chain the provider is disposed, then the fixture's `DisposeFixture()` runs,
  whether the test passed or failed.

## Building

```
dotnet test --solution FluentGwt.slnx
```

xunit v3 on Microsoft.Testing.Platform, opted into by the root `global.json`.
`docs/spec.md` is the behavioural specification the library is built from.
