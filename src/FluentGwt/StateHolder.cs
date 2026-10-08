using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FluentGwt;

public abstract record StateHolder
{
	internal static readonly object DefaultKey = new();

	private static class Holder<Value>
	{
		public static readonly ConditionalWeakTable<StateHolder, Lazy<ConcurrentDictionary<object, Value>>>
			Store = new();
	}

	internal void AddState<Value>(object key, Func<Value> state)
	{
		ArgumentNullException.ThrowIfNull(key);

		if (Holder<Value>.Store.TryGetValue(this, out var lazy))
			lazy.Value.AddOrUpdate(key,
				_ => state(),
				(_, _) => state());
		else
			Holder<Value>.Store.Add(this, new Lazy<ConcurrentDictionary<object, Value>>(() =>
			{
				var dictionary = new ConcurrentDictionary<object, Value>();
				dictionary.AddOrUpdate(key,
					_ => state(),
					(_, _) => state());
				return dictionary;
			}));
	}

	internal Value GetState<Value>(object key)
	{
		var dictionary = Holder<Value>.Store.TryGetValue(this, out var lazy)
			? lazy.Value
			: throw new InvalidOperationException($"State for {typeof(Value).Name} is not available");
		return dictionary.TryGetValue(key, out var state)
			? state
			: throw new InvalidOperationException($"State for {typeof(Value).Name}({key}) is not available");
	}
}
