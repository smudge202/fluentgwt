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

	public ThenThrows<Target, Failure> ThenThrows<Failure>() where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: false), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenThrows<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: false), Step.OnResult<Target, Failure>(Step.From(assertion)));

	public ThenThrows<Target, Failure> ThenThrows<Failure>(Action<Target, Failure> assertion) where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: false), Step.From(assertion));

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>() where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: true), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: true), Step.OnResult<Target, Failure>(Step.From(assertion)));

	public ThenThrows<Target, Failure> ThenThrowsExactly<Failure>(Action<Target, Failure> assertion) where Failure : Exception =>
		new(Expect.Act<Target, Failure>(_given.Arrange, _act, exactly: true), Step.From(assertion));

	public ThenThrows<Target, Failure> ThenArrangementFails<Failure>() where Failure : Exception =>
		new(Expect.Arrangement<Target, Failure>(_given), Expect.Nothing<Target, Failure>());

	public ThenThrows<Target, Failure> ThenArrangementFails<Failure>(Action<Failure> assertion) where Failure : Exception =>
		new(Expect.Arrangement<Target, Failure>(_given), Step.OnResult<Target, Failure>(Step.From(assertion)));

	internal async Task<Target> Act()
	{
		var target = await _given.Arrange();
		await _act(target);
		return target;
	}
}
