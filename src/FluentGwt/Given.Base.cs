using System.Runtime.CompilerServices;

namespace FluentGwt;

public abstract record GivenBase<T> : State<T>
{
	public When<T> When(Action<T> act) => new(this, Step.From(act));

	[OverloadResolutionPriority(1)]
	public When<T> When(Func<T, Task> act) => new(this, Step.From(act));

	public When<T> When(Func<T, ValueTask> act) => new(this, Step.From(act));

	public When<T, Result> When<Result>(Func<T, Result> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => ValueTask.FromResult(act(x)));
	}

	[OverloadResolutionPriority(1)]
	public When<T, Result> When<Result>(Func<T, Task<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => new ValueTask<Result>(act(x)));
	}

	public When<T, Result> When<Result>(Func<T, ValueTask<Result>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, act);
	}
}
