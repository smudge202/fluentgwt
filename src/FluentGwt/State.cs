using System.Collections.Concurrent;

namespace FluentGwt;

public abstract record State<T> : StateHolder
{
	protected abstract Func<T> Target { get; }
	private ConcurrentQueue<Func<T, Task>> Transitions =>
		GetState<ConcurrentQueue<Func<T, Task>>>(this);

	protected State() =>
		AddState(this, () => new ConcurrentQueue<Func<T, Task>>());

	internal void AddTransition(Func<T, Task> state) =>
		Transitions.Enqueue(state);

	internal async Task Execute()
	{
		var target = Target();
		while (Transitions.TryDequeue(out var state))
			await state(target);
	}
}
