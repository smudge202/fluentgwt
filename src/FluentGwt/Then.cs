using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class Then<Target>
{
	private readonly When<Target> _when;
	private readonly Func<Target, ValueTask> _assertion;

	internal Then(When<Target> when, Func<Target, ValueTask> assertion)
	{
		_when = when;
		_assertion = assertion;
	}

	public static implicit operator Task(Then<Target> then)
	{
		ArgumentNullException.ThrowIfNull(then);
		return then.ToTask();
	}

	public Task ToTask() => Execute();

	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	private async Task Execute() => await _assertion(await _when.Act());
}
