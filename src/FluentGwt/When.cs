using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class When<Target>
{
	private readonly State<Target> _given;
	private readonly Func<Target, ValueTask> _act;

	internal When(State<Target> given, Func<Target, ValueTask> act)
	{
		_given = given;
		_act = act;
	}

	public When<Target> And(Action<Target> step) => new(_given, _act.FollowedBy(Step.From(step)));

	[OverloadResolutionPriority(1)]
	public When<Target> And(Func<Target, Task> step) => new(_given, _act.FollowedBy(Step.From(step)));

	public When<Target> And(Func<Target, ValueTask> step) => new(_given, _act.FollowedBy(Step.From(step)));

	public Then<Target> Then(Action<Target> assertion) => new(this, Step.From(assertion));

	[OverloadResolutionPriority(1)]
	public Then<Target> Then(Func<Target, Task> assertion) => new(this, Step.From(assertion));

	public Then<Target> Then(Func<Target, ValueTask> assertion) => new(this, Step.From(assertion));

	internal async Task<Target> Act()
	{
		var target = await _given.Arrange();
		await _act(target);
		return target;
	}
}
