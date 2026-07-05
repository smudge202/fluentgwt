using System.Runtime.ExceptionServices;

namespace FluentGwt;

internal static class Teardown
{
	public static async Task Around<Target>(State<Target> given, Func<Task> body)
	{
		var execution = body();
		await execution.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		var fixture = given.Current as ServiceFixture;
		var failures = fixture is null ? [] : await fixture.TearDown();
		if (fixture is not null && (failures.Count > 0 || !execution.IsCompletedSuccessfully))
			fixture.ReportSeed();
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
