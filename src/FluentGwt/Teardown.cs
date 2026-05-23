using System.Runtime.ExceptionServices;

namespace FluentGwt;

internal static class Teardown
{
	public static async Task Around<Target>(State<Target> given, Func<Task> body)
	{
		var execution = body();
		await execution.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		var failures = given.Current is ServiceFixture fixture ? await fixture.TearDown() : [];
		if (failures.Count == 0 || execution.IsCompletedSuccessfully)
		{
			await execution;
			Rethrow(failures);
			return;
		}
		throw new AggregateException([.. execution.Exception?.InnerExceptions ?? [new TaskCanceledException(execution)], .. failures]);
	}

	public static void Rethrow(IReadOnlyList<Exception> failures)
	{
		if (failures.Count == 1)
			ExceptionDispatchInfo.Throw(failures[0]);
		if (failures.Count > 1)
			throw new AggregateException(failures);
	}
}
