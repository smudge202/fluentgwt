namespace FluentGwt;

internal static class Step
{
	public static Func<Target, ValueTask> From<Target>(Action<Target> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x =>
		{
			step(x);
			return ValueTask.CompletedTask;
		};
	}

	public static Func<Target, ValueTask> From<Target>(Func<Target, Task> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x => new ValueTask(step(x));
	}

	public static Func<Target, ValueTask> From<Target>(Func<Target, ValueTask> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return step;
	}

	public static Func<Target, ValueTask> From<Target>(Func<Target, CancellationToken, Task> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x => new ValueTask(step(x, Runner.Token));
	}

	public static Func<Target, ValueTask> From<Target>(Func<Target, CancellationToken, ValueTask> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x => step(x, Runner.Token);
	}

	public static Func<Target, Result, ValueTask> From<Target, Result>(Action<Target, Result> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return (x, result) =>
		{
			step(x, result);
			return ValueTask.CompletedTask;
		};
	}

	public static Func<Target, Result, ValueTask> OnResult<Target, Result>(Func<Result, ValueTask> step) => (_, result) => step(result);

	public static Func<Target, Result, ValueTask> OnTarget<Target, Result>(Func<Target, ValueTask> step) => (x, _) => step(x);

	public static Func<Target, ValueTask> FollowedBy<Target>(this Func<Target, ValueTask> first, Func<Target, ValueTask> next) =>
		async x =>
		{
			await first(x);
			await next(x);
		};

	public static Func<Target, Result, ValueTask> FollowedBy<Target, Result>(this Func<Target, Result, ValueTask> first, Func<Target, Result, ValueTask> next) =>
		async (x, result) =>
		{
			await first(x, result);
			await next(x, result);
		};
}
