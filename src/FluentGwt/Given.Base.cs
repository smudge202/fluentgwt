using System.Runtime.CompilerServices;

namespace FluentGwt;

public abstract record GivenBase<T> : State<T>
{
	public When<T> When(Action<T> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x =>
		{
			act(x);
			return ValueTask.CompletedTask;
		});
	}

	[OverloadResolutionPriority(1)]
	public When<T> When(Func<T, Task> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return new(this, x => new ValueTask(act(x)));
	}

	public When<T> When(Func<T, ValueTask> act) => new(this, act);

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

	public When<T, Result> When<Result>(Func<T, ValueTask<Result>> act) => new(this, act);
}
