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

	public When<Target, Result> And(Action<Target> step) => Preserving(Step.From(step));

	[OverloadResolutionPriority(1)]
	public When<Target, Result> And(Func<Target, Task> step) => Preserving(Step.From(step));

	public When<Target, Result> And(Func<Target, ValueTask> step) => Preserving(Step.From(step));

	[OverloadResolutionPriority(1)]
	public When<Target, Result> And(Func<Target, CancellationToken, Task> step) => Preserving(Step.From(step));

	public When<Target, Result> And(Func<Target, CancellationToken, ValueTask> step) => Preserving(Step.From(step));

	public When<Target, Next> AndResult<Next>(Func<Target, Next> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return Replacing(x => ValueTask.FromResult(act(x)));
	}

	[OverloadResolutionPriority(1)]
	public When<Target, Next> AndResult<Next>(Func<Target, Task<Next>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return Replacing(x => new ValueTask<Next>(act(x)));
	}

	public When<Target, Next> AndResult<Next>(Func<Target, ValueTask<Next>> act)
	{
		ArgumentNullException.ThrowIfNull(act);
		return Replacing(act);
	}

	public Then<Target, Result> Then(Action<Result> assertion) =>
		new(this, Step.OnResult<Target, Result>(Step.From(assertion)));

	[OverloadResolutionPriority(1)]
	public Then<Target, Result> Then(Func<Result, Task> assertion) =>
		new(this, Step.OnResult<Target, Result>(Step.From(assertion)));

	public Then<Target, Result> Then(Func<Result, ValueTask> assertion) =>
		new(this, Step.OnResult<Target, Result>(Step.From(assertion)));

	public Then<Target, Result> Then(Action<Target, Result> assertion) => new(this, Step.From(assertion));

	public Then<Target, Result> ThenFixture(Action<Target> assertion) =>
		new(this, Step.OnTarget<Target, Result>(Step.From(assertion)));

	public ThenThrows<Target, Failure> ThenThrows<Failure>() where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: false), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenThrows<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: false), Step.OnResult<Target, Failure>(Step.From(assertion)));

	public ThenThrows<Target, Failure> ThenThrows<Failure>(Action<Target, Failure> assertion) where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: false), Step.From(assertion));

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>() where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: true), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: true), Step.OnResult<Target, Failure>(Step.From(assertion)));

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>(Action<Target, Failure> assertion) where Failure : Exception =>
		new(_given, Expect.Act<Target, Failure>(_given.Arrange, Discarding, exactly: true), Step.From(assertion));

	public ThenThrows<Target, Failure> ThenArrangementFails<Failure>() where Failure : Exception =>
		new(_given, Expect.Arrangement<Target, Failure>(_given), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenArrangementFails<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(_given, Expect.Arrangement<Target, Failure>(_given), Step.OnResult<Target, Failure>(Step.From(assertion)));

	internal State<Target> Given => _given;

	internal async Task<(Target Target, Result Result)> Act()
	{
		var target = await _given.Arrange();
		return (target, await _act(target));
	}

	private async ValueTask Discarding(Target target) => await _act(target);

	private When<Target, Result> Preserving(Func<Target, ValueTask> step) =>
		new(_given, async x =>
		{
			var result = await _act(x);
			await step(x);
			return result;
		});

	private When<Target, Next> Replacing<Next>(Func<Target, ValueTask<Next>> act) =>
		new(_given, async x =>
		{
			await _act(x);
			return await act(x);
		});
}
