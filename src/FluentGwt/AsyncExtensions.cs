namespace FluentGwt;

internal static class AsyncExtensions
{
	public static Task AsCompletedTask(this Action? transition)
	{
		transition?.Invoke();
		return Task.CompletedTask;
	}

	public static Func<Target, Task> AsCompletedTask<Target>(this Action<Target>? transition) => x =>
	{
		transition?.Invoke(x);
		return Task.CompletedTask;
	};
}
