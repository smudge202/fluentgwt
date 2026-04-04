namespace FluentGwt;

public sealed class When<Target, Result>
{
	private readonly State<Target> _given;
	private readonly Func<Target, ValueTask<Result>> _act;

	internal When(State<Target> given, Func<Target, ValueTask<Result>> act)
	{
		_given = given;
		_act = act;
	}

	public Then<Target, Result> Then(Action<Result> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return new(this, (_, result) =>
		{
			assertion(result);
			return ValueTask.CompletedTask;
		});
	}

	internal async Task<(Target Target, Result Result)> Act()
	{
		var target = await _given.Arrange();
		return (target, await _act(target));
	}
}
