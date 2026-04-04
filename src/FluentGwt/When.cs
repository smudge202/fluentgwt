namespace FluentGwt;

public sealed class When<Target>
{
	private readonly State<Target> _given;
	private readonly Func<Target, ValueTask> _act;

	internal When(State<Target> given, Func<Target, ValueTask> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		_given = given;
		_act = act;
	}

	public Then<Target> Then(Action<Target> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return new(this, x =>
		{
			assertion(x);
			return ValueTask.CompletedTask;
		});
	}

	internal async Task<Target> Act()
	{
		var target = await _given.Arrange();
		await _act(target);
		return target;
	}
}
