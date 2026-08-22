# FluentGwt — behavioural specification

Status: settled. Written 2026-10-08 as a clean-room specification; the owner's rulings of
2026-10-08 on all twenty-one open questions are folded in (see
[Rulings, 2026-10-08](#33-rulings-2026-10-08)), including rulings 19–21, which supersede rulings 4
and 5. No questions are open.

This document describes **behaviour and capability only**. It was written from a reading of a
Given/When/Then test library the owner co-wrote at a previous employer (2019–20), a broad survey of
how that library's consumers actually used it, the owner's own 2020 library, and this repository
at `e1e3eed` (2023). No code, identifiers, file names or domain names from the previous employer's
library or its consumers appear here; where the old design is discussed it is described in plain
words ("the old fixture base", "the old test-host helper"). Every example is written fresh in the
intended new syntax, and the API names in it are the library's surface, used consistently and
summarised in [Public surface](#29-public-surface).

The implementer works from this document alone.

---

## Contents

1. [Reading this document](#1-reading-this-document)
2. [The execution model in one page](#2-the-execution-model-in-one-page)
3. [C1 — Fixture and service container lifecycle](#3-c1--fixture-and-service-container-lifecycle)
4. [C2 — State givens and transition givens](#4-c2--state-givens-and-transition-givens)
5. [C3 — Deferred givens and execution phases](#5-c3--deferred-givens-and-execution-phases)
6. [C4 — When steps, with and without results](#6-c4--when-steps-with-and-without-results)
7. [C5 — Then assertions](#7-c5--then-assertions)
8. [C6 — Exception expectations](#8-c6--exception-expectations)
9. [C7 — And chaining](#9-c7--and-chaining)
10. [C8 — Async and cancellation throughout](#10-c8--async-and-cancellation-throughout)
11. [C9 — Withdrawn](#11-c9--withdrawn)
12. [C10 — Teardown and fixture-scoped cancellation (usage)](#12-c10--teardown-and-fixture-scoped-cancellation-usage)
13. [C11 — Test configuration (usage)](#13-c11--test-configuration-usage)
14. [C12 — Integration gating and hosted services](#14-c12--integration-gating-and-hosted-services)
15. [C13 — Theory and data support](#15-c13--theory-and-data-support)
16. [C14 — Seeded test data and test identity](#16-c14--seeded-test-data-and-test-identity)
17. [C15 — Service overrides in the fixture container (usage)](#17-c15--service-overrides-in-the-fixture-container-usage)
18. [C16 — Outbound HTTP redirection in the fixture container](#18-c16--outbound-http-redirection-in-the-fixture-container)
19. [C17 — Test host: self-hosting a real ASP.NET app](#19-c17--test-host-self-hosting-a-real-aspnet-app)
20. [C18 — Host overrides: services, configuration, pipeline](#20-c18--host-overrides-services-configuration-pipeline)
21. [C19 — Reaching into a running host (usage)](#21-c19--reaching-into-a-running-host-usage)
22. [C20 — Test authentication and authorisation (usage)](#22-c20--test-authentication-and-authorisation-usage)
23. [C21 — Host-to-host and host-outbound HTTP redirection](#23-c21--host-to-host-and-host-outbound-http-redirection)
24. [C22 — Real-socket hosting (usage)](#24-c22--real-socket-hosting-usage)
25. [C23 — Host logs in test output, and log assertions (usage)](#25-c23--host-logs-in-test-output-and-log-assertions-usage)
26. [C24 — Time control](#26-c24--time-control)
27. [C25 — Analysers](#27-c25--analysers)
28. [Coverage matrix](#28-coverage-matrix)
29. [Public surface](#29-public-surface)
30. [What the old design got wrong, and what modern .NET makes unnecessary](#30-what-the-old-design-got-wrong-and-what-modern-net-makes-unnecessary)
31. [Package split](#31-package-split)
32. [Build order](#32-build-order)
33. [Rulings, 2026-10-08](#33-rulings-2026-10-08)
34. [Delivery: CI, branching, versioning and publishing](#34-delivery-ci-branching-versioning-and-publishing)

---

## 1. Reading this document

**Each capability section has four parts:** the problem it solves, an example in the new syntax,
acceptance criteria as test names the implementation is driven by, and coverage marks against the
2023 repository (`e1e3eed`) and the owner's 2020 library.

**"(usage)" in a heading** marks a capability that the old library did *not* provide as such, but
that the survey of its consumers showed was relied on — usually through copy-pasted helper code or
a workaround. The evidence is described generically in each section.

**About the 2020 library.** The owner's 2020 library turned out to be, behaviourally, the core of
the old employer library: the same fixture base, givens, deferral, when/then, exception and
integration behaviour, without the web test-host helpers. So its coverage marks track the old
core closely. Everything it offers can be reused freely; it is the owner's.

**About the 2023 repository.** It contains targeted givens (a chain started from any object) and
untargeted givens (a chain started from nothing), keyed/named state on the chain, and deferred
transitions that run in order when the chain executes. It has no When, no Then, no service
container, no host. Its root namespace is already `FluentGwt`; the package, assembly, repository
and solution are all named `FluentGwt` to match.

**Conventions in the examples** are the owner's: tabs, file-scoped namespaces, `var`,
expression-bodied members, nullable on, primary constructors, no `I` on the library's own
interfaces, no `T` on type parameters, no `Async` suffix, British spelling, xunit v3,
AwesomeAssertions, Moq at I/O edges, Bogus seeded.

**The fixture convention for this library** is a nested `internal sealed class Fixture`, exposed
on the test class as `private Fixture Context { get; } = new();`. It is `internal` because the
`XTests.Fluent.cs` steps are extension methods declared in another class, and a private nested type
cannot appear in their signatures.

A typical test class, used throughout:

```csharp
// OrderPlacementTests.cs
namespace Shop.Orders.Tests;

public sealed partial class OrderPlacementTests
{
	[Fact]
	public Task WhenStockIsAvailableThenOrderIsAccepted()
		=> Context
			.GivenDefaults()
			.GivenStock(5)
			.WhenPlacingOrder(x => x.Quantity = 2)
			.Then(x => x.Status.Should().Be(PlacementStatus.Accepted));
}
```

```csharp
// OrderPlacementTests.Fixture.cs
namespace Shop.Orders.Tests;

public sealed partial class OrderPlacementTests
{
	private Fixture Context { get; } = new();

	internal sealed class Fixture : ServiceFixture
	{
		public Mock<Warehouse> Warehouse { get; } = new(MockBehavior.Strict);
		public PlaceOrder Order { get; set; } = null!;
	}
}
```

```csharp
// OrderPlacementTests.Fluent.cs
namespace Shop.Orders.Tests;

using Fixture = OrderPlacementTests.Fixture;

internal static class OrderPlacementTestsFluent
{
	public static Given<Fixture> GivenDefaults(this Fixture fixture)
		=> fixture
			.Given(x => x.Services.AddOrdering())
			.Given(x => x.Services.Override(x.Warehouse.Object))
			.Given(x => x.Order = new(x.Fake.Commerce.Ean13(), 1));

	public static Given<Fixture> GivenStock(this Given<Fixture> given, int available)
		=> given.Given(x => x.Warehouse
			.Setup(m => m.Available(x.Order.Sku, It.IsAny<CancellationToken>()))
			.ReturnsAsync(available));

	public static When<Fixture, PlacementResult> WhenPlacingOrder(this Given<Fixture> given, Action<PlaceOrder>? change = null)
		=> given.When((x, cancellationToken) =>
		{
			change?.Invoke(x.Order);
			return x.Resolve<OrderInteractions>().Place(x.Order, cancellationToken);
		});
}
```

---

## 2. The execution model in one page

Everything else in this document hangs off this model, so it is stated once here.

**A chain is a description, not an execution.** Building `Context.Given(...).When(...).Then(...)`
runs nothing. Execution happens exactly once, when the chain's last step — a Then, or an `And`
after it — is awaited or converted to the `Task` a test method returns.

**Phases, in order, every time:**

| # | Phase | What runs |
|---|---|---|
| 1 | Immediate givens | Every given not marked deferred, in declaration order. State givens and transition givens interleave in the order written. |
| 2 | Host start | Every `FixtureHost` attached to the fixture (C17), started in attach order. An `ApplicationHost` attaches itself when it is created, and starts completely — the application's own hosted services included, whether or not integration is enabled, less any the test removed in arrangement (C12). |
| 3 | Deferred givens | Every deferred given, in the order it was deferred. |
| 4 | Integration start | Only when the test assembly carries the integration marker (C12): every `IHostedService` in the fixture container, started in registration order. |
| 5 | Act | The When step and any `And` act steps, in order. Exactly once. |
| 6 | Assert | Every Then and `And` assertion, in order. |
| 7 | Teardown | Always, whether phases 1–6 succeeded or not: hosted services stopped in reverse order; teardown callbacks (C10) in reverse registration order; attached hosts disposed in reverse attach order; the fixture container's provider disposed; the fixture disposed. |

**Failure attribution.** A failure in phases 1–4 fails the test as an *arrangement* failure and is
never treated as the act's exception — an exception expectation (C6) cannot be satisfied by a
given that threw. A failure in phase 7 is reported, but never masks an earlier failure.

**Cancellation.** Every phase receives the test's cancellation token (C8).

---

## 3. C1 — Fixture and service container lifecycle

### Problem

Component tests resolve the real thing through the real container. That only works if the test
can compose a `ServiceCollection` freely during arrangement, and if a composition mistake — a
registration made *after* the provider was built, which a built provider silently ignores — fails
the test loudly instead of letting it pass against the wrong graph. The old fixture base did
exactly this, and it was the most valued property in the survey: almost every service-level suite
registered the product's `AddX` and then overrode edges in later givens.

The container must also be cleaned up reliably. The survey showed suites implementing disposal by
hand on the test class because the old library disposed the fixture on some assertion paths and
not others (see [§30](#30-what-the-old-design-got-wrong-and-what-modern-net-makes-unnecessary)).

### Behaviour

- The fixture base exposes a mutable `Services` collection.
- The **first resolution** builds the provider exactly once and **locks** the collection. Any later
  attempt to obtain `Services` for mutation throws `InvalidOperationException` whose message names
  every service type resolved so far, so the reader can see which given resolved too early.
- Resolution is thread-safe per fixture. Two fixtures never contend (the old library used one
  global lock across every fixture in the process).
- **The provider is built with `ValidateOnBuild` and `ValidateScopes` on by default**, so the
  first resolution validates the whole graph: an unconstructable registration, or a scoped service
  resolved from the root, fails there rather than at whichever resolution happens to reach it. A
  validation failure in phases 1–4 is an arrangement failure carrying the container's own
  exception, which names every offending registration.
- The opt-out is per fixture: `ValidateOnBuild` and `ValidateScopes` are `bool` properties on the
  fixture base, both `true` by default, settable in the fixture class or in any given until the
  first resolution. Setting either afterwards throws `InvalidOperationException`, as `Services`
  does. Tests that deliberately compose a partial graph turn `ValidateOnBuild` off.
- `IsResolvable<Service>()` answers whether the real provider can construct the service, and is the
  basis of resolvable-first tests. It resolves from the real provider and therefore also locks.
  With validation on, it is also `false` when the provider fails validation, and the validation
  exception is written to the test output (C23) so the reason is visible.
- `WhenResolving<Service>()` is a built-in When step returning the resolved instance. It is
  available on every chain and checks at run time that the target is a `ServiceFixture`, failing
  with a message naming the actual target otherwise. (A C# 14 extension block constrained to
  fixtures would need every type argument written — `WhenResolving<Fixture, Service>()` — because
  an explicit type argument list cannot leave the receiver's type parameter to inference.)
- At the end of the chain the provider is disposed (async where the service is `IAsyncDisposable`),
  then the fixture's own `DisposeAsync` runs. This happens on every path.

### Example

```csharp
[Fact]
public Task WhenComposedThenOrderInteractionsAreResolvable()
	=> Context
		.GivenDefaults()
		.WhenResolving<OrderInteractions>()
		.Then(x => x.Should().NotBeNull());

[Fact]
public Task WhenRegisteringAfterResolutionThenArrangementFails()
	=> Context
		.GivenDefaults()
		.Given(x => x.Resolve<OrderInteractions>())
		.Given(x => x.Services.AddSingleton<Clock, FixedClock>())
		.WhenPlacingOrder()
		.ThenArrangementFails<InvalidOperationException>(e => e.Message.Should().Contain(nameof(OrderInteractions)));
```

### Acceptance criteria

- `WhenServiceRegisteredBeforeResolutionThenResolveReturnsIt`
- `WhenServicesAccessedAfterResolutionThenInvalidOperationIsThrown`
- `WhenRegistrationIsRefusedThenMessageNamesEveryResolvedService`
- `WhenResolvedTwiceThenOneProviderIsBuilt`
- `WhenResolvedConcurrentlyThenOneProviderIsBuilt`
- `WhenTwoFixturesResolveConcurrentlyThenNeitherWaitsForTheOther`
- `WhenServiceIsComposableThenIsResolvableIsTrue`
- `WhenDependencyIsMissingThenIsResolvableIsFalse`
- `WhenRegistrationIsUnconstructableThenFirstResolutionFailsAsArrangement`
- `WhenScopedServiceIsResolvedFromRootThenResolutionFails`
- `WhenValidationFailsThenIsResolvableIsFalseAndReasonIsWritten`
- `WhenValidateOnBuildIsTurnedOffThenPartialGraphResolves`
- `WhenValidationIsChangedAfterResolutionThenInvalidOperationIsThrown`
- `WhenResolvingServiceThenWhenResultIsTheInstance`
- `WhenChainSucceedsThenProviderIsDisposed`
- `WhenAssertionFailsThenProviderIsStillDisposed`
- `WhenResultAssertionCompletesThenFixtureIsDisposed`
- `WhenServiceIsAsyncDisposableThenItIsDisposedAsynchronously`
- `WhenFixtureOverridesDisposeThenItRunsAfterProviderDisposal`

### Coverage

- **2023: No.** No service container, no fixture base.
- **2020: Yes, with defects.** Lock-after-resolve with a message naming resolved types, a
  resolvable check, disposal of the fixture after fixture assertions. Defects: global lock; the
  resolvable check built a throwaway provider (duplicating singletons and their side effects);
  result assertions never disposed the fixture.

---

## 4. C2 — State givens and transition givens

### Problem

Two different kinds of arrangement keep being conflated. A **state given** says "this value
exists": a model, an identifier, an expected response. A **transition given** says "do this to the
system": register a service, configure a mock, seed a row. They behave differently — state can be
read back and replaced; a transition runs once, at execution, in order — and a chain that can
express both lets steps share data without stuffing everything onto the fixture as mutable
properties. The survey showed fixtures growing to dozens of settable properties purely to pass
values between steps.

The 2023 repository already draws this distinction and its semantics are kept.

### Behaviour

Kept from 2023:

- `target.Given()` starts a **targeted** chain on any object; `Given.With(...)` starts an
  **untargeted** chain.
- `Given(value)`, `Given(name, value)` and `Given(key, value)` store **state** on the chain, keyed
  by type plus an optional name or key. Storing again under the same key replaces the value.
  A null name or key throws `ArgumentNullException`.
- `Get<Value>()`, `Get<Value>(name)`, `Get<Value>(key)` read state back; a missing entry throws
  `InvalidOperationException` naming the type and key.
- Adding unnamed state of the same type as the chain's target throws, because it would replace
  the target.
- `Given(x => ...)` and `Given(async x => ...)` record **transitions**, which do not run when
  declared, and run in declaration order at execution.

New:

- State is readable **inside** transitions and When steps through the chain context, so a
  transition can use state declared before *or after* it (state is assigned in phase 1 order, and a
  later state given replaces earlier state before any reader that runs after it).
- Transitions may take the cancellation token (C8).
- Transitions on a `ServiceFixture` target receive the fixture.

### Example

```csharp
public static Given<Fixture> GivenCustomer(this Given<Fixture> given, string tier)
	=> given
		.Given("tier", tier)
		.Given((x, state) => x.Customers
			.Setup(m => m.Tier(x.Order.CustomerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(state.Get<string>("tier")));
```

### Acceptance criteria

The 2023 tests stand (targeted and untargeted instantiation, named and keyed state, replacement,
null-key rejection, deferral, ordered execution). Additionally:

- `WhenStateIsReplacedBeforeExecutionThenTransitionSeesReplacement`
- `WhenTransitionReadsStateThenStateIsAvailableAtExecution`
- `WhenStateIsMissingThenMessageNamesTypeAndKey`
- `WhenTransitionsAreDeclaredThenNoneRunBeforeExecution`
- `WhenStateAndTransitionsInterleaveThenTheyApplyInDeclarationOrder`
- `WhenTransitionTakesCancellationThenItReceivesTheTestToken`

### Coverage

- **2023: Yes** for targeted/untargeted chains, keyed/named state and deferred ordered
  transitions. **No** for state visible inside transitions, cancellation, or any connection to
  When/Then.
- **2020: Partly.** Transitions on the fixture only. No chain state; everything was a fixture
  property.

---

## 5. C3 — Deferred givens and execution phases

### Problem

Some givens must run after all others regardless of where they are written. The survey shows two
distinct reasons, and both were common (about one service suite in three used deferral):

1. **Resolution must wait for registration.** Seeding a database through the container resolves a
   service, which locks the collection (C1). If a later given still needs to register a mock, the
   seed must run last.
2. **Configuration must precede start.** A given that starts a host must see every override any
   later given adds.

Consumers deferred by marking the *previous* given, and had to remember to do it on every step that
resolved anything; forgetting produced the lock exception from C1 far from its cause. Host start
was the commonest deferred step, and seeding the running host had to be deferred *after* it.

### Behaviour

- `.Deferred()` marks the given immediately before it as deferred. Calling it with no preceding
  given throws `InvalidOperationException`. "Given" here means a transition: state givens are
  stored when declared and have nothing to defer, so `.Deferred()` after one marks the transition
  before it. It is available on targeted chains, which is where fixtures live.
- Deferred givens run in phase 3, after host start, in the order they were deferred.
- Host start is **not** a given the user defers: it is phase 2, automatic, and happens after all
  immediate givens however the chain is written (C17). This removes the commonest reason to defer.
- Deferred givens may themselves add state and transitions; anything they add runs immediately
  within phase 3, not at the end of the chain.

### Example

```csharp
public static Given<Fixture> GivenOrderOnFile(this Given<Fixture> given)
	=> given
		.Given(async (x, cancellationToken) =>
		{
			await using var scope = x.Resolve<IServiceScopeFactory>().CreateAsyncScope();
			var orders = scope.ServiceProvider.GetRequiredService<OrderStore>();
			await orders.Save(x.Order, cancellationToken);
		})
		.Deferred();
```

### Acceptance criteria

- `WhenGivenIsDeferredThenItRunsAfterImmediateGivens`
- `WhenSeveralGivensAreDeferredThenTheyRunInDeferralOrder`
- `WhenDeferredGivenResolvesThenLaterImmediateRegistrationsStillApply`
- `WhenDeferredIsCalledFirstThenInvalidOperationIsThrown`
- `WhenHostIsConfiguredThenDeferredGivensRunAgainstTheStartedHost`
- `WhenDeferredGivenAddsTransitionThenItRunsWithinTheDeferredPhase`
- `WhenImmediateGivenFailsThenNoDeferredGivenRuns`

### Coverage

- **2023: No.** Transitions are deferred until execution, but there is no second phase.
- **2020: Yes.** Mark-the-previous-given deferral, FIFO. No automatic host phase.

---

## 6. C4 — When steps, with and without results

### Problem

The act must be one, explicit, exactly-once step whose result flows to the assertions. Most facts
in the survey were "make a request, assert the response"; others were "call a method, assert its
result"; others "do something, assert on the fixture's mocks".

### Behaviour

- `When(x => ...)` declares an act without a result; Then then asserts on the fixture.
- `When(x => result)` declares an act with a result.
- Each has async forms returning `Task`, `ValueTask`, `Task<Result>` or `ValueTask<Result>`, and
  forms taking the cancellation token.
- The act runs **exactly once** per chain, however many assertions follow.
- A chain has one When. A second act is expressed with `And` (C7), not with a second When.
- The old library offered a two-stage "prepare, then act" lambda signature, and a way to name a
  method group and supply its arguments later. The first is unnecessary (preparation can happen
  inside the act lambda); the second was used by one suite and cost a large generic-arity overload
  set. **Neither is carried forward.**

### Example

```csharp
public static When<Fixture, HttpResponseMessage> WhenGettingForecast(this Given<Fixture> given, string city)
	=> given.When((x, cancellationToken) => x.Weather.CreateClient().GetAsync($"/forecast/{city}", cancellationToken));
```

### Acceptance criteria

- `WhenActHasNoResultThenThenReceivesTheFixture`
- `WhenActHasResultThenThenReceivesTheResult`
- `WhenSeveralAssertionsFollowThenActRunsOnce`
- `WhenActIsValueTaskThenItIsAwaited`
- `WhenActTakesCancellationThenItReceivesTheTestToken`
- `WhenChainIsNeverAwaitedThenActNeverRuns`
- `WhenGivensFailThenActDoesNotRun`

### Coverage

- **2023: No.**
- **2020: Yes.** Act with and without result, memoised; the curried and method-group forms
  described above.

---

## 7. C5 — Then assertions

### Problem

Assertions need the result, the fixture, or both, and a test method must be able to return the
whole chain as one expression.

### Behaviour

- `Then(result => ...)`: assertion on the result.
- `Then((x, result) => ...)`: assertion on fixture and result.
- `ThenFixture(x => ...)`: assertion on the fixture after an act with a result (a separate name,
  because an overload taking the fixture would be ambiguous with one taking the result whenever the
  lambda body compiles against both).
- After an act without a result, `Then(x => ...)` receives the fixture.
- All forms have async variants.
- Then returns the library's own chain type (`Then<Target>`, `Then<Target, Result>`), not a
  `Task`, so `.And(...)` can follow it (C7). The chain type is **awaitable and implicitly
  convertible to `Task`**, so a fact can be an expression-bodied member returning `Task` whether it
  ends in a Then or an `And`. Awaiting or converting triggers execution.
- An assertion failure propagates the assertion library's own exception unchanged, so the test
  runner shows the assertion's message.
- The core package depends on AwesomeAssertions (Apache-2.0) for the failures it raises itself
  (C6). Assertions written in consumer lambdas may use any library.

### Example

```csharp
[Fact]
public Task WhenCityIsUnknownThenForecastIsNotFound()
	=> Context
		.GivenDefaults()
		.WhenGettingForecast("Atlantis")
		.Then(x => x.StatusCode.Should().Be(HttpStatusCode.NotFound));

[Fact]
public Task WhenOrderIsPlacedThenWarehouseIsReserved()
	=> Context
		.GivenDefaults()
		.GivenStock(5)
		.WhenPlacingOrder()
		.ThenFixture(x => x.Warehouse.Verify(m => m.Reserve(x.Order.Sku, 1, It.IsAny<CancellationToken>()), Times.Once));
```

### Acceptance criteria

- `WhenThenAssertsResultThenItReceivesTheActResult`
- `WhenThenAssertsFixtureAndResultThenItReceivesBoth`
- `WhenThenFixtureIsUsedThenItReceivesTheFixtureAfterTheAct`
- `WhenAssertionFailsThenOriginalAssertionExceptionPropagates`
- `WhenThenIsReturnedAsTaskThenChainExecutes`
- `WhenThenIsAwaitedThenChainExecutes`
- `WhenAndFollowsThenInExpressionBodiedFactThenChainExecutes`
- `WhenAsyncAssertionIsUsedThenItIsAwaited`
- `WhenThenIsAwaitedTwiceThenChainExecutesOnce`

### Coverage

- **2023: No.**
- **2020: Yes.** Result, fixture-and-result and fixture assertions, the last through an
  intermediate step. Then returned a plain `Task`.

---

## 8. C6 — Exception expectations

### Problem

Protocol validation and error-path tests expect the act to throw a specific exception and then
inspect it, and sometimes also verify the fixture's mocks afterwards. The survey showed one suite
with well over a hundred exception-expecting facts, and others asserting on exception messages.

### Behaviour

- `ThenThrows<Failure>()` passes when the act throws `Failure` or a type derived from it;
  `ThenThrowsExactly<Failure>()` requires the exact type.
- Both accept an optional assertion on the exception, and on fixture and exception.
- If the act does not throw, the test fails with a message naming the expected type.
- If the act throws an unrelated type, the test fails, and the failure's message carries the actual
  exception in full — its type, message and stack. (AwesomeAssertions raises its own failure type
  and cannot attach an inner exception, so the message is the carrier.)
- These failures are raised through AwesomeAssertions, so their messages read like every other
  assertion failure in the suite (expected type, actual type, the actual exception's message).
- **Only the act is covered.** An exception from a given fails the test as an arrangement failure
  (§2), even if its type matches. (The old library ran the givens inside the same expectation, so a
  broken given could satisfy it.)
- `ThenArrangementFails<Failure>()` exists for the rare test whose *subject* is arrangement — the
  library's own tests, and composition tests such as the lock in C1.
- `And(x => ...)` after an exception expectation asserts on the fixture (C7).

### Example

```csharp
[Fact]
public Task WhenFrameIsTruncatedThenDecodingThrows()
	=> Context
		.GivenDefaults()
		.WhenDecoding(x => x.Frame[..^1])
		.ThenThrows<MalformedFrameException>(e => e.Offset.Should().BeGreaterThan(0))
		.And(x => x.Metrics.Verify(m => m.Rejected(), Times.Once));
```

### Acceptance criteria

- `WhenActThrowsExpectedTypeThenExpectationPasses`
- `WhenActThrowsDerivedTypeThenThrowsPasses`
- `WhenActThrowsDerivedTypeThenThrowsExactlyFails`
- `WhenActDoesNotThrowThenFailureNamesExpectedType`
- `WhenActThrowsUnrelatedTypeThenFailureCarriesActualException`
- `WhenGivenThrowsExpectedTypeThenExpectationDoesNotPass`
- `WhenAssertionOnTheExceptionFailsThenItsMessagePropagates`
- `WhenArrangementFailsAsExpectedThenThenArrangementFailsPasses`

### Coverage

- **2023: No.**
- **2020: Yes, with the defect above** (givens inside the expectation). Depended on an old
  assertion library for the check itself.

---

## 9. C7 — And chaining

### Problem

Chains read as sentences. Three different "and"s were used in the survey, and they mean different
things depending on where they appear.

### Behaviour

| Position | Meaning |
|---|---|
| After a given | `And(x => ...)` is another transition; identical to `Given(x => ...)`, for reading. |
| After a When | `And(x => ...)` is a further act step run after the When, in phase 5. The When's result is preserved. `AndResult(x => result)` replaces it (used in the survey to "cancel, then await completion and assert on the completion"). |
| After a Then or an exception expectation | `And(...)` is a further assertion, run in order in phase 6. After a Then on a result, `And` receives the result; `AndFixture` receives the fixture. |

- Every `And` has sync, async and cancellation-taking forms.
- An `And` assertion runs even if a previous assertion in the chain passed; the first failing
  assertion fails the test and the rest do not run.

### Example

```csharp
[Fact]
public Task WhenCancelledThenConnectionObservesCancellation()
	=> Context
		.GivenDefaults()
		.GivenConnectedInBackground()
		.When(x => x.Cancellation.Cancel())
		.And((x, cancellationToken) => x.Completion.WaitAsync(cancellationToken))
		.Then(x => x.ObservedToken.IsCancellationRequested.Should().BeTrue());

[Fact]
public Task WhenOrderIsPlacedThenResponseAndStoreAgree()
	=> Context
		.GivenDefaults()
		.WhenPlacingOrder()
		.Then(x => x.Status.Should().Be(PlacementStatus.Accepted))
		.AndFixture(x => x.Store.Orders.Should().ContainSingle());
```

### Acceptance criteria

- `WhenAndFollowsGivenThenItRunsAsATransitionInOrder`
- `WhenAndFollowsWhenThenItRunsAfterTheAct`
- `WhenAndFollowsWhenThenResultIsPreserved`
- `WhenAndResultFollowsWhenThenResultIsReplaced`
- `WhenAndFollowsThenThenItRunsAsAnAssertion`
- `WhenAndFixtureFollowsThenThenItReceivesTheFixture`
- `WhenAndFollowsThrowsThenItReceivesTheFixture`
- `WhenFirstAssertionFailsThenLaterAssertionsDoNotRun`

### Coverage

- **2023: No.** (Givens chain with `.Given`, not `.And`.)
- **2020: Yes.** All three positions, with a result-replacing form after When.

---

## 10. C8 — Async and cancellation throughout

### Problem

Every step in a real component test does I/O. The old library supported async lambdas, but not on
every step, so consumers wrote their own givens returning a `Task` of the chain and then needed a
second family of steps extending that `Task` — about one suite in six did this, doubling their
helper code. Hosted services were started with no cancellation at all, so a hung start hung the
run.

### Behaviour

- Every step accepts sync, `Task` and `ValueTask` forms. No step returns a `Task` of the chain; the
  chain is lazy, so async work inside a given never needs the chain itself to be awaited.
- Every Given, When and `And` step after either has a form taking a `CancellationToken`, in `Task` and `ValueTask` shapes. Assertions do not: on a When with a result, `Then((x, result) => ...)` and a token form `(result, cancellationToken) => ...` would both be two-parameter lambdas, and an `async (x, result) => ...` would silently bind as the token form.
- The token is the **test's** token: with the xunit package referenced, it is
  `TestContext.Current.CancellationToken`, supplied without per-test code. Without it, `None`.
- The fixture's own `Cancellation` token (C10) is linked to it.
- The library never blocks on async work (`.Result`, `.Wait()`).

### Example

```csharp
public static Given<Fixture> GivenForecastCached(this Given<Fixture> given)
	=> given.Given(async (x, cancellationToken) =>
		await x.Resolve<ForecastCache>().Store(x.Get<Forecast>(), cancellationToken));
```

### Acceptance criteria

- `WhenGivenIsAsyncThenItCompletesBeforeTheNextGiven`
- `WhenGivenReturnsValueTaskThenItIsAwaited`
- `WhenTestIsCancelledThenStepsReceiveACancelledToken`
- `WhenXunitPackageIsReferencedThenTokenIsTheTestContextToken`
- `WhenXunitPackageIsAbsentThenTokenIsNone`
- `WhenStepThrowsOperationCancelledThenTestReportsCancellation`

### Coverage

- **2023: Partly.** Async transitions (`Task` only); no cancellation.
- **2020: Partly.** `Task` lambdas on most steps; no `ValueTask`; no cancellation.

---

## 11. C9 — Withdrawn

Withdrawn 2026-10-08 (tolerating a failing act); an act expected to fail is asserted with C6.

---

## 12. C10 — Teardown and fixture-scoped cancellation (usage)

### Problem

**Usage evidence:** suites that started background work in a given (a connection pumping data, a
task awaiting a pipe) put a `CancellationTokenSource` on every fixture and cancelled it by hand in
assertions, so background tasks would end. A suite running against a real database deleted the
database inside a `finally` block *within the act*, because the library had no teardown step.
Suites hosting applications cancelled and disposed every host by hand in a fixture `Dispose`.

### Behaviour

- `ServiceFixture.Cancellation` is a token linked to the test token and cancelled at the start of
  teardown, before anything is disposed.
- `x.OnTeardown(Func<CancellationToken, ValueTask>)` registers cleanup from any step; callbacks run
  in reverse registration order in phase 7.
- Every teardown callback runs even if an earlier one throws; all teardown failures are aggregated
  and reported after any primary failure, never instead of it. Concretely: a primary failure alone
  propagates unchanged; teardown failures alone propagate as themselves (one) or as an
  `AggregateException` (several); both together propagate as one `AggregateException` whose first
  inner exceptions are the primary failure's. Callbacks receive `CancellationToken.None` until the
  xunit package supplies the test token.
- Teardown runs on every path, including arrangement failure (for whatever was arranged so far).

### Example

```csharp
public static Given<Fixture> GivenScratchDatabase(this Given<Fixture> given)
	=> given.Given(async (x, cancellationToken) =>
	{
		var database = await ScratchDatabase.Create(x.TestId, cancellationToken);
		x.Services.AddSingleton(database);
		x.OnTeardown(token => database.Drop(token));
	});
```

### Acceptance criteria

- `WhenChainEndsThenFixtureCancellationIsSignalled`
- `WhenTestTokenIsCancelledThenFixtureCancellationIsSignalled`
- `WhenTeardownCallbacksAreRegisteredThenTheyRunInReverseOrder`
- `WhenTeardownCallbackThrowsThenLaterCallbacksStillRun`
- `WhenAssertionAndTeardownBothFailThenAssertionFailureIsReported`
- `WhenGivenFailsThenTeardownStillRunsForWhatWasArranged`

### Coverage

- **2023: No.**
- **2020: No** (only fixture `IDisposable`, called on some paths).

---

## 13. C11 — Test configuration (usage)

### Problem

**Usage evidence:** test projects carried an `appsettings.json` in their output (holding the
integration switch and placeholders for real-environment settings) and user secrets for real
credentials; integration-capable suites read these. Every hosted suite also built an in-memory
configuration dictionary per host.

### Behaviour

- `ServiceFixture.Configuration` is built once per process from, in increasing precedence: an
  optional `appsettings.json` in the test output directory, an optional
  `appsettings.{environment}.json`, user secrets for the test assembly where configured, and
  environment variables.
- The same `IConfiguration` is registered in the fixture container with `TryAdd`, so a test's own
  registration wins.
- `x.Configure(key, value)` overlays a value for this fixture only.
- The seed override (C14) is read from this configuration. Integration is **not**: it is decided
  when the test assembly is built, and no configuration key or environment variable enables it
  (C12).

### Example

```csharp
public static Given<Fixture> GivenRegion(this Given<Fixture> given, string region)
	=> given.Given(x => x.Configure("Ordering:Region", region));
```

### Acceptance criteria

- `WhenAppSettingsFileIsPresentThenValuesAreAvailable`
- `WhenEnvironmentVariableIsSetThenItOverridesAppSettings`
- `WhenValueIsConfiguredOnFixtureThenItOverridesEverySource`
- `WhenFixtureValueIsConfiguredThenOtherFixturesDoNotSeeIt`
- `WhenTestRegistersItsOwnConfigurationThenItWins`
- `WhenNoSettingsFileExistsThenConfigurationStillBuilds`

### Coverage

- **2023: No.**
- **2020: Partly** — the settings file plus environment variables were read, but only for the
  integration switch.

---

## 14. C12 — Integration gating and hosted services

### Problem

An integration test touches something outside the process. It must be skipped by default — with a
visible reason — and run when a machine is set up for it. When it runs, the background parts of
the system (`IHostedService`s: consumers, pollers, schedulers) in the fixture container must
actually run for the duration of the act, and stop afterwards.

A test that hosts the real application (C17) is a different case. The application starts as it
would in production, background services and all; a test that does not want one of them running
says so in its arrangement, by name.

**Usage evidence, cautionary:** the gate was barely used. The survey found one test project that
declared the switch (set to off) and *no* use of the gated attribute anywhere; meanwhile a suite
that required a real local database ran ungated on every machine. That argues for a gate that
says, at the point of use, *why* a test is an integration test — so an ungated test that does the
same thing stands out in review. The owner's 2016 work required exactly that declaration, and it is
revived here.

The old switch was a configuration value read at run time, so whether a run included integration
tests depended on whichever settings file or environment variable happened to be present. Here
the choice belongs to the build: it is visible in the command that produced the test assembly.

### Behaviour

- **The switch is an MSBuild property**, `FluentGwtIntegration`. Integration is enabled for a test
  assembly built with it set to `true` — `dotnet test -p:FluentGwtIntegration=true`, or
  `<FluentGwtIntegration>true</FluentGwtIntegration>` in the project or a `Directory.Build.props`.
  Unset, empty or any other value is off (`true` is compared case-insensitively, as MSBuild
  compares). There is **no run-time switch**: no configuration key, no environment variable (C11).
- **The xunit package ships `buildTransitive` MSBuild targets**, so every project that references
  the package, directly or transitively, imports them. When the property is `true` they:
  1. **append** `INTEGRATION` to `DefineConstants`, so consumer code may use `#if INTEGRATION`.
     Appended, never replacing: `DEBUG`, `TRACE` and the target-framework symbols survive. Nothing
     in the library requires the symbol.
  2. add an `AssemblyAttribute` item for `FluentGwt.IntegrationEnabledAttribute`, so the SDK's
     generated assembly info carries the **marker** `[assembly: IntegrationEnabled]`.

  When the property is not `true`, they do neither. Whatever the property, they also stamp
  `[assembly: FluentGwt.XunitTestRunner]`, which is how core finds the test token (C8). Changing the property changes the generated
  assembly info, so an incremental build recompiles; no clean build is needed.
  - **Why a dedicated property** rather than `-p:DefineConstants=INTEGRATION`: a property set on
    the command line replaces the project's `DefineConstants` outright, dropping `DEBUG`, `TRACE`
    and the target-framework symbols — and a compilation symbol alone leaves nothing to read at
    run time.
- **The marker** `IntegrationEnabledAttribute` is an argument-less, assembly-level attribute defined
  in the core package, so the core reads it without the xunit package. A project that does not use
  the xunit package may write `[assembly: IntegrationEnabled]` itself.
- **Where the marker is read.** The integration attributes read it, through `IntegrationGate.IsOpen`,
  from the entry assembly — the test executable itself under Microsoft.Testing.Platform. (xunit's
  dynamic skip calls a static property, which cannot know the test class, so the test class's own
  assembly is not available to it.) The fixture reads it from the assembly that declares its own
  runtime type, and
  exposes the answer as `ServiceFixture.IntegrationEnabled`. Under the library's convention — a
  nested `Fixture` in the test class — both are the test assembly. It is read per evaluation;
  nothing is cached for the process. "Integration is enabled", everywhere in this document, means
  the marker is present.
- **Gating is attribute-only** (xunit package): `[IntegrationFact]` and `[IntegrationTheory]`.
  There is no chain step for it.
  - Each takes an `IntegrationJustification` and a reason, in that order, followed by the
    caller-file/line parameters xunit v3 requires.
  - `IntegrationJustification` is a `[Flags]` enum: `NetworkIo`, `DiskIo`, `UnsafeCode`,
    `MultipleThreads`, `ThreadSynchronisation`. It says *what* makes the test an integration test;
    the reason says *why*, in the author's words.
  - A zero justification or an empty reason is reported as a failed test, never run and never
    skipped.
  - Both use xunit v3's native dynamic skip. With the marker absent, the test is reported as
    *skipped* with the message `Integration disabled (built without FluentGwtIntegration=true).
    Justification: NetworkIo, MultipleThreads. Reason: <reason>`; for a theory, every row is
    skipped with that message. With the marker present, the test runs.
  - The attribute is written once per test. No `#if` around it is needed: an assembly built without
    the property still compiles, discovers and reports every integration test, as skipped.
- **Fixture-container hosted services** — when integration is enabled, phase 4 resolves every
  `IHostedService` in the fixture container and starts each with the test token, in registration
  order. Phase 7 stops them in reverse order with the test token.
  - A start failure fails the test as an arrangement failure.
  - A stop failure is reported as a teardown failure.
  - Services implementing `IHostedLifecycleService` receive the starting/started and
    stopping/stopped calls in the order the generic host would make them.
  - When integration is disabled, fixture-container hosted services are **not** started.
- **Test-host hosted services** (AspNetCore package) — a test host (C17) starts **completely**,
  including the application's own hosted services, **whether or not integration is enabled**. The
  marker has no effect on hosts. A host's hosted-service start failure is a host start failure
  (C17).
- **Removing hosted services** (core) — extension methods on `IServiceCollection`, so they work on
  the fixture's `Services` (keeping a service out of phase 4) and inside a host's
  `ConfigureServices` callback (keeping it out of a host). The callback runs against the
  application's real collection after its own composition (C18), so a removal there sees every
  registration the application made.
  - `RemoveHostedService<Implementation>()` and `RemoveHostedService(Type)` remove every
    `IHostedService` registration whose implementation is exactly that type. A registration's
    **implementation** is:
    - for a type registration, the descriptor's implementation type;
    - for an instance registration, the instance's runtime type;
    - for a factory registration, the factory delegate's declared return type.
      `AddHostedService<Implementation>(factory)` produces a delegate typed
      `Func<IServiceProvider, Implementation>`, so the type is known without invoking it. A factory
      declared as returning `IHostedService` or `object` has **no knowable implementation**: it is
      never matched, and the library never invokes a factory to find out.
  - Only unkeyed registrations are considered; the generic host never starts a keyed
    `IHostedService`.
  - If nothing matches, the test fails as an arrangement failure naming the type, and the message
    gives the number of factory registrations with no knowable implementation, since the service
    may be one of them. A renamed service therefore cannot silently start running again.
  - `RemoveApplicationHostedServices()` removes every hosted service except the **framework's**. A
    registration is the framework's if its implementation (by the rule above) is defined in an
    assembly named `Microsoft.AspNetCore` or `Microsoft.Extensions`, or whose name starts with
    `Microsoft.AspNetCore.` or `Microsoft.Extensions.`. The web server's own host service is
    therefore never removed, and the host still serves requests. A factory registration with no
    knowable implementation is kept, and written to the test output (C23). A framework hosted
    service the application opted into itself (a health-check publisher, say) is kept too; a test
    that does not want it names it with `RemoveHostedService(Type)`.
  - Removal is a point in the sequence: a hosted service registered after it — by the test, later
    in the same `ConfigureServices` callback or in a later one — is kept.

### Example

```csharp
[IntegrationFact(IntegrationJustification.NetworkIo | IntegrationJustification.MultipleThreads, "Publishes to the real message bus and waits for the dispatch consumer")]
public Task WhenOrderIsPublishedThenDispatchConsumesIt()
	=> Context
		.GivenDefaults()
		.GivenMessageBus()
		.WhenPublishing(x => x.Order)
		.Then(x => x.Dispatched.Should().ContainSingle());
```

Run with integration on a machine set up for it; without the property the test is reported as
skipped:

```shell
dotnet test -p:FluentGwtIntegration=true
```

The symbol is there for code that only an integration build can compile — here, a given using a
client package the project references only when the property is set:

```csharp
#if INTEGRATION
	public static Given<Fixture> GivenMessageBus(this Given<Fixture> given)
		=> given.Given(x => x.Services.AddMessageBusClient(x.Configuration));
#endif
```

A host-level test that does not want the application's background work running:

```csharp
public static Given<Fixture> GivenNoOutboxRelay(this Given<Fixture> given)
	=> given.Given(x => x.Api.ConfigureServices(services => services.RemoveHostedService<OutboxRelay>()));

public static Given<Fixture> GivenNoBackgroundWork(this Given<Fixture> given)
	=> given.Given(x => x.Api.ConfigureServices(services => services.RemoveApplicationHostedServices()));
```

### Acceptance criteria

- `WhenMarkerIsAbsentThenIntegrationFactIsSkippedWithJustificationAndReason`
- `WhenMarkerIsPresentThenIntegrationFactRuns`
- `WhenMarkerIsAbsentThenEveryIntegrationTheoryRowIsSkippedWithTheMessage`
- `WhenMarkerIsPresentThenIntegrationTheoryRuns`
- `WhenIntegrationFactHasNoJustificationOrReasonThenTestFails`
- `WhenIntegrationPropertyIsSetThenIntegrationSymbolIsDefined`
- `WhenIntegrationPropertyIsSetThenMarkerIsEmitted`
- `WhenIntegrationPropertyIsUnsetThenNoSymbolAndNoMarker`
- `WhenIntegrationPropertyIsNotTrueThenNoSymbolAndNoMarker`
- `WhenIntegrationPropertyIsSetThenOtherConstantsArePreserved`
- `WhenXunitPackageIsReferencedTransitivelyThenTargetsStillApply`
- `WhenMarkerIsPresentThenFixtureReportsIntegrationEnabled`
- `WhenMarkerIsPresentThenHostedServicesStartBeforeTheAct`
- `WhenMarkerIsPresentThenHostedServicesStopAfterAssertions`
- `WhenMarkerIsPresentThenHostedServicesStopInReverseOrder`
- `WhenMarkerIsAbsentThenHostedServicesAreNotStarted`
- `WhenAssertionFailsThenHostedServicesAreStillStopped`
- `WhenHostedServiceFailsToStartThenTestFailsAsArrangement`
- `WhenHostedServiceStartsThenItReceivesTheTestToken`
- `WhenLifecycleServiceIsRegisteredThenAllLifecycleCallsAreMade`
- `WhenMarkerIsAbsentThenApplicationHostStartsApplicationHostedServices`
- `WhenMarkerIsPresentThenApplicationHostStartsApplicationHostedServices`
- `WhenHostedServiceIsRemovedByTypeThenItDoesNotStart`
- `WhenHostedServiceIsRegisteredAsInstanceThenRemovalByTypeRemovesIt`
- `WhenHostedServiceIsRegisteredByTypedFactoryThenRemovalByTypeRemovesIt`
- `WhenFactoryReturnTypeIsNotKnowableThenRemovalByTypeDoesNotMatchIt`
- `WhenKeyedHostedServiceIsRegisteredThenRemovalIgnoresIt`
- `WhenRemovedHostedServiceIsNotRegisteredThenArrangementFailsNamingTheType`
- `WhenRemovalMatchesNothingThenMessageCountsUnknowableFactories`
- `WhenHostedServiceIsRemovedFromFixtureServicesThenPhaseFourDoesNotStartIt`
- `WhenApplicationHostedServicesAreRemovedThenFrameworkHostedServicesAreKept`
- `WhenApplicationHostedServicesAreRemovedThenApplicationHostStillServesRequests`
- `WhenApplicationHostedServicesAreRemovedThenUnknowableFactoriesAreKeptAndWritten`
- `WhenTestAddsHostedServiceAfterRemovalThenItIsKept`

### Coverage

- **2023: No.**
- **2020: Yes, with defects.** A fact attribute deciding skip in its constructor (static, cached
  for the process) from a run-time configuration value, with no justification; hosted services
  started and stopped around the act with no cancellation, and start/stop failures logged and
  swallowed. No test hosts.

---

## 15. C13 — Theory and data support

### Problem

Theories need data that is (a) typed, (b) able to depend on the per-test fixture — a value derived
from the fixture's seeded data, or a delegate that mutates a fixture-built model — and (c)
deterministic, so rows are reproducible and stable between discovery and execution.

**Usage evidence:** theory data included *delegates over the fixture* (a row was "a function from
the fixture to the input"), and *delegates over a serialised payload* (a row was "a corruption to
apply"). Custom data attributes generated rows exhaustively (every index into a frame, every
invalid character in a range) and from boundary values. Several of those generators used
unseeded randomness, so a failure on one run could not be reproduced on the next.

### Behaviour

- Plain xunit v3 theory data works unchanged with chains: a theory parameter can be used inside any
  step.
- `FixtureData<Fixture, Value>` (xunit package) is typed theory data whose rows are functions of the
  fixture, each with a **label** used as the row's display name (a delegate otherwise displays as its
  type name). Its rows are `FixtureRow<Value>` (core), written `FixtureRow<Value>.For<Fixture>(label,
  x => ...)` when not built through `FixtureData`.
  - **Why the row type does not name the fixture.** xunit requires theory methods and their
    `MemberData` to be public, and the fixture is `internal`; a parameter or member typed
    `FixtureRow<Fixture, Value>` would force it public. So the row is `FixtureRow<Value>`, the
    data member is declared as `TheoryData<FixtureRow<Value>>` and returns a `FixtureData<Fixture,
    Value>` (which checks every row against the fixture where it is written), and a row given to a
    chain on another fixture type fails as an arrangement failure naming both types.
  - The row is evaluated against the per-test fixture during phase 1, after the givens before it.
    `Given(row)` stores the value as chain state; `Given(row, (x, value) => ...)` hands it to the
    fixture, which is the form a one-line theory needs, since steps cannot yet read chain state
    (C2's chain context is not built).
- `TheoryRandom.Create(seed)` gives row generators a `System.Random` from an explicit seed — never
  an unseeded generator — so generated rows are identical on every enumeration. `FluentGwtSeed`
  (C14), when set, overrides that seed too; `TheoryRandom.Create(seed, configuration)` takes the
  configuration explicitly.
- Theory rows carry the row's identity into `TestId` (C14), so each row has its own identifier for
  isolated resources, even when rows share a seed.

### Example

```csharp
// OrderPlacementTests.Theories.cs
public sealed partial class OrderPlacementTests
{
	public static TheoryData<FixtureRow<PlaceOrder>> InvalidOrders => new FixtureData<Fixture, PlaceOrder>
	{
		{ "zero quantity", x => x.Order with { Quantity = 0 } },
		{ "unknown sku", x => x.Order with { Sku = x.Random.String2(13) } },
		{ "negative quantity", x => x.Order with { Quantity = -x.Random.Int(1, 9) } },
	};
}

// OrderPlacementTests.cs
[Theory, MemberData(nameof(InvalidOrders))]
public Task WhenOrderIsInvalidThenItIsRejected(FixtureRow<PlaceOrder> order)
	=> Context
		.GivenDefaults()
		.Given(order, (x, invalid) => x.Order = invalid)
		.WhenPlacingOrder()
		.Then(x => x.Status.Should().Be(PlacementStatus.Invalid));
```

### Acceptance criteria

- `WhenTheoryParameterIsUsedInStepThenEachRowSeesItsValue`
- `WhenFixtureRowIsGivenThenItIsEvaluatedAgainstThisTestsFixture`
- `WhenFixtureRowIsGivenThenEvaluatedValueIsAvailableAsState`
- `WhenFixtureDataHasLabelsThenRowsDisplayByLabel`
- `WhenTheoryDataIsEnumeratedTwiceThenRowsAreIdentical`
- `WhenTheoryRowsDifferThenEachRowHasItsOwnTestId`
- `WhenProviderSeedIsOverriddenByConfigurationThenRowsFollowIt`

### Coverage

- **2023: No.**
- **2020: No.** (Plain xunit data only.)

---

## 16. C14 — Seeded test data and test identity

### Problem

Generated data finds bugs that hand-picked data hides, but a failure must be reproducible. The
survey showed nearly every fixture holding a **static, process-wide** random generator — usually
unseeded, occasionally seeded with a constant. A static generator shared across tests makes each
test's values depend on which tests ran before it and in what order, so even the seeded ones were
not reproducible under parallel execution.

The design keeps the exploration of fresh data on every run and makes each failure replayable:
every fixture owns its seed, the seed is reported when a test fails, and re-running with that seed
reproduces the failure.

Separately, **usage evidence:** almost every hosted suite generated a random per-test identifier to
name an isolated in-memory database, so tests running in parallel never shared state.

### Behaviour

- Each fixture has its own `Random` (a Bogus `Randomizer`) and `Fake` (a Bogus `Faker`, locale
  configurable, default `en_GB`) — Bogus package — both seeded with the fixture's `Seed`.
- **A fresh seed every run.** Unless one is declared, a fixture's seed is drawn from a
  non-deterministic source when the fixture is created, so every run, and every test, uses new data.
- **A declared seed.** A fixture declares a fixed seed by overriding `protected virtual int?
  FixedSeed` (default `null`); every run then uses it. Several fixtures share a seed by promoting it
  to a common class — a shared fixture base that overrides `FixedSeed`, or a static class whose
  constant each fixture returns. There is no assembly-level or global seed declaration.
- **Reported on failure.** When a test fails in any phase, the seed is written to the test output
  (C23) as `FluentGwt seed: 1234567 (fresh; replay with FluentGwtSeed=1234567)`, naming whether it
  was fresh, declared or forced. Nothing is written for a passing test.
- **Replay.** Setting `FluentGwtSeed` in configuration (C11) forces the seed of every fixture in the
  run, overriding declared seeds. A failure is reproduced by re-running that test alone with the
  reported seed. A value that is not an integer fails the test as an arrangement failure naming the
  value.
- **`x.TestId`** is a short identifier (twelve lowercase base-32 characters, safe in a database or
  schema name) **derived from the seed plus the test's identity** — the test class's full name, the
  test method name and, for a theory, the row's display name (the label, for a `FixtureData` row) —
  using a **stable** hash (not `string.GetHashCode`, which .NET randomises per process). It is
  therefore reproducible from the seed, and distinct between tests, and between theory rows, that
  share a seed. Without the xunit package the identity is the fixture type's full name.
- A consequence worth knowing: two concurrent executions of the *same* test under the *same*
  declared or forced seed share a `TestId`. Isolation by `TestId` is per test, not per execution.
- The core package chooses the seed and derives `TestId`; it does not depend on Bogus.

### Example

```csharp
internal sealed class Fixture : ServiceFixture
{
	public PlaceOrder Order => field ??= new(Fake.Commerce.Ean13(), Random.Int(1, 9));
}
```

A seed shared by every fixture in a suite, promoted to a common base:

```csharp
internal abstract class ProtocolFixture : ServiceFixture
{
	protected override int? FixedSeed => 20261008;
}
```

### Acceptance criteria

- `WhenSeedIsNotDeclaredThenEachRunUsesANewSeed`
- `WhenSeedIsDeclaredThenEveryRunUsesIt`
- `WhenSeedIsDeclaredOnACommonBaseThenEveryDerivedFixtureUsesIt`
- `WhenTwoFixturesUseTheSameSeedThenGeneratedValuesAreIdentical`
- `WhenTestsRunInAnyOrderThenEachTestsValuesDependOnlyOnItsSeed`
- `WhenTestFailsThenSeedIsReported`
- `WhenTestPassesThenSeedIsNotWritten`
- `WhenSeedIsForcedByConfigurationThenTestReplays`
- `WhenSeedIsForcedByConfigurationThenItOverridesADeclaredSeed`
- `WhenForcedSeedIsNotAnIntegerThenArrangementFailsNamingTheValue`
- `WhenTestIdIsDerivedFromSameSeedAndTestThenItIsIdentical`
- `WhenTwoTestsShareASeedThenTheirTestIdsDiffer`
- `WhenTheoryRowsShareASeedThenTheirTestIdsDiffer`
- `WhenTestIdIsDerivedInSeparateProcessesThenItIsTheSame`

### Coverage

- **2023: No.** (Its own tests use an unseeded static generator.)
- **2020: No.**

---

## 17. C15 — Service overrides in the fixture container (usage)

### Problem

**Usage evidence:** the dominant idiom across service suites was "register the product with its
own `AddX`, then replace an edge with a mock". Consumers relied on *last registration wins* for
single resolution, which silently fails for anything resolved as `IEnumerable<T>` (the original
stays in the list) and for keyed registrations. The old test-host helper used remove-all-then-add,
which is the intended meaning.

### Behaviour

- `Services.Override<Service>(instance)`, `Override<Service, Implementation>()` and
  `Override<Service>(factory)` remove **every** existing registration of `Service` and add the
  replacement with the original's lifetime (singleton for an instance). Overriding a service that
  was never registered adds it as transient, the container's own default.
- Keyed forms take a service key and touch only that key; an unkeyed override leaves keyed
  registrations alone, and the reverse.
- `Services.Stub<Service>()` registers a `Mock<Service>` (Moq package) and returns it, replacing any
  existing registration.
- These obey the lock in C1.

### Example

```csharp
public static Given<Fixture> GivenWarehouseOffline(this Given<Fixture> given)
	=> given.Given(x => x.Services.Override<Warehouse>(new OfflineWarehouse()));
```

### Acceptance criteria

- `WhenServiceIsOverriddenThenResolveReturnsTheOverride`
- `WhenServiceIsOverriddenThenEnumerableContainsOnlyTheOverride`
- `WhenOverrideHasNoLifetimeThenOriginalLifetimeIsKept`
- `WhenKeyedServiceIsOverriddenThenOtherKeysAreUntouched`
- `WhenStubIsRequestedThenMockIsRegisteredAndReturned`
- `WhenOverrideFollowsResolutionThenInvalidOperationIsThrown`

### Coverage

- **2023: No.**
- **2020: No** (plain `Services` only).

---

## 18. C16 — Outbound HTTP redirection in the fixture container

### Problem

A component under test calls another service over `HttpClient`. The test must control what comes
back, without a network and without replacing the typed client — the pipeline (authentication
handlers, resilience) is part of what is being tested.

**Usage evidence:** domain suites replaced the primary handler of every `HttpClientFactory` client
with a stub returning a canned response, through a post-configure hook on the factory's options.
Client-library suites set a handler hook on the client's options to return a malformed response.

### Behaviour (Http package, no ASP.NET dependency)

- `x.Services.RedirectHttp()` replaces the **primary** handler of every factory-created client;
  `RedirectHttp(name)` and `RedirectHttp<Client>()` limit it to one named or typed client.
  Delegating handlers and resilience handlers stay in place.
- The redirection target is one of: `RespondingWith(request => response)` (sync or async), a stub
  `HttpMessageHandler`, or a test host (C21).
- Every redirected request is recorded; `x.Http.Requests` exposes them for assertions.
- An unmatched request (a responder returning null) fails the test with the request line in the
  message rather than reaching the network. It does so by throwing from the handler, so a product
  that swallows its outbound failures also swallows this one; the recorded requests still show it.
- Redirection is applied as a post-configuration of the factory's options, so it wins over a primary
  handler the product sets in its own composition, whichever is registered first.
- A stub handler given to `Via` is called without being owned: the factory's handler rotation never
  disposes it.

### Example

```csharp
public static Given<Fixture> GivenUpstreamForecast(this Given<Fixture> given, HttpStatusCode status)
	=> given.Given(x => x.Services
		.RedirectHttp<ForecastClient>()
		.RespondingWith(_ => new HttpResponseMessage(status) { Content = JsonContent.Create(x.Forecast) }));

[Fact]
public Task WhenForecastIsFetchedThenUpstreamIsCalledOnce()
	=> Context
		.GivenDefaults()
		.GivenUpstreamForecast(HttpStatusCode.OK)
		.WhenFetchingForecast()
		.ThenFixture(x => x.Http.Requests.Should().ContainSingle());
```

### Acceptance criteria

- `WhenAllClientsAreRedirectedThenEveryClientReceivesTheStubResponse`
- `WhenNamedClientIsRedirectedThenOtherClientsAreUntouched`
- `WhenTypedClientIsRedirectedThenItsDelegatingHandlersStillRun`
- `WhenResilienceIsConfiguredThenRedirectionSitsBeneathIt`
- `WhenRequestIsRedirectedThenItIsRecorded`
- `WhenNoResponseMatchesThenTestFailsNamingTheRequest`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 19. C17 — Test host: self-hosting a real ASP.NET app

### Problem

Endpoint behaviour — routing, model binding, authentication, authorisation, filters, middleware
order — is only tested by running the real pipeline. The survey's single biggest body of code was
host bootstrap copied between suites: about sixty lines per suite to build a host, start it on a
background task, discover its address through a callback the application wrote into the fixture,
and dispose it — with startup exceptions swallowed in a `catch` that only broke into a debugger,
so a host that failed to start surfaced later as a confusing "no address" error. Later suites moved
to `WebApplicationFactory` but still built it by hand in each test class.

Suites used two kinds of application: the **product's own entry point**, and a **test-composed**
application (a test-only composition that adds the product's controllers and services, so a test
can host one area of an API).

### Behaviour (AspNetCore package)

- **The host type is `ApplicationHost`.** It is not called `TestHost` because that name collides
  with the `Microsoft.AspNetCore.TestHost` namespace (CA1724), which host suites commonly import.
- **Hosts are created with the fixture passed in**, not through a fixture method:
  - `ApplicationHost.For<EntryPoint>(fixture)` creates a host for the product's entry point
    (backed by `WebApplicationFactory<EntryPoint>`).
  - `ApplicationHost.Composed(fixture, name, services, pipeline)` creates a test-composed host
    from two delegates (the `XTests.Startup.cs` file): `Action<WebApplicationBuilder> services`
    composes the application's services, and `Action<WebApplication> pipeline` maps its pipeline
    after `Build()`. Two delegates rather than one that builds and returns the application, because
    the test's service overrides (C18) must run after the application's registrations but before
    `Build()`, and the pipeline insertion point (C18) sits between `Build()` and the pipeline
    delegate — neither point exists inside a single delegate that calls `Build()` itself.
  - The typical declaration is a get-only property assigned in the fixture's constructor:
    `public ApplicationHost Api { get; }` and `public Fixture() { Api = ApplicationHost.For<Program>(this); }`.
  - **Why not `x.Host<EntryPoint>()`:** a member another package adds to `ServiceFixture` is an
    extension, and calling an extension from inside the fixture class needs `this.`, which the
    conventions forbid. Passing `this` as an argument is ordinary.
- **The core contract is `FixtureHost`.** Core defines a public `FixtureHost` interface —
  `ValueTask Start(CancellationToken)` plus `IAsyncDisposable` — and `ServiceFixture.Attach(FixtureHost)`.
  An `ApplicationHost` attaches itself to the fixture it is given. Attached hosts are **started in
  phase 2** (after the immediate givens, before the deferred ones) in attach order, and **disposed
  in phase 7** in reverse attach order, after the teardown callbacks (C10) and before the fixture
  container's provider. `Attach` is synchronous and the library never blocks on async work, so a
  host attached after phase 2 (in a deferred given, say) is not started by the fixture; an
  `ApplicationHost` starts itself on first use instead — `CreateClient()`, `Services` or `Address`.
- Several hosts may be created per fixture, each with its own name; each is independent.
- **One host per test.** A host belongs to the fixture it was created with and lives for that one
  test; hosts are never shared across tests or across a class, and the library offers no
  class-shared host.
- A host starts **completely**, as the application would in production: the application's own
  hosted services start with it, whether or not integration is enabled. A test that does not want
  one running removes it in arrangement with
  `host.ConfigureServices(services => services.RemoveHostedService<Implementation>())` or
  `services.RemoveApplicationHostedServices()` in the same callback (C12).
- Hosts started in phase 2 start after every immediate given, so overrides (C18) written anywhere
  in the chain apply. After start, every arrangement member (`ConfigureServices`, `Configure`,
  `Pipeline`, `Environment`, `OnSockets`, `WithProtocols`) throws `InvalidOperationException`
  naming the host, mirroring C1.
- A host that fails to start — including a hosted service of its own failing to start — fails the
  test as an arrangement failure, carrying the startup exception.
- By default a host runs on the in-memory test server.
  `ApplicationHost.For<EntryPoint>(this).OnSockets()` runs an entry-point host on real sockets
  instead, through the factory's Kestrel mode; a test-composed host cannot be put on sockets (C22).
- `host.Name` is the host's name: the entry type's name for an entry-point host, the given name
  for a composed one.
- `host.Address` is the base address. On the test server each host gets `http://{name}/`, with the
  name lower-cased and every character outside `[a-z0-9-]` replaced by `-` — `http://program/` for
  `ApplicationHost.For<Program>(this)`. On sockets it is the bound address (C22).
- `host.CreateClient()` returns an `HttpClient` over the test server's handler with the host's base
  address and **redirects not followed** (every suite in the survey turned them off to assert on
  3xx responses).
- Hosts never need the fixture registered inside the application; anything the application needs
  from the test is supplied through overrides (C18).
- **Hosted services may be stopped twice.** `WebApplicationFactory` stops an entry-point host's
  hosted services twice when it is disposed: its dispose stops the minimal-hosting host and then
  disposes it, which stops it again. A hosted service under test should tolerate a second
  `StopAsync`.

### Example

```csharp
// WeatherEndpointTests.Startup.cs
public sealed partial class WeatherEndpointTests
{
	private static void ComposeServices(WebApplicationBuilder builder)
		=> builder.Services.AddWeather();

	private static void ComposePipeline(WebApplication app)
		=> app.MapWeather();
}

// WeatherEndpointTests.Fixture.cs
internal sealed class Fixture : ServiceFixture
{
	public Fixture()
	{
		Weather = ApplicationHost.Composed(this, "weather", ComposeServices, ComposePipeline);
	}

	public ApplicationHost Weather { get; }
}

// WeatherEndpointTests.Fluent.cs
public static When<Fixture, HttpResponseMessage> WhenGettingForecast(this Given<Fixture> given, string city)
	=> given.When((x, cancellationToken) => x.Weather.CreateClient().GetAsync($"/forecast/{city}", cancellationToken));
```

### Acceptance criteria

- `WhenEntryPointHostIsDeclaredThenRequestsReachTheRealPipeline`
- `WhenComposedHostIsDeclaredThenRequestsReachItsEndpoints`
- `WhenTwoHostsAreDeclaredThenEachHasItsOwnAddress`
- `WhenTwoTestsDeclareTheSameHostThenEachGetsItsOwnInstance`
- `WhenOverrideIsWrittenAfterHostDeclarationThenItStillApplies`
- `WhenOverrideIsAttemptedAfterStartThenInvalidOperationNamesTheHost`
- `WhenHostIsCreatedThenItIsAttachedAndStartedInPhaseTwo`
- `WhenHostIsAttachedAfterPhaseTwoThenItStartsOnFirstUse`
- `WhenChainEndsThenHostsAreDisposedAfterTeardownCallbacksAndBeforeTheProvider`
- `WhenHostIsNamedThenAddressIsItsLowerCaseNameWithInvalidCharactersReplaced`
- `WhenEntryPointHostIsCreatedThenItsNameIsTheEntryTypesName`
- `WhenHostFailsToStartThenTestFailsWithTheStartupException`
- `WhenApplicationHostedServiceFailsToStartThenTestFailsAsArrangement`
- `WhenClientIsCreatedThenRedirectsAreNotFollowed`
- `WhenChainEndsThenHostsAreDisposedInReverseOrder`
- `WhenAssertionFailsThenHostsAreStillDisposed`
- `WhenNoHostIsDeclaredThenNoHostIsStarted`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 20. C18 — Host overrides: services, configuration, pipeline

### Problem

**Usage evidence:** host suites replaced application services with mocks (with remove-all-then-add
semantics), supplied per-test configuration values (database names, upstream addresses, policy
toggles), and, in one middleware suite, let individual tests insert a middleware into the
application pipeline through a hook on the fixture. Because the old host started inside a given,
suites that needed per-test overrides resorted to collecting override delegates and passing them
into a composite "default configuration" step — a workaround that disappears with phase-2 start.

### Behaviour

- `host.ConfigureServices(services => ...)` registers a callback that runs against the
  **application's real `IServiceCollection`**, after its own composition and before the provider is
  built (`ConfigureTestServices` for an entry-point host; after the `services` delegate and before
  `Build()` for a composed one). Callbacks run in the order they were registered. Because the
  collection is the real one, `Override` (C15), `Stub` (Moq package), `RedirectHttp` (C16, C21),
  `RemoveHostedService` and `RemoveApplicationHostedServices` (C12) all work inside it unchanged.
  - **Why a callback and not a recorded collection replayed later:** a replayed recording starts
    empty, so it cannot remove every registration of a type, keep the original registration's
    lifetime, or check that a hosted-service removal matched something — each of those needs the
    application's registrations in front of it.
- `host.Services` is the **running** application's `IServiceProvider` (C19), available once the
  host has started; using it on a host that has not started starts it (C17).
- `host.Configure(key, value)` sets configuration with the highest precedence: through
  `UseSetting` for an entry-point host, and as an in-memory configuration source added last for a
  composed one.
- `host.Pipeline(app => ...)` (an `Action<IApplicationBuilder>`) inserts middleware: at the front
  of the pipeline for an entry-point host, through a startup filter; between `Build()` and the
  `pipeline` delegate for a composed one.
- `host.Environment(name)` sets the hosting environment name (default `Development`).
- All of these obey the post-start lock: after start each throws `InvalidOperationException` naming
  the host.

### Example

```csharp
public static Given<Fixture> GivenUpstreamDown(this Given<Fixture> given)
	=> given.Given(x => x.Weather.ConfigureServices(services => services.Override<ForecastSource>(new FailingForecastSource())));

public static Given<Fixture> GivenForwardedHeaders(this Given<Fixture> given)
	=> given.Given(x => x.Weather.Pipeline(app => app.UseForwardedHeaders()));

public static Given<Fixture> GivenUpstream(this Given<Fixture> given, Uri upstream)
	=> given
		.Given(x => x.Weather.Configure("Forecast:Upstream", upstream.ToString()))
		.Given(x => x.Weather.Environment("Staging"));
```

### Acceptance criteria

- `WhenHostServiceIsOverriddenThenApplicationResolvesTheOverride`
- `WhenHostServiceIsOverriddenThenApplicationEnumerableContainsOnlyTheOverride`
- `WhenHostServiceIsOverriddenThenOriginalLifetimeIsKept`
- `WhenServicesCallbacksAreRegisteredThenTheyRunInOrderAfterTheApplicationsComposition`
- `WhenComposedHostPipelineIsAddedThenItRunsBeforeThePipelineDelegate`
- `WhenHostConfigurationIsSetThenApplicationReadsIt`
- `WhenPipelineMiddlewareIsAddedThenItRunsForEveryRequest`
- `WhenEnvironmentIsSetThenApplicationSeesIt`
- `WhenTwoTestsOverrideTheSameServiceThenNeitherSeesTheOthers`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 21. C19 — Reaching into a running host (usage)

### Problem

**Usage evidence:** the commonest deferred givens in host suites seeded data *through the running
application's own container* (create a scope, resolve the data context, add rows, save), and the
commonest custom assertions read persisted state back the same way. A variant registered the same
in-memory database name in both the fixture container and the application so either side could see
the other's writes.

### Behaviour

- `host.Scope(async (services, cancellationToken) => ...)` runs work in a fresh scope of the
  running application's container, disposing the scope afterwards.
- `host.Resolve<Service>()` resolves from the application's root provider (for singletons).
- `host.Services` is the running application's root `IServiceProvider` itself (C18), for work that
  needs the provider rather than one service.
- Like every other `ApplicationHost` member that needs the running application, they start the host
  on first use. Used in an immediate given, that start happens before later arrangement, which the
  host then refuses — so seeding belongs in a deferred given, as in the example.
- `Scope` passes its work the fixture's `Cancellation` (C10), the token linked to the test's.
  It has sync, `Task` and result-returning forms.

### Example

```csharp
public static Given<Fixture> GivenForecastOnFile(this Given<Fixture> given)
	=> given
		.Given(x => x.Weather.Scope(async (services, cancellationToken) =>
		{
			var store = services.GetRequiredService<ForecastStore>();
			await store.Save(x.Forecast, cancellationToken);
		}))
		.Deferred();

public static Then<Fixture, HttpResponseMessage> ThenForecastIsStored(this When<Fixture, HttpResponseMessage> when)
	=> when.ThenFixture(x => x.Weather.Scope(async (services, cancellationToken) =>
		(await services.GetRequiredService<ForecastStore>().All(cancellationToken)).Should().ContainSingle()));
```

### Acceptance criteria

- `WhenScopeIsUsedAfterStartThenItResolvesApplicationServices`
- `WhenScopeIsUsedBeforeStartThenTheHostStartsOnFirstUse`
- `WhenScopeIsGivenWorkThenItReceivesTheFixtureCancellation`
- `WhenScopeCompletesThenScopedServicesAreDisposed`
- `WhenDeferredGivenSeedsThroughHostThenActSeesTheData`
- `WhenAssertionReadsThroughHostThenItSeesActWrites`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 22. C20 — Test authentication and authorisation (usage)

### Problem

**Usage evidence:** this was the most repeated host idiom of all. Across the API suites there were
over fifty uses of a "user has claim" given (with a literal value or a value from the fixture),
over twenty of "user is not authenticated", a given overriding an endpoint authorisation policy's
requirements, and the triad of facts it enables on nearly every endpoint: unauthenticated is 401,
authenticated without the right claim is 403, authorised succeeds. The old test-host package shipped
a test authentication scheme whose options held "is authenticated" and a claim list. Some older
suites instead ran a real in-process identity provider on a second host and obtained real tokens.

Real token issuance is **out of scope**. What is in scope is the reason those suites did it: an API
protected by a real provider — Entra ID single sign-on, say — must be testable as written, without
the provider. So the library replaces the provider's handlers with stubs under the same scheme
names, and exposes what the stubs issued and what the application's authorisation did with it,
so a test can show that the claims it gave reach the API's policies and are acted on.

### Behaviour

- **Real providers are never called.** No token is requested, issued or validated, and no metadata
  or signing-key document is fetched.
- `host.Authentication()` (idempotent, returning `TestAuthentication`) replaces the **handler** of
  every authentication scheme the application registers — the bearer scheme Entra ID's web API
  registration adds, an OpenID Connect scheme, a cookie scheme — with the library's stub handler,
  **keeping each scheme's name**. `[Authorize(AuthenticationSchemes = ...)]`, policies that name
  schemes, and the application's default-scheme selection therefore behave as written. The swap is
  made on the authentication options after the application's composition: the provider's own
  registration code runs unchanged; only its handler never runs.
- Where the application registers no scheme (a test-composed host), a stub scheme named
  `FluentGwt` is registered and made the default authenticate and challenge scheme.
- A stub challenge answers 401 and a stub forbid answers 403; nothing redirects to a provider's
  sign-in page.
- With no further givens, requests are **anonymous** (every stub returns no result) so protected
  endpoints answer 401.
- `GivenAuthenticatedUser()` authenticates every request, under every stubbed scheme, as a principal
  with no claims beyond a name identifier generated from the seed (C14).
  `GivenAuthenticatedUser(scheme)` authenticates under that scheme only, leaving the others
  anonymous, so a test can show that an endpoint demands a particular scheme.
- `GivenClaim(type, value)` and `GivenClaim(type, x => value)` add claims; adding a claim implies
  authenticated. `GivenRole(role)` adds a claim of the scheme's role claim type.
- The stub builds each identity with the scheme name as its authentication type, and with the name
  and role claim types of the scheme it replaced where that scheme's options declare them (the
  token validation parameters of a bearer or OpenID Connect scheme); otherwise the `ClaimsIdentity`
  defaults. `[Authorize(Roles = ...)]`, `IsInRole` and `User.Identity.Name` behave as they would
  with the real provider.
- Claims are given **as the application sees them after validation**: the replaced handler's
  inbound claim mapping does not run, so a test gives claims in the form the application's code and
  policies read them.
- `GivenAnonymousUser()` returns to anonymous, for readability in tests that assert 401.
- `GivenPolicy(name, policy => ...)` replaces the requirements of a named authorisation policy.
- **What a test can inspect:**
  - `host.Authentication().Principal` — the principal the stubs will issue, from phase 2 onwards.
  - `host.Authentication().Issued` — one record per authentication a stub performed: the scheme,
    the principal and its claims, and the request.
  - `host.Authorisation.Decisions` — one record per policy evaluation in the authorisation
    middleware: the endpoint, the policy (its name where it has one, and its requirements), whether
    it succeeded, and the requirements that failed. It is recorded by decorating the application's
    authorisation result handler, so the application's own handler still runs.
- Downstream token acquisition (on-behalf-of, client credentials) is an outbound HTTP call to the
  provider, not authentication of the incoming request; it is redirected like any other (C21).
- The principal and the records are per fixture; parallel tests never see each other's.

### Example

```csharp
// InvoiceEndpointTests.Fixture.cs — the product's Program registers Entra ID for its web API
internal sealed class Fixture : ServiceFixture
{
	public Fixture()
	{
		Api = ApplicationHost.For<Program>(this);
	}

	public ApplicationHost Api { get; }
	public string TenantId => field ??= Random.Guid().ToString();
}

// InvoiceEndpointTests.Fluent.cs
public static Given<Fixture> GivenDefaults(this Fixture fixture)
	=> fixture.Given(x => x.Api.Authentication());

// InvoiceEndpointTests.cs
[Fact]
public Task WhenUserLacksTenantClaimThenRequestIsForbidden()
	=> Context
		.GivenDefaults()
		.GivenRole("Invoices.Read")
		.WhenListingInvoices()
		.Then(x => x.StatusCode.Should().Be(HttpStatusCode.Forbidden))
		.AndFixture(x => x.Api.Authorisation.Decisions.Should().ContainSingle(d => d.Policy == "InvoiceReader" && !d.Succeeded));

[Fact]
public Task WhenUserHasRoleAndTenantClaimThenInvoicesAreListed()
	=> Context
		.GivenDefaults()
		.GivenRole("Invoices.Read")
		.GivenClaim("tid", x => x.TenantId)
		.WhenListingInvoices()
		.Then(x => x.StatusCode.Should().Be(HttpStatusCode.OK))
		.AndFixture(x => x.Api.Authentication().Issued.Should().ContainSingle(i => i.Scheme == JwtBearerDefaults.AuthenticationScheme));
```

### Acceptance criteria

- `WhenNoUserIsGivenThenProtectedEndpointIsUnauthorised`
- `WhenAuthenticatedUserIsGivenThenRequestIsAuthenticated`
- `WhenClaimIsGivenThenPrincipalCarriesIt`
- `WhenClaimIsGivenThenItReachesTheEndpointsUser`
- `WhenClaimValueComesFromFixtureThenItIsEvaluatedAtExecution`
- `WhenRequiredClaimIsMissingThenRequestIsForbidden`
- `WhenPolicyIsOverriddenThenNewRequirementsApply`
- `WhenAnonymousUserIsGivenAfterClaimsThenRequestIsUnauthorised`
- `WhenTestsRunInParallelThenPrincipalsAreIsolated`
- `WhenApplicationRegistersEntraIdThenItsSchemeIsStubbedUnderTheSameName`
- `WhenSchemesAreStubbedThenNoRequestReachesTheIdentityProvider`
- `WhenApplicationRegistersNoSchemeThenStubSchemeIsTheDefault`
- `WhenStubbedSchemeChallengesThenResponseIsUnauthorisedNotARedirect`
- `WhenUserIsGivenForOneSchemeThenOtherSchemesStayAnonymous`
- `WhenSchemeDeclaresRoleClaimTypeThenGivenRoleSatisfiesRoleAuthorisation`
- `WhenRequestIsAuthenticatedThenIssuedRecordsSchemeAndPrincipal`
- `WhenPolicyIsEvaluatedThenDecisionRecordsOutcomeAndFailedRequirements`
- `WhenDecisionsAreRecordedThenApplicationResultHandlerStillRuns`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 23. C21 — Host-to-host and host-outbound HTTP redirection

### Problem

The application under test calls other services. **Usage evidence:** a webhook suite ran two hosts
and redirected every outbound client of the first to the second's address; client-library suites
pointed a typed client in the fixture container at a hosted API; and almost every such suite
accepted any server certificate to make it work.

### Behaviour

- `RedirectHttp(...)` (C16 semantics) works inside a host's `ConfigureServices` callback (C18),
  against the application's own collection.
- `.To(otherHost)` targets another host created with the same fixture: with in-memory hosts the request goes through
  that host's in-memory handler; with socket hosts it goes to its address, trusting **only that
  host's certificate**.
- `x.Services.RedirectHttp<Client>().To(host)` points a fixture-container client at a host — the
  client-library test case.
- Host start order respects redirection: a host that is a redirection target starts first.

### Example

```csharp
public static Given<Fixture> GivenSubscriberRunning(this Given<Fixture> given)
	=> given.Given(x => x.Publisher.ConfigureServices(services => services.RedirectHttp("webhooks").To(x.Subscriber)));
```

### Acceptance criteria

- `WhenHostClientIsRedirectedToHostThenRequestReachesTheOtherHost`
- `WhenFixtureClientIsRedirectedToHostThenRequestReachesTheHost`
- `WhenTargetHostIsDeclaredLaterThenItStillStartsFirst`
- `WhenSocketHostIsTargetedThenOnlyItsCertificateIsTrusted`
- `WhenRedirectionIsCircularThenArrangementFailsNamingTheHosts`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 24. C22 — Real-socket hosting (usage)

### Problem

**Usage evidence:** several suites needed a real listening socket rather than the in-memory test
server — WebSocket endpoints exercised with a real client, HTTP/2-only gRPC endpoints, and
out-of-process clients. Each bound loopback port 0 with HTTPS (some with a certificate file checked
into the test project) and discovered the bound address by hand.

### Behaviour

- `.OnSockets()` runs the host through `WebApplicationFactory`'s Kestrel mode (`UseKestrel`, .NET
  10), never a Kestrel the library binds by hand. Through the factory's Kestrel options it binds
  loopback on an ephemeral port with HTTPS using a certificate **generated in memory for the test
  run** (never a file in the repository).
- **Entry-point hosts only.** Socket hosting is available only for a host built from a real entry
  point (`ApplicationHost.For<EntryPoint>(fixture)`). A test-composed host
  (`ApplicationHost.Composed(fixture, name, services, pipeline)`, C17) is a `WebApplication` built
  from its two delegates, with no entry point for `WebApplicationFactory` to load,
  so it cannot use the factory's Kestrel mode — and the library never binds Kestrel by hand.
  `.OnSockets()` on a test-composed host fails the test as an arrangement failure in phase 2,
  before any host starts, with a message naming the host and saying that a test-composed host has
  no entry point for `WebApplicationFactory`'s Kestrel mode, so real sockets need a host declared
  with `ApplicationHost.For<EntryPoint>(fixture)`.
- `.WithProtocols(...)` selects HTTP/1.1, HTTP/2 or both.
- `host.Address` reports the actual bound address.
- `host.CreateClient()` trusts exactly that certificate.
- `host.CreateWebSocket()` returns a connected-ready client configured likewise.

### Example

```csharp
internal sealed class Fixture : ServiceFixture
{
	public Fixture()
	{
		Relay = ApplicationHost.For<Program>(this).OnSockets().WithProtocols(HttpProtocols.Http1AndHttp2);
	}

	public ApplicationHost Relay { get; }
}
```

### Acceptance criteria

- `WhenHostRunsOnSocketsThenAddressIsLoopbackWithBoundPort`
- `WhenHostRunsOnSocketsThenClientTrustsOnlyItsCertificate`
- `WhenHttpTwoIsSelectedThenGrpcCallSucceeds`
- `WhenWebSocketIsOpenedThenItConnectsToTheHost`
- `WhenTwoSocketHostsRunInParallelThenPortsDiffer`
- `WhenHostRunsOnSocketsThenItIsServedByTheFactorysKestrelMode`
- `WhenComposedHostIsPutOnSocketsThenArrangementFailsExplainingWhy`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 25. C23 — Host logs in test output, and log assertions (usage)

### Problem

**Usage evidence:** every hand-rolled host configured its own logger writing to the debug and
console sinks at "warning, or verbose when a debugger is attached", tagged with the host's name, so
a failing test's server-side error could be found. Console output is not attributed to a test under
parallel execution.

### Behaviour

- Logs from the fixture container and from every host go to the current test's output (xunit's
  `TestContext.Current.TestOutputHelper`), prefixed with the host name.
- Minimum level for the output: `Warning`, or `Debug` when a debugger is attached; the
  configuration key `FluentGwt:LogLevel` (C11) overrides it with any `LogLevel` name.
- `x.Logs` is a `FakeLogCollector` capturing **every** level for assertions, whatever the output
  level.
- The fixture captures the test's output writer when it is created, so work the test started in
  the background still writes to that test, and parallel tests never share output. A line written
  after the test has finished has no test to belong to and is dropped.
- The library's own diagnostics — the seed of a failed test, container validation failures,
  teardown failures — go to the same output.

### Example

```csharp
[Fact]
public Task WhenUpstreamFailsThenWarningIsLogged()
	=> Context
		.GivenDefaults()
		.GivenUpstreamForecast(HttpStatusCode.BadGateway)
		.WhenFetchingForecast()
		.ThenFixture(x => x.Logs.Records.Should().Contain(r => r.Level == LogLevel.Warning));
```

### Acceptance criteria

- `WhenHostLogsWarningThenItAppearsInTestOutput`
- `WhenLogComesFromHostThenItIsPrefixedWithHostName`
- `WhenDebuggerIsDetachedThenInformationIsNotWritten`
- `WhenLogIsWrittenThenItIsAvailableForAssertion`
- `WhenTestsRunInParallelThenLogsAreAttributedToTheirTest`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 26. C24 — Time control

### Problem

Fixtures in the survey stamped data with the wall clock at construction, and the old host package
depended on an authentication clock abstraction that is now obsolete. Wall-clock time makes tests
non-reproducible, and retry and timeout behaviour cannot be tested in reasonable time without
controlling it.

### Behaviour

- `x.Time` is a `FakeTimeProvider` starting at an instant derived from the seed (C14): midnight UTC
  on 1 January 2000 plus `Seed` seconds, which puts any 31-bit seed between 2000 and 2068.
- It is registered as `TimeProvider` in the fixture container with `TryAdd`, so products that
  inject `TimeProvider` (including resilience pipelines) use it and a test that registers its own
  wins.
- In every host it **overrides** the application's `TimeProvider` registration rather than using
  `TryAdd`: real applications register `TimeProvider.System` themselves, and the test must still
  control time.
- `x.Time.Advance(...)` moves time forward within any step.

### Example

```csharp
[Fact]
public Task WhenReservationExpiresThenStockIsReleased()
	=> Context
		.GivenDefaults()
		.GivenReservation()
		.When(x => x.Time.Advance(TimeSpan.FromMinutes(16)))
		.ThenFixture(x => x.Warehouse.Verify(m => m.Release(x.Order.Sku, It.IsAny<CancellationToken>()), Times.Once));
```

### Acceptance criteria

- `WhenFixtureIsCreatedThenTimeStartsAtSeededInstant`
- `WhenProductInjectsTimeProviderThenItReceivesTheFakeProvider`
- `WhenTestRegistersItsOwnTimeProviderThenItWins`
- `WhenTimeIsAdvancedThenTimersFire`
- `WhenHostIsStartedThenItSharesTheFixtureTime`
- `WhenApplicationRegistersSystemTimeProviderThenHostStillUsesTheFixtureTime`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 27. C25 — Analysers

### Problem

Three of the faults the survey found are visible in source before a test ever runs, and each one
fails far from its cause at run time — or not at all:

1. A fixture exposed through a property that constructs a new instance on every access, so state
   written by one step is invisible to the next and disposal never runs (§30, fault 13).
2. A chain that is built but neither returned nor awaited. Because a chain is a description (§2),
   nothing runs, and the test passes having tested nothing.
3. A given that resolves from the container, written before a given that still registers, and not
   deferred — the lock failure of C1, reported at the later given rather than the one at fault (C3).

### Behaviour (Analysers package)

| Id | Severity | Reports | Code fix |
|---|---|---|---|
| `FG0001` | Warning | A property on a test class whose type derives from `ServiceFixture` and that is not a getter-only auto-property initialised with `new()` — an expression-bodied `=> new()` included. | Rewrite as `{ get; } = new();` |
| `FG0002` | Warning | An expression whose type is one of the library's chain types (`Given<…>`, `When<…>`, `Then<…>`, `ThenThrows<…>`) whose value is discarded: an expression statement, or a chain assigned to a local that is never returned or awaited. | None |
| `FG0003` | Warning | Within one chain expression, an immediate given whose lambda resolves from the fixture container (`Resolve`, `IsResolvable`), followed by a given whose lambda touches `Services`; or an immediate given whose lambda uses a running host (`ApplicationHost.Scope`, `ApplicationHost.Resolve`). | Append `.Deferred()` |

- Analysis is per method body. A step method called from the chain is not followed into; the
  analysers report what is visible in the chain as written.
- Every diagnostic message names the member at fault and, for `FG0003`, the later given that would
  fail.
- The package carries no runtime code. The core package depends on it, so every consumer gets the
  diagnostics without a separate reference.

### Example

```csharp
// FG0001: a new fixture on every access
private Fixture Context => new();

// FG0002: built, never executed
[Fact]
public void WhenStockIsAvailableThenOrderIsAccepted()
	=> Context.GivenDefaults().WhenPlacingOrder().Then(x => x.Status.Should().Be(PlacementStatus.Accepted));

// FG0003: resolves before a later registration, not deferred
public static Given<Fixture> GivenEmptyStore(this Fixture fixture)
	=> fixture
		.Given(x => x.Resolve<OrderStore>().Clear())
		.Given(x => x.Services.Override(x.Warehouse.Object));
```

### Acceptance criteria

- `WhenFixturePropertyIsExpressionBodiedThenFG0001IsReported`
- `WhenFixturePropertyHasASetterThenFG0001IsReported`
- `WhenFixturePropertyIsGetterOnlyWithNewInitialiserThenNothingIsReported`
- `WhenFG0001CodeFixIsAppliedThenPropertyBecomesGetterOnlyWithInitialiser`
- `WhenChainIsAnExpressionStatementThenFG0002IsReported`
- `WhenChainIsAssignedAndNeverAwaitedThenFG0002IsReported`
- `WhenChainIsReturnedOrAwaitedThenNothingIsReported`
- `WhenResolvingGivenPrecedesRegistrationWithoutDeferralThenFG0003IsReported`
- `WhenResolvingGivenIsDeferredThenNothingIsReported`
- `WhenImmediateGivenUsesRunningHostThenFG0003IsReported`
- `WhenFG0003CodeFixIsAppliedThenDeferredIsAppended`
- `WhenStepMethodIsCalledFromChainThenItIsNotAnalysedThrough`

### Coverage

- **2023: No.**
- **2020: No.**

---

## 28. Coverage matrix

"Usage" marks capabilities found in consumers rather than in the old library's API. C9 is
withdrawn and has no row.

| # | Capability | Usage? | 2023 | 2020 | Package |
|---|---|---|---|---|---|
| C1 | Fixture and container lifecycle, lock after resolve, validation, resolvable-first, disposal | | No | Yes (defects) | `FluentGwt` |
| C2 | State givens vs transition givens | | **Yes** (core) | Partly | `FluentGwt` |
| C3 | Deferred givens and phases | | No | Yes | `FluentGwt` |
| C4 | When steps, results, exactly once | | No | Yes | `FluentGwt` |
| C5 | Then assertions | | No | Yes | `FluentGwt` |
| C6 | Exception expectations | | No | Yes (defect) | `FluentGwt` |
| C7 | And chaining | | No | Yes | `FluentGwt` |
| C8 | Async and cancellation | | Partly | Partly | `FluentGwt` (+ `.Xunit` for the token) |
| C10 | Teardown, fixture cancellation | usage | No | No | `FluentGwt` |
| C11 | Test configuration | usage | No | Partly | `FluentGwt` |
| C12 | Integration gating (build-time), hosted services | | No | Yes (defects) | `FluentGwt` (marker, lifecycle, removal) + `.Xunit` (build targets, gate) + `.AspNetCore` (removal inside a host's `ConfigureServices`) |
| C13 | Theory and fixture-relative data | usage | No | No | `FluentGwt.Xunit` |
| C14 | Seeded data, test identity | | No | No | `FluentGwt` (seed) + `.Bogus` |
| C15 | Service overrides | usage | No | No | `FluentGwt` (+ `.Moq` for stubs) |
| C16 | Outbound HTTP redirection | usage | No | No | `FluentGwt.Http` |
| C17 | Test host | usage | No | No | `FluentGwt.AspNetCore` |
| C18 | Host overrides | usage | No | No | `FluentGwt.AspNetCore` |
| C19 | Reaching into a host | usage | No | No | `FluentGwt.AspNetCore` |
| C20 | Test authentication and authorisation | usage | No | No | `FluentGwt.AspNetCore` |
| C21 | Host-to-host redirection | usage | No | No | `FluentGwt.AspNetCore` |
| C22 | Real-socket hosting (entry-point hosts) | usage | No | No | `FluentGwt.AspNetCore` |
| C23 | Logs in test output, log assertions | usage | No | No | `FluentGwt.Xunit` (sink) + core (capture) |
| C24 | Time control | | No | No | `FluentGwt` |
| C25 | Analysers | | No | No | `FluentGwt.Analysers` |

---

## 29. Public surface

All in the root namespace `FluentGwt` (extension classes included, so a test file needs one
`using`). Signatures are indicative; overload families (sync / `Task` / `ValueTask` / with
`CancellationToken`) are written once as `«step»`.

**Chain (core, extending 2023)**

| Member | Purpose |
|---|---|
| `Given<Target>`, `Given` | Existing 2023 records: targeted and untargeted arrangement chains. |
| `target.Given()`, `Given.With(...)` | Existing entry points; both kept. |
| `.Given(value)`, `.Given(name, value)`, `.Given(key, value)` | State givens (existing). |
| `.Given(«step»)`, `.Given((x, state) => ...)` | Transition givens (existing, widened). |
| `.Given(FixtureRow<Target, Value>)` | Evaluate a fixture-relative theory row into state. |
| `.Get<Value>()`, `.Get<Value>(name)`, `.Get<Value>(key)` | Read state (existing). |
| `.Deferred()` | Defer the preceding given to phase 3. |
| `.And(«step»)` on `Given<Target>` | Another transition. |
| `When<Target>`, `When<Target, Result>` | Act. |
| `.When(«step»)`, `.When<Result>(«step»)`, `.WhenResolving<Service>()` | Declare the act. |
| `.And(«step»)`, `.AndResult(«step»)` on `When` | Further act steps. |
| `Then<Target>`, `Then<Target, Result>`, `ThenThrows<Target, Failure>` | The library's own assertion chain types; `.And` may follow; awaitable and implicitly convertible to `Task`. |
| `.Then(«assertion»)`, `.ThenFixture(«assertion»)` | C5. |
| `.ThenThrows<Failure>(«assertion»?)`, `.ThenThrowsExactly<Failure>(«assertion»?)`, `.ThenArrangementFails<Failure>(«assertion»?)` | C6. |
| `.And(«assertion»)`, `.AndFixture(«assertion»)` on `Then` | Further assertions. |

**Fixture (core)**

| Member | Purpose |
|---|---|
| `abstract class ServiceFixture : IAsyncDisposable` | Fixture base. Consumers derive a nested `internal sealed class Fixture`, exposed as `private Fixture Context { get; } = new();`. |
| `Services` | Mutable until first resolution (C1). |
| `Resolve<Service>()`, `Resolve<Service>(key)`, `IsResolvable<Service>()` | C1. |
| `ValidateOnBuild`, `ValidateScopes` | Container validation, both `true` by default (C1). |
| `Configuration`, `Configure(key, value)` | C11. |
| `Cancellation`, `OnTeardown(...)` | C10. |
| `Seed`, `protected virtual int? FixedSeed`, `TestId` | C14. |
| `Time` | C24. |
| `Logs` | C23. |
| `IServiceCollection.Override<Service>(...)` (+ keyed) | C15. |
| `[assembly: IntegrationEnabled]` (`IntegrationEnabledAttribute`) | The integration marker the xunit package's build targets emit (C12). |
| `IntegrationEnabled` | Whether the assembly declaring the fixture's runtime type carries the marker (C12). |
| `IServiceCollection.RemoveHostedService<Implementation>()`, `RemoveHostedService(Type)`, `RemoveApplicationHostedServices()` | Keep hosted services out of phase 4 or out of a host (C12). |
| `interface FixtureHost : IAsyncDisposable` (`ValueTask Start(CancellationToken)`), `Attach(FixtureHost)` | A host the fixture starts in phase 2 and disposes in phase 7, in reverse, after teardown callbacks and before the provider; one attached after phase 2 is not started by the fixture (C17). |
| `abstract class TestRunnerAttribute` | Assembly-level attribute supplying the test token; core reads it from the entry assembly (the test executable under Microsoft.Testing.Platform). The xunit package derives `XunitTestRunnerAttribute`, and its build targets stamp it on every consuming project. |

**Xunit package** — `[IntegrationFact(justification, reason)]`, `[IntegrationTheory(justification,
reason)]`, `[Flags] enum IntegrationJustification` (`NetworkIo`, `DiskIo`, `UnsafeCode`,
`MultipleThreads`, `ThreadSynchronisation`), the `buildTransitive` targets reading the MSBuild
property `FluentGwtIntegration` and defining the `INTEGRATION` symbol, `FixtureData<Fixture, Value>`,
`FixtureData` and its `FixtureRow<Value>` rows, the test-output log sink, test identity for `TestId`, and the
`XunitTestRunnerAttribute` supplying the test token, and `IntegrationGate.IsOpen`, the static property the integration attributes skip unless.

**Bogus package** — `ServiceFixture.Random`, `ServiceFixture.Fake` (seeded with `Seed`).

**Moq package** — `IServiceCollection.Stub<Service>()`.

**Http package** — `IServiceCollection.RedirectHttp()`, `RedirectHttp(name)`,
`RedirectHttp<Client>()` returning a redirection builder with `RespondingWith(...)`, `Via(handler)`;
`ServiceFixture.Http.Requests`.

**AspNetCore package** — `ApplicationHost : FixtureHost` (not `TestHost`, which collides with the
`Microsoft.AspNetCore.TestHost` namespace, CA1724), created by `ApplicationHost.For<EntryPoint>(fixture)`
and `ApplicationHost.Composed(fixture, name, services, pipeline)`; on `ApplicationHost`:
`ConfigureServices(Action<IServiceCollection>)`, `Services` (the running application's
`IServiceProvider`), `Configure(key, value)`, `Pipeline(Action<IApplicationBuilder>)`,
`Environment(name)`, `Name`, `OnSockets()` (entry-point hosts only), `WithProtocols(...)`, `Address`,
`CreateClient()`, `CreateWebSocket()`, `Scope(...)`, `Resolve<Service>()`, `Authentication()`
returning `TestAuthentication` (`Principal`, `Issued`), `Authorisation` (`Decisions`); redirection
builder `.To(host)`; givens `GivenAuthenticatedUser()`, `GivenAuthenticatedUser(scheme)`,
`GivenAnonymousUser()`, `GivenClaim(...)`, `GivenRole(...)`, `GivenPolicy(...)` (each taking the
host where a fixture has more than one, and defaulting to the only host otherwise).

**Analysers package** — diagnostics `FG0001`, `FG0002`, `FG0003` and their code fixes (C25).

---

## 30. What the old design got wrong, and what modern .NET makes unnecessary

### Behavioural and ergonomic faults of the old design

1. **Disposal depended on which assertion you wrote.** Fixture assertions disposed the fixture;
   result assertions did not. Hosts leaked unless the test class also disposed the fixture by hand,
   and suites did both. → One teardown phase, always (§2, C10).
2. **An exception expectation covered the givens.** A broken given could make an exception test
   pass. → Expectations cover the act only (C6).
3. **Deferral was manual and fragile.** Every resolving or host-starting given had to be marked,
   and an unmarked one failed with a lock error far from its cause. → Automatic host phase; explicit
   deferral only for seeding (C3); an analyser for the rest (C25).
4. **Givens were not uniformly async**, forcing a parallel family of steps that extended a `Task`
   of the chain. → Lazy chain, async everywhere (C8).
5. **Host bootstrap was copied into every suite**, swallowed startup exceptions behind a
   debugger-only `catch`, discovered addresses by injecting the fixture into the application, and
   accepted any TLS certificate. → C17, C21, C22.
6. **Per-test host overrides required collecting delegates** and passing them into a composite
   start step. → Phase-2 start (C18).
7. **Hosted-service failures were logged and ignored**, and started with no cancellation. → C12.
8. **The resolvable check built a second provider**, constructing singletons twice with their side
   effects. → Resolve from the real provider (C1).
9. **One global lock** serialised resolution across every fixture in the process. → Per fixture.
10. **The integration gate was decided in an attribute constructor**, cached for the process,
    switched on by whatever settings file or environment variable was present, and barely used
    while an ungated suite required a real database. → A build-time switch, read at run time per
    test from an assembly marker, through dynamic skip on an attribute that must state its
    justification and reason (C12).11. **Shared static random generators** made "seeded" data order-dependent. → A seed per fixture,
    fresh unless declared, reported on failure and replayable (C14).
12. **A large generic-arity overload family** (acts with up to four deferred arguments, each in two
    shapes) existed for one consumer. → Dropped (C4).
13. **The fixture's identity was not protected.** One suite exposed its fixture through a property
    that constructed a new instance on every access, so state written by one step was invisible to
    the next and disposal never ran. → The convention `private Fixture Context { get; } = new();`,
    the library obtaining the fixture once per chain, and analyser `FG0001` (C25).
14. **The core's exception check depended on an old assertion library.** → The core uses
    AwesomeAssertions (Apache-2.0) for the failures it raises itself; consumer assertions are
    unconstrained and their exceptions propagate unchanged (C5, C6).

### What modern .NET and xunit v3 make unnecessary or better

| Topic | Assessment |
|---|---|
| **xunit v3 `TestContext.Current.CancellationToken`** | Replaces `CancellationToken.None` everywhere. The library supplies it to every step automatically (C8). The xUnit1051 analyser will flag every awaited call in consumer code that omits it; step lambdas that receive the token make compliance natural. |
| **`IAsyncLifetime` returning `ValueTask`** | Available, but **not needed** by this design: the chain owns setup and teardown, and the fixture is `IAsyncDisposable`. xunit v3 also disposes a test class that implements `IAsyncDisposable`; the library does not require it, and a test class need implement nothing. `IAsyncLifetime` is the tool for a *class-shared* resource, which this library deliberately does not provide: one host per test (C17). |
| **`WebApplicationFactory` vs a hand-rolled test host** | `WebApplicationFactory` gives in-memory hosting, `ConfigureTestServices`, `CreateClient` with redirect control, access to `Services`, and correct startup-failure propagation — replacing the hand-rolled host for entry-point hosts. For **test-composed** applications (which have no entry point), building a `WebApplication` with the test server (`UseTestServer`) is simpler than forcing a factory. For **real sockets**, the factory's .NET 10 Kestrel mode (`UseKestrel`) is used; the library never binds Kestrel by hand. That mode needs an entry point to load, so socket hosting is for entry-point hosts only, and a test-composed host cannot be put on sockets (C22). |
| **Dynamic skip (`Assert.Skip`, `Assert.SkipUnless`, `Assert.SkipWhen`; `SkipUnless`/`SkipType` on fact attributes)** | Replaces a fact attribute that sets `Skip` in its constructor. The test is evaluated at run time and reported as skipped with a reason, per test, with no process-wide cache. The integration attributes are built on the native properties, with the caller-file/line constructor xUnit3003 requires, and decide from the build-time integration marker (C12). xunit v3's **explicit tests** (`Explicit = true`) are an alternative gate worth knowing about, but they hide tests by default rather than skipping them visibly. |
| **NuGet `buildTransitive` MSBuild targets and the SDK's `AssemblyAttribute` item** | Replace a run-time configuration switch for integration. A package can ship targets that every consuming project imports, directly or transitively; one dedicated property (`FluentGwtIntegration`) then appends a compilation symbol to `DefineConstants` and emits an assembly-level marker through `AssemblyAttribute`, with no generated source of the library's own. Setting `DefineConstants` on the command line instead would replace the project's other constants (`DEBUG`, `TRACE`, the target-framework symbols) (C12). |
| **`TimeProvider` / `FakeTimeProvider`** | Replaces wall-clock stamps in fixtures and the obsolete authentication clock. C24. |
| **`Microsoft.Extensions.Http.Resilience`** | Standard resilience handlers add retries and timeouts: a stub returning 5xx triggers retries, slowing tests and changing call counts. Redirection must replace only the primary handler so resilience is still exercised (C16), and resilience delays become instant under `FakeTimeProvider`. Tests that are not about resilience may want it removed; that is a product-composition choice, not a library default. |
| **Keyed services** | Overrides and resolution must be key-aware (C15), otherwise "remove all registrations of a type" destroys unrelated keyed registrations. |
| **`ConfigureHttpClientDefaults`** | Replaces the post-configure-the-factory-options trick used to redirect every client (C16). |
| **`ValidateOnBuild` / `ValidateScopes`** | Make resolvable-first tests stronger by validating the whole graph at the first resolution. On by default for the fixture container, with a per-fixture opt-out (C1). |
| **Fake logging (`Microsoft.Extensions.Diagnostics.Testing`)** | Replaces bespoke log sinks for assertions (C23). |
| **xunit v3 per-test output via `TestContext`** | Replaces console and debug sinks, with output attributed to the right test under parallelism (C23). |
| **`string.GetHashCode` is randomised per process** | Any value derived from it changes every run. The owner's recorded convention derives a Bogus seed from a namespace's `GetHashCode()`; **that is not reproducible on .NET Core and later**. This library draws a fresh seed per fixture, lets a fixture declare a fixed one, and derives `TestId` with a stable hash (C14). |
| **Theory pre-enumeration** | xunit enumerates theory data at discovery; random data in rows breaks the match between discovered and executed rows. Generated rows draw only from an explicit seed (C13). |
| **EF Core in-memory provider** | Used by almost every hosted suite for isolation. Microsoft advises against it for testing behaviour that depends on a relational database. Not a library concern, but `TestId` (C14) serves a SQLite in-memory or container-backed alternative equally. |
| **.NET 10, C# 14** | `netstandard2.0`/C# 8 constraints (and the 2023 repository's `netstandard2.0`/C# 9) are gone. The `field` keyword, collection expressions and primary constructors simplify fixtures. |

---

## 31. Package split

Following the provider-package rule: the core holds the abstraction and takes no test-framework,
mocking, data or ASP.NET dependency — its one assertion dependency is AwesomeAssertions, for the
failures it raises itself; each integration is its own package extending the core through
extension methods in the root namespace, with `TryAdd` defaults.

| Package | Holds | Depends on |
|---|---|---|
| `FluentGwt` | Chain (2023, extended), phases, `ServiceFixture`, container lock and validation, overrides, configuration, integration marker attribute and its reading, hosted-service lifecycle and removal helpers, teardown, `FixtureHost` and `Attach`, seed selection, `TestId`, `FakeTimeProvider` wiring, log capture, `TestRunnerAttribute` | `Microsoft.Extensions.DependencyInjection`, `.Configuration.*`, `.Hosting.Abstractions`, `.TimeProvider.Testing`, `.Diagnostics.Testing`, `AwesomeAssertions`, `FluentGwt.Analysers` |
| `FluentGwt.Xunit` | Test token, `buildTransitive` targets for `FluentGwtIntegration` (the `INTEGRATION` symbol and the marker), `[IntegrationFact]`/`[IntegrationTheory]`, `IntegrationJustification`, `FixtureData`, test-output log sink, test identity for `TestId` | core, `xunit.v3.extensibility.core` |
| `FluentGwt.Bogus` | `Random`, `Fake` | core, `Bogus` |
| `FluentGwt.Moq` | `Stub<Service>()` | core, `Moq` |
| `FluentGwt.Http` | `RedirectHttp`, responders, request recording | core, `Microsoft.Extensions.Http` |
| `FluentGwt.AspNetCore` | `ApplicationHost`, host overrides (a `ConfigureServices` callback on the application's own collection, so hosted-service removals apply there), scopes, stubbed authentication and authorisation records, host-to-host redirection, socket hosting for entry-point hosts | core, `.Http`, `Microsoft.AspNetCore.Mvc.Testing` (and `Microsoft.AspNetCore.App` framework reference) |
| `FluentGwt.Analysers` | Roslyn analysers and code fixes `FG0001`–`FG0003`; no runtime code | `Microsoft.CodeAnalysis.CSharp.Workspaces` (analyser asset only) |

---

## 32. Build order

Dependency order only; no ranking beyond that.

1. **Modernise the 2023 base** — retarget to .NET 10, xunit v3 on Microsoft.Testing.Platform,
   AwesomeAssertions, tabs and file-scoped namespaces, and the `FluentGwt` package, assembly and
   solution names; keep its tests green (C2 as it stands).
2. **Execution model** — phases 1, 5, 6, 7 with a lazy, awaitable chain whose Then is the library's
   own type; C4, C5, C7, C8 (without the xunit token yet).
3. **C6** exception expectations (needs the act/arrangement split from step 2).
4. **C1** `ServiceFixture`, the container lock and validation; **C15** overrides; `FixtureHost`
   and `Attach` (phase 2 start, phase 7 disposal).
5. **C10** teardown and fixture cancellation (needs phase 7 and the fixture).
6. **C3** deferral (phase 3).
7. **C11** configuration, then **C12** in the core: the integration marker and its reading, the
   fixture-container hosted-service lifecycle (phase 4), and the hosted-service removal helpers.
8. **C14** seed selection, replay and `TestId`; **C24** time.
9. **Xunit package** — test token, the `buildTransitive` targets for `FluentGwtIntegration`,
   integration attributes with justification and reason, test identity, **C13**, log sink
   (**C23**).
10. **Bogus** and **Moq** packages.
11. **Http package** — **C16**.
12. **AspNetCore package** — **C17** (`ApplicationHost`, attached and started in phase 2, starting
    completely), then **C18** (the `ConfigureServices` callback, in which C12's hosted-service
    removals apply), **C19**, **C20**, **C21**, **C22** (entry-point hosts
    only).
13. **Analysers package** — **C25** (needs the chain types and `ApplicationHost` to analyse against).
14. **Package metadata and reproducible builds** — **D1**.
15. **Continuous integration** — **D2** (needs D1 to pack in CI).
16. **Branching model and protection** — **D3** (needs D2's checks to require).
17. **Versioning, prereleases from dev, and releases from main** — **D4** (needs D1–D3).
18. **README badges** — **D5** (needs D2 and D4 to point at).

---

## 33. Rulings, 2026-10-08

The owner's rulings on the twenty-one questions this section previously held. Each is folded into
the sections it names. Where a later ruling supersedes an earlier one, the earlier row is kept for
the record and marked; the sections follow the later ruling.

| # | Question | Ruling |
|---|---|---|
| 1 | Private fixture vs extension steps | `internal sealed class Fixture`, exposed as `private Fixture Context { get; } = new();` — the library's convention, not a deviation (§1). |
| 2 | Then's return type | The library's own awaitable chain type, implicitly convertible to `Task`; `.And` can follow a Then (C5, C7). |
| 3 | Hosted-service start failure | Fails the test as an arrangement failure (C12). |
| 4 | `EnableIntegration` parsing and gating | **Superseded in part by ruling 19.** Attribute-only gating; the chain step is removed. ~~Any non-empty value enables integration.~~ The attribute's reason is the skip reason. ~~Consumer-side `#if` wrapping is the intended pattern, needing no library support.~~ Attribute-only gating and the reason in the skip message stand (C12). |
| 5 | Hosted services in test hosts when integration is off | **Superseded by ruling 21.** ~~Removed by default — the application's own, by assembly rule, never the framework's — with a per-service opt-back-in.~~ |
| 6 | Tolerating a failing act | Dropped; C9 is withdrawn. |
| 7 | Per-test vs class-shared hosts | One host per test; no class-shared host (C17). |
| 8 | Real token issuance | Out of scope. Real providers (Entra ID and the like) are replaced by stub handlers under the same scheme names, exposing the principal, the scheme and the authorisation decisions (C20). |
| 9 | Naming | `FluentGwt` for package, assembly, root namespace, repository and solution; packages `FluentGwt`, `.Xunit`, `.AspNetCore`, `.Http`, `.Moq`, `.Bogus` (§31). |
| 10 | A type the brief named but the 2023 code does not contain | Dropped from the document. |
| 11 | Untargeted chains | Kept: simple tests need no fixture (C2). |
| 12 | Seed scope and override | A fresh seed per fixture each run unless the fixture, or a common class it uses, declares one; reported on failure; replayed with `FluentGwtSeed` (C14). |
| 13 | `ValidateOnBuild`/`ValidateScopes` | On by default for the fixture container, with an opt-out (C1). |
| 14 | Assertion dependency in core | AwesomeAssertions (Apache-2.0) is acceptable, including in core for exception-expectation messages (C5, C6, §31). |
| 15 | Analysers | Shipped, in `FluentGwt.Analysers`, for the three faults (C25). |
| 16 | Integration justification | Revived: an `IntegrationJustification` flags enum alongside the reason, both in the skip message (C12). |
| 17 | `TestId` uniqueness | Derived from the seed plus the test's identity: reproducible, and distinct between tests sharing a seed (C14). |
| 18 | Socket hosting mechanism | `WebApplicationFactory`'s Kestrel mode (`UseKestrel`, .NET 10); never a hand-bound Kestrel (C22). |
| 19 | `EnableIntegration=false`; how integration is switched on | **Supersedes ruling 4 in part.** Integration gating is compile-time; there is no run-time `EnableIntegration` switch, so the question of parsing `false` is moot. The xunit package's `buildTransitive` targets read the MSBuild property `FluentGwtIntegration`: set to `true`, they append `INTEGRATION` to `DefineConstants` and emit the assembly marker `[assembly: IntegrationEnabled]`. `[IntegrationFact]`/`[IntegrationTheory]` read the marker at run time — absent, skipped with the justification and reason; present, run — so consumers write the attribute once, with no `#if` per test. A dedicated property, because `-p:DefineConstants=...` replaces the project's other constants. Fixture-container hosted services start in phase 4 only when the marker is present (C11, C12). |
| 20 | Socket hosting for test-composed hosts | `.OnSockets()` is available only for hosts built from a real entry point, through `WebApplicationFactory`'s Kestrel mode (ruling 18 stands). A test-composed host cannot be put on sockets: `.OnSockets()` on one is an arrangement failure explaining why (C17, C22). |
| 21 | The boundary of hosted-service removal | **Supersedes ruling 5.** Test hosts start completely, the application's own hosted services included, regardless of integration; there is no assembly-based removal and no `KeepHostedService`. Arrangement helpers remove what a test does not want: `RemoveHostedService<Implementation>()` (and `RemoveHostedService(Type)`) by implementation type, and `RemoveApplicationHostedServices()`, which never removes the framework's. The application-opted framework service of the question is therefore kept by the convenience and removed by naming it (C12, C17). |

### Open questions

None.

---

## 34. Delivery: CI, branching, versioning and publishing

Added 2026-10-08 at the owner's request. These are delivery steps rather than library capabilities,
numbered D1–D5 and placed after C25 in the build order (§32, steps 14–18). They follow the same
pattern: what problem each solves, what is done, and how it is checked. Where a check can be a test
it is one; where it is a repository setting, the check is a command whose output proves it.

**Who does what.** The repository is public and the owner's standing instruction is that nothing is
pushed or published without asking. So every step that changes GitHub settings, creates a branch on
the remote, stores a secret or publishes a package is marked **(owner)**: it is either performed by
the owner or performed only after the owner approves that specific action. The local `gh` CLI is
signed in as the ATG account, which must not be used for this repository; anything done through
`gh` needs the owner's own account.

### D1 — Package metadata and reproducible builds

**Problem.** A package with no licence expression, readme, repository link or symbols is hard to
trust and hard to debug into, and a build that embeds local paths is not reproducible.

**Done.**
- A `src/Directory.Build.props` gives every library project: `PackageId` (its `AssemblyName`),
  `Authors`, a per-package `Description` and `PackageTags`, `PackageLicenseExpression` `Apache-2.0`
  (the repository's `LICENSE`), `PackageProjectUrl` and `RepositoryUrl`
  (`https://github.com/smudge202/fluentgwt`), `PackageReadmeFile` with the README packed in,
  `IncludeSymbols` with `SymbolPackageFormat` `snupkg`, `PublishRepositoryUrl`,
  `EmbedUntrackedSources`, and `ContinuousIntegrationBuild` when `GITHUB_ACTIONS` is set. Source
  Link for GitHub ships with the .NET SDK and needs no package.
- Test and helper projects are `IsPackable=false`.
- The xunit package keeps its `buildTransitive` targets (C12).

**Checked by** an integration test in the style of the C12 build-target tests:
- `WhenLibraryIsPackedThenEveryPackageCarriesLicenceReadmeRepositoryAndSymbols` — packs the solution
  to a temp folder and reads each `.nuspec`: licence expression, readme, repository URL and commit,
  and a matching `.snupkg`.
- `WhenTestProjectsArePackedThenNothingIsProduced`.

### D2 — Continuous integration

**Problem.** Tests that only run on the owner's machine protect nothing once there are pull
requests, and the integration build (C12) is the one most likely to be skipped by hand.

**Done.** `.github/workflows/ci.yml`:
- Runs on pull requests into `dev` and `main`, and on pushes to `dev`, `main` and `v*` tags.
- Jobs on `ubuntu-latest` only — no Windows job (ruling 26): `actions/setup-dotnet` from
  `global.json`; `dotnet format --verify-no-changes`; `dotnet build -warnaserror`;
  `dotnet test --solution FluentGwt.slnx` (the default build); and
  `dotnet test --solution FluentGwt.slnx -p:FluentGwtIntegration=true` (the integration build,
  which needs network for the transitive-package test).
- Test results as TRX (`Microsoft.Testing.Extensions.TrxReport`) uploaded as an artefact, and a step
  that fails the job if a test project ran zero tests — the "solution omits its test project" trap.
- `permissions: contents: read`; third-party actions pinned to a commit SHA, not a tag; a
  `concurrency` group that cancels superseded runs on the same pull request.
- Dependency updates by **Dependabot** (ruling 24): `.github/dependabot.yml` covers the `nuget` and
  `github-actions` ecosystems with `target-branch: dev`, so each update arrives as a pull request
  into `dev` that CI must pass.

**Checked by** the workflow running green on the first pull request, with each job's test count in
its log, and by a deliberately failing commit on a throwaway branch turning it red (then deleted).

### D3 — Branching model and protection (owner)

**Problem.** Without protection a direct push to `main` can publish an untested package (D4), and
without a model there is no place for unreleased work to collect.

**Done.** Gitflow:

| Branch | Purpose | Created from | Merges into |
|---|---|---|---|
| `main` | released code; every commit on it is a published version | — | — |
| `dev` | integration; the repository's **default** branch; every push builds and publishes an alpha prerelease (D4) | `main` | `release/*` |
| `feature/*` | one capability or change | `dev` | `dev`, by pull request |
| `release/x.y.z` | stabilising a version | `dev` | `main`, then back into `dev` |
| `hotfix/x.y.z` | an urgent fix to a release | `main` | `main`, then back into `dev` |

The integration branch is named `dev`, not `develop` (ruling 23). Feature branches target `dev`,
and because `dev` is the default branch, a pull request opened without choosing a base targets it
too.

The current `clean-room-rebuild` branch becomes the first feature branch: `dev` is created from
`main`, made the default branch, and the rebuild arrives in it by pull request **(owner)**.

Branch protection, as GitHub rulesets on `main` and `dev` **(owner)**:
- pull request required, no direct pushes;
- required status checks: every D2 job, and the branch up to date before merging;
- no force pushes, no deletion;
- signed commits required (local commits are signed; merges made in the GitHub UI are signed by
  GitHub);
- required approvals **0** while the owner is the only maintainer, since GitHub does not let an
  author approve their own pull request; raise to 1 when a second maintainer exists.

A tag ruleset restricts creating `v*` tags to the owner, since a tag is what publishes (D4).

**Checked by** `gh api repos/smudge202/fluentgwt/rulesets` listing the rulesets,
`gh api repos/smudge202/fluentgwt --jq .default_branch` printing `dev`, and a direct push to `dev`
being refused.

### D4 — Versioning, prereleases from dev, and releases from main (owner approves each release)

**Problem.** Versions typed by hand drift from what was released; a publish that can run from any
branch can release untested code; a re-publish of an existing version is rejected late or silently
ignored; and unreleased work on `dev` should be installable without claiming a released number.

**Done.**
- **Versions come from tags, through MinVer** (ruling 22): every library project references
  `MinVer` (a build-time-only package reference). A `vX.Y.Z` tag on `main` is version `X.Y.Z`
  (`MinVerTagPrefix` `v`); every other commit builds as a prerelease of the next patch version, so a
  local or CI build never claims a released number. `MinVerDefaultPreReleaseIdentifiers` is
  `alpha`, so MinVer appends the height — the commit count since the last tag — giving
  `x.y.z-alpha.N`. MinVer reads tags only, which suits gitflow: the version decision is the tag
  made when a release branch lands on `main`.
- **Prereleases from `dev` go to GitHub Packages** (ruling 23). `.github/workflows/prerelease.yml`
  runs on every push to `dev` (a merged pull request) and:
  1. runs the D2 build and both test runs on that commit;
  2. packs in `Release`, so every package is `x.y.z-alpha.N`;
  3. pushes every `.nupkg` to `https://nuget.pkg.github.com/smudge202/index.json` with the
     workflow's own `GITHUB_TOKEN`, under `permissions: contents: read, packages: write`. No secret
     is stored.

  GitHub Packages is free for public repositories. **Consumers must authenticate to the GitHub
  NuGet registry even for a public package**: a package source for that URL with their GitHub user
  name and a personal access token carrying `read:packages`. The README says so beside the
  prerelease feed.
- **Releases from `main` go to nuget.org.** `.github/workflows/publish.yml` runs on a pushed `v*`
  tag and:
  1. fails unless the tagged commit is on `main` (`git merge-base --is-ancestor`);
  2. runs the D2 build and both test runs again on that exact commit;
  3. packs in `Release`;
  4. **fails if any package's version already exists on nuget.org** — checked before pushing anything,
     so a release is all packages or none;
  5. pushes every `.nupkg` and `.snupkg` to nuget.org through **Trusted Publishing** (ruling 25):
     the job has `permissions: id-token: write` (and `contents: write` for step 6); the
     `NuGet/login` action (pinned to a commit SHA, D2) exchanges the job's GitHub OIDC token for a
     short-lived nuget.org API key, given the owner's nuget.org user name from the repository
     variable `NUGET_USER` (a name, not a secret); and
     `dotnet nuget push "*.nupkg" --api-key <the action's NUGET_API_KEY output> --source https://api.nuget.org/v3/index.json`
     pushes with that key. No API key is stored as a secret, so there is nothing to leak or rotate;
  6. creates a GitHub release for the tag with the packages attached.
- The publish job runs in a GitHub **environment** named `nuget` with the owner as required reviewer,
  so every release waits for the owner's approval even after the tag is pushed.
- Releasing is therefore: merge `release/x.y.z` into `main` → tag `vx.y.z` on `main` → approve the
  `nuget` environment.
- **Trusted Publishing setup (owner)**, performed once from these steps before the first release:
  1. Sign in to nuget.org as the account that will own the packages; from the account menu open
     **Trusted Publishing** and add a policy.
  2. Repository owner `smudge202`, repository `fluentgwt`, workflow file `publish.yml`,
     environment `nuget`. Scopes: **Push**, with **Push new packages and package versions**;
     **Unlist or relist** left off. Glob pattern `FluentGwt*` — not `*`, which would let this
     workflow publish any package the account owns.
     **Done 2026-10-08:** the policy exists with these settings.
  2a. After the first release to nuget.org, once every `FluentGwt*` ID exists, edit the policy's
     push scope to **Push only new package versions**, so the workflow can never create a new
     package ID.
  3. In the GitHub repository, add the repository variable `NUGET_USER` holding that nuget.org
     user name, and create the `nuget` environment with the owner as required reviewer.
- **ID prefix reservation (owner)** (ruling 27), after the first release to nuget.org, since
  eligibility likely requires a published package:
  1. Confirm the `FluentGwt` packages are listed under the owner's nuget.org account.
  2. Request reservation of the `FluentGwt` prefix (covering `FluentGwt.*`) by the route nuget.org's
     ID prefix reservation documentation gives — at the time of writing, an email to
     `account@nuget.org` from the owning account's address naming the account and the prefix.
  3. Once granted, every `FluentGwt` package shows the verified check mark, and nobody else can
     publish a new `FluentGwt.*` ID.

**Checked by** a dry run: the publish workflow on a tag in a fork or with the push step disabled,
showing the version derived, the existence check passing, and the packages it would push; and, for
prereleases, the first push to `dev` producing `x.y.z-alpha.N` packages listed under the
repository's **Packages**.

### D5 — README badges

**Problem.** A reader should see at a glance whether the build is green, what the current version
is and what the licence is.

**Done.** A badge row at the top of the README:
- CI status for `dev` and for `main` (the D2 workflow's badge);
- nuget.org version for each package (`FluentGwt`, `.Xunit`, `.AspNetCore`, `.Http`, `.Moq`,
  `.Bogus`, `.Analysers`), and total downloads for `FluentGwt`;
- licence (Apache-2.0) and target framework (.NET 10).

Badges for packages that are not yet published are added with the first publish, not before — a
badge reading "not found" is worse than none.

**Checked by** every badge resolving once D2 and D4 have run.

### Rulings

The owner's rulings on the six delivery questions this section previously held, folded into D2–D5.

22. **Versioning tool: MinVer.** Tags only; gitflow puts the version decision on the tag made when a
    release branch lands on `main`, which is exactly what MinVer reads (D4).
23. **Prereleases: from `dev`, to GitHub Packages.** The integration branch is named `dev`, not
    `develop`. Every push to `dev` builds an alpha prerelease (`x.y.z-alpha.N`, MinVer's prerelease
    from height) and publishes it to GitHub Packages, which is free for public repositories;
    consumers must authenticate to the GitHub NuGet registry even for public packages, with a
    personal access token carrying `read:packages`. Releases from `main` go to nuget.org (D3, D4).
24. **Dependency updates: Dependabot**, for the `nuget` and `github-actions` ecosystems, targeting
    `dev` (D2).
25. **Publishing credential: nuget.org Trusted Publishing.** No API key secret; the publish job has
    `permissions: id-token: write` and uses the `NuGet/login` action to exchange the OIDC token for a
    short-lived key, then `dotnet nuget push` with that key. The owner sets it up from the steps in
    D4.
26. **CI platform: Linux only** (`ubuntu-latest`); no Windows job (D2).
27. **Package ID prefix: reserve `FluentGwt` on nuget.org after the first publish**, since
    eligibility likely requires a published package. The owner performs it from the steps in D4.
