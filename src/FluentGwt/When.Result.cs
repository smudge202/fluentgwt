using System.Runtime.CompilerServices;

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
		return Then((_, result) => assertion(result));
	}

	public Then<Target, Result> Then(Action<Target, Result> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return new(this, (target, result) =>
		{
			assertion(target, result);
			return ValueTask.CompletedTask;
		});
	}

	[OverloadResolutionPriority(1)]
	public Then<Target, Result> Then(Func<Result, Task> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return new(this, (_, result) => new ValueTask(assertion(result)));
	}

	public Then<Target, Result> Then(Func<Result, ValueTask> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return new(this, (_, result) => assertion(result));
	}

	public Then<Target, Result> ThenFixture(Action<Target> assertion)
	{
		ArgumentNullException.ThrowIfNull(assertion);
		return Then((target, _) => assertion(target));
	}

	internal async Task<(Target Target, Result Result)> Act()
	{
		var target = await _given.Arrange();
		return (target, await _act(target));
	}
}
