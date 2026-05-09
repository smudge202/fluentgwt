using System.Collections.Concurrent;

namespace FluentGwt;

public abstract record State<Target> : StateHolder
{
	protected abstract Func<Target> Subject { get; }
	private ConcurrentQueue<Func<Target, Task>> Transitions =>
		GetState<ConcurrentQueue<Func<Target, Task>>>(this);

	protected State() =>
		AddState(this, () => new ConcurrentQueue<Func<Target, Task>>());

	internal void AddTransition(Func<Target, Task> state) =>
		Transitions.Enqueue(state);

	internal Target Current => Subject();

	internal async Task<Target> Arrange()
	{
		var target = Subject();
		while (Transitions.TryDequeue(out var state))
			await state(target);
		return target;
	}
}
