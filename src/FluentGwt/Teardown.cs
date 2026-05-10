namespace FluentGwt;

internal static class Teardown
{
	public static async Task Around<Target>(State<Target> given, Func<Task> body)
	{
		try
		{
			await body();
		}
		finally
		{
			if (given.Current is ServiceFixture fixture)
				await fixture.DisposeAsync();
		}
	}
}
