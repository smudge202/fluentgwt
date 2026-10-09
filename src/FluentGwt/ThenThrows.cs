using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class ThenThrows<Target, Failure>
	where Failure : Exception
{
	private readonly State<Target> _given;
	private readonly Func<Task<(Target, Failure)>> _expectation;
	private readonly Func<Target, Failure, ValueTask> _assertion;
	private readonly Lazy<Task> _execution;

	internal ThenThrows(State<Target> given, Func<Task<(Target, Failure)>> expectation, Func<Target, Failure, ValueTask> assertion)
	{
		_given = given;
		_expectation = expectation;
		_assertion = assertion;
		_execution = new(Execute);
	}

	public static implicit operator Task(ThenThrows<Target, Failure> then)
	{
		ArgumentNullException.ThrowIfNull(then);
		return then.ToTask();
	}

	public ThenThrows<Target, Failure> And(Action<Target> assertion) => Followed(Step.From(assertion));

	[OverloadResolutionPriority(1)]
	public ThenThrows<Target, Failure> And(Func<Target, Task> assertion) => Followed(Step.From(assertion));

	public ThenThrows<Target, Failure> And(Func<Target, ValueTask> assertion) => Followed(Step.From(assertion));

	public Task ToTask() => _execution.Value;

	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	private ThenThrows<Target, Failure> Followed(Func<Target, ValueTask> assertion) =>
		new(_given, _expectation, _assertion.FollowedBy(Step.OnTarget<Target, Failure>(assertion)));

	private Task Execute() =>
		Teardown.Around(_given, async () =>
		{
			var (target, failure) = await _expectation();
			await _assertion(target, failure);
		});
}
