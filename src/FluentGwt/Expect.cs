using AwesomeAssertions;

namespace FluentGwt;

internal static class Expect
{
	public static Func<Task<(Target, Failure)>> Act<Target, Failure>(Func<Task<Target>> arrange, Func<Target, ValueTask> act, bool exactly)
		where Failure : Exception =>
		async () =>
		{
			var target = await arrange();
			return (target, await Throws<Failure>(async () => await act(target), exactly));
		};

	public static Func<Task<(Target, Failure)>> Arrangement<Target, Failure>(State<Target> given)
		where Failure : Exception =>
		async () =>
		{
			var failure = await Throws<Failure>(given.Arrange, exactly: false);
			return (given.Current, failure);
		};

	public static Func<Target, Failure, ValueTask> Nothing<Target, Failure>() => (_, _) => ValueTask.CompletedTask;

	private static async Task<Failure> Throws<Failure>(Func<Task> act, bool exactly)
		where Failure : Exception =>
		exactly
			? (await act.Should().ThrowExactlyAsync<Failure>()).Which
			: (await act.Should().ThrowAsync<Failure>()).Which;
}
