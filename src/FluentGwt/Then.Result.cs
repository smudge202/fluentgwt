using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class Then<Target, Result>
{
	private readonly When<Target, Result> _when;
	private readonly Func<Target, Result, ValueTask> _assertion;

	internal Then(When<Target, Result> when, Func<Target, Result, ValueTask> assertion)
	{
		_when = when;
		_assertion = assertion;
	}

	public static implicit operator Task(Then<Target, Result> then)
	{
		ArgumentNullException.ThrowIfNull(then);
		return then.ToTask();
	}

	public Task ToTask() => Execute();

	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	private async Task Execute()
	{
		var (target, result) = await _when.Act();
		await _assertion(target, result);
	}
}
