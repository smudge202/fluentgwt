namespace FluentGwt;

internal static class Step
{
	public static Func<T, ValueTask> From<T>(Action<T> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x =>
		{
			step(x);
			return ValueTask.CompletedTask;
		};
	}

	public static Func<T, ValueTask> From<T>(Func<T, Task> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return x => new ValueTask(step(x));
	}

	public static Func<T, ValueTask> From<T>(Func<T, ValueTask> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return step;
	}

	public static Func<T, Result, ValueTask> From<T, Result>(Action<T, Result> step)
	{
		ArgumentNullException.ThrowIfNull(step);
		return (x, result) =>
		{
			step(x, result);
			return ValueTask.CompletedTask;
		};
	}

	public static Func<T, Result, ValueTask> OnResult<T, Result>(Func<Result, ValueTask> step) => (_, result) => step(result);

	public static Func<T, Result, ValueTask> OnTarget<T, Result>(Func<T, ValueTask> step) => (x, _) => step(x);

	public static Func<T, ValueTask> FollowedBy<T>(this Func<T, ValueTask> first, Func<T, ValueTask> next) =>
		async x =>
		{
			await first(x);
			await next(x);
		};

	public static Func<T, Result, ValueTask> FollowedBy<T, Result>(this Func<T, Result, ValueTask> first, Func<T, Result, ValueTask> next) =>
		async (x, result) =>
		{
			await first(x, result);
			await next(x, result);
		};
}
