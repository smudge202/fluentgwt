using System.Runtime.CompilerServices;

namespace FluentGwt;

public abstract record GivenBase<Target> : State<Target>
{
	public When<Target> When(Action<Target> act) => new(this, Step.From(act));

	[OverloadResolutionPriority(1)]
	public When<Target> When(Func<Target, Task> act) => new(this, Step.From(act));

	public When<Target> When(Func<Target, ValueTask> act) => new(this, Step.From(act));

	[OverloadResolutionPriority(1)]
	public When<Target> When(Func<Target, CancellationToken, Task> act) => new(this, Step.From(act));

	public When<Target> When(Func<Target, CancellationToken, ValueTask> act) => new(this, Step.From(act));

	[OverloadResolutionPriority(1)]
	public When<Target, Result> When<Result>(Func<Target, CancellationToken, Task<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => new ValueTask<Result>(act(x, Runner.Token)));
	}

	public When<Target, Result> When<Result>(Func<Target, CancellationToken, ValueTask<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => act(x, Runner.Token));
	}

	public When<Target, Result> When<Result>(Func<Target, Result> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => ValueTask.FromResult(act(x)));
	}

	[OverloadResolutionPriority(1)]
	public When<Target, Result> When<Result>(Func<Target, Task<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => new ValueTask<Result>(act(x)));
	}

	public When<Target, Result> When<Result>(Func<Target, ValueTask<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, act);
	}

	public When<Target, Service> WhenResolving<Service>() where Service : notnull =>
		When(x => x is ServiceFixture fixture
			? fixture.Resolve<Service>()
			: throw new InvalidOperationException(
				$"WhenResolving needs a chain whose target is a {nameof(ServiceFixture)}; this chain's target is {typeof(Target).Name}."));
}
