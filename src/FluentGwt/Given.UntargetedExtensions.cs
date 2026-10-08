namespace FluentGwt;

public static class UntargetedGivenExtensions
{
	public static Given Given(this Given given, Action transition) =>
		given.Given(transition.AsCompletedTask);

	public static Given Given(this Given given, Func<Task> transition)
	{
		ArgumentNullException.ThrowIfNull(given);
		given.AddTransition(_ => transition());
		return given;
	}
}
