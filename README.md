# FluentGwt

[![CI (dev)](https://github.com/smudge202/fluentgwt/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/smudge202/fluentgwt/actions/workflows/ci.yml?query=branch%3Adev)
[![CI (main)](https://github.com/smudge202/fluentgwt/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/smudge202/fluentgwt/actions/workflows/ci.yml?query=branch%3Amain)
[![Licence: Apache-2.0](https://img.shields.io/badge/licence-Apache--2.0-blue)](https://github.com/smudge202/fluentgwt/blob/main/LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

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

Targets .NET 10.

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
- `.Deferred()` after a Given runs it after every immediate Given, so a Given that resolves can sit
  anywhere without locking out registrations written after it.
- `OnTeardown(token => ...)` registers cleanup, run in reverse order at the end of the chain on
  every path. `Cancellation` is signalled first, so background work can stop. A teardown failure
  never hides the test's own: both together arrive as one `AggregateException`, the test's first.
- `Configuration` reads `appsettings.json`, `appsettings.{environment}.json`, user secrets and
  environment variables; `Configure(key, value)` overrides a value for this fixture only.
- `Time` is a `FakeTimeProvider` registered as `TimeProvider`; `Time.Advance(...)` moves it.
- `Logs` captures every log record for assertions; warnings and above also go to the test output
  (`FluentGwt:LogLevel` changes that).

### Seeds and test data

Every fixture has a `Seed`: fresh each run, fixed by overriding `FixedSeed`, or forced by setting
`FluentGwtSeed` in configuration. A failing test writes its seed to the test output —
`FluentGwt seed: 1234567 (fresh; replay with FluentGwtSeed=1234567)` — so the failure can be
replayed. `TestId` is a short identifier derived from the seed and the test, for naming isolated
resources such as a scratch database.

With **FluentGwt.Bogus**, `x.Fake` and `x.Random` generate data from that seed (locale `en_GB`,
or `FluentGwt:Locale`). They are extension properties, so inside the fixture class itself they
are reached as `this.Fake`.

## Packages

| Package | Adds |
|---|---|
| `FluentGwt` | the chain, `ServiceFixture`, overrides, configuration, seeds, time, logging |
| `FluentGwt.Xunit` | the test's cancellation token and identity, integration gating, `FixtureData`, test output |
| `FluentGwt.Bogus` | `x.Fake` and `x.Random` |
| `FluentGwt.Moq` | `Services.Stub<Service>()`, a `Mock<Service>` that replaces every registration |

### Integration tests

`csharp
[IntegrationFact(IntegrationJustification.NetworkIo, "Publishes to the real message bus")]
public Task WhenOrderIsPublishedThenDispatchConsumesIt() => ...
`

An integration test says what makes it one and why. It is reported as skipped unless the test
project is built with `-p:FluentGwtIntegration=true`, which also defines `INTEGRATION` for code
that only an integration build can compile. In an integration build, a `ServiceFixture`'s hosted
services start before the act and stop at teardown.

### Theory data from the fixture

`csharp
public static TheoryData<FixtureRow<PlaceOrder>> InvalidOrders => new FixtureData<Fixture, PlaceOrder>
{
	{ "zero quantity", x => x.Order with { Quantity = 0 } },
	{ "negative quantity", x => x.Order with { Quantity = -1 } },
};

[Theory, MemberData(nameof(InvalidOrders))]
public Task WhenOrderIsInvalidThenItIsRejected(FixtureRow<PlaceOrder> order)
	=> Context
		.GivenOrdering()
		.Given(order, (x, invalid) => x.Order = invalid)
		.WhenPlacingOrder()
		.Then(result => result.Status.Should().Be(PlacementStatus.Invalid));
`

Rows display by label and are evaluated against the test's own fixture.

## Building

```
dotnet test --solution FluentGwt.slnx
dotnet test --solution FluentGwt.slnx -p:FluentGwtIntegration=true
```

xunit v3 on Microsoft.Testing.Platform, opted into by the root `global.json`.
`docs/spec.md` is the behavioural specification the library is built from.
