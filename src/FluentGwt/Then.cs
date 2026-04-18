using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class Then<Target>
{
	private readonly When<Target> _when;
	private readonly Func<Target, ValueTask> _assertion;
	private readonly Lazy<Task> _execution;

	internal Then(When<Target> when, Func<Target, ValueTask> assertion)
	{
		_when = when;
		_assertion = assertion;
		_execution = new(Execute);
	}

	public static implicit operator Task(Then<Target> then)
	{
		ArgumentNullException.ThrowIfNull(then);
		return then.ToTask();
	}

	public Then<Target> And(Action<Target> assertion) => new(_when, _assertion.FollowedBy(Step.From(assertion)));

	[OverloadResolutionPriority(1)]
	public Then<Target> And(Func<Target, Task> assertion) => new(_when, _assertion.FollowedBy(Step.From(assertion)));

	public Then<Target> And(Func<Target, ValueTask> assertion) => new(_when, _assertion.FollowedBy(Step.From(assertion)));

	public Task ToTask() => _execution.Value;

	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	private async Task Execute() => await _assertion(await _when.Act());
}
