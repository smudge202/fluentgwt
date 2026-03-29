namespace FluentGwt;

public sealed class When<Target>
{
	private readonly State<Target> _given;
	private readonly Action<Target> _act;

	internal When(State<Target> given, Action<Target> act)
	{
		_given = given;
		_act = act;
	}

	public Then<Target> Then(Action<Target> assertion) => new(this, assertion);

	internal async Task<Target> Act()
	{
		var target = await _given.Arrange();
		_act(target);
		return target;
	}
}
