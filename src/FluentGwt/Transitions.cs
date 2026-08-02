namespace FluentGwt;

internal sealed class Transitions<Target>
{
	private readonly Lock _lock = new();
	private readonly List<(Func<Target, Task> Run, bool Deferred)> _transitions = [];

	public void Add(Func<Target, Task> transition)
	{
		lock (_lock)
			_transitions.Add((transition, false));
	}

	public void DeferLast()
	{
		lock (_lock)
		{
			if (_transitions.Count == 0)
				throw new InvalidOperationException("Deferred marks the given before it, and this chain has no given to defer yet.");
			_transitions[^1] = _transitions[^1] with { Deferred = true };
		}
	}

	public (IReadOnlyList<Func<Target, Task>> Immediate, IReadOnlyList<Func<Target, Task>> Deferred) Take()
	{
		lock (_lock)
		{
			var immediate = _transitions.Where(x => !x.Deferred).Select(x => x.Run).ToList();
			var deferred = _transitions.Where(x => x.Deferred).Select(x => x.Run).ToList();
			_transitions.Clear();
			return (immediate, deferred);
		}
	}
}
