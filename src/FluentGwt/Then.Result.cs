using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class Then<Target, Result>
{
	private readonly When<Target, Result> _when;
	private readonly Func<Target, Result, ValueTask> _assertion;
	private readonly Lazy<Task> _execution;

	internal Then(When<Target, Result> when, Func<Target, Result, ValueTask> assertion)
	{
		_when = when;
		_assertion = assertion;
		_execution = new(Execute);
	}

	public static implicit operator Task(Then<Target, Result> then)
	{
		ArgumentNullException.ThrowIfNull(then);
		return then.ToTask();
	}

	public Then<Target, Result> And(Action<Result> assertion) => Followed(Step.OnResult<Target, Result>(Step.From(assertion)));

	[OverloadResolutionPriority(1)]
	public Then<Target, Result> And(Func<Result, Task> assertion) => Followed(Step.OnResult<Target, Result>(Step.From(assertion)));

	public Then<Target, Result> And(Func<Result, ValueTask> assertion) => Followed(Step.OnResult<Target, Result>(Step.From(assertion)));

	public Then<Target, Result> And(Action<Target, Result> assertion) => Followed(Step.From(assertion));

	public Then<Target, Result> AndFixture(Action<Target> assertion) => Followed(Step.OnTarget<Target, Result>(Step.From(assertion)));

	[OverloadResolutionPriority(1)]
	public Then<Target, Result> AndFixture(Func<Target, Task> assertion) => Followed(Step.OnTarget<Target, Result>(Step.From(assertion)));

	public Then<Target, Result> AndFixture(Func<Target, ValueTask> assertion) => Followed(Step.OnTarget<Target, Result>(Step.From(assertion)));

	public Task ToTask() => _execution.Value;

	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	private Then<Target, Result> Followed(Func<Target, Result, ValueTask> assertion) => new(_when, _assertion.FollowedBy(assertion));

	private async Task Execute()
	{
		var (target, result) = await _when.Act();
		await _assertion(target, result);
	}
}
