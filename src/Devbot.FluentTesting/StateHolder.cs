using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FluentGwt
{
    public abstract record StateHolder
    {
        internal static readonly object DefaultKey = new();

        private static class Holder<T>
        {
            public static readonly ConditionalWeakTable<StateHolder, Lazy<ConcurrentDictionary<object, Task<T>>>>
                Store = new();
        }

        // TODO: try to provide access to derived type
        internal void AddState<T>(object key, Func<StateHolder, Task<T>> state)
        {
            if (Holder<T>.Store.TryGetValue(this, out var lazy))
                lazy.Value.AddOrUpdate(key, async _ => await state(this),
                    async (_, _) => await state(this));
            else
                Holder<T>.Store.Add(this, new Lazy<ConcurrentDictionary<object, Task<T>>>(() =>
                {
                    // TODO: consider something more lightweight
                    var dictionary = new ConcurrentDictionary<object, Task<T>>();
                    dictionary.AddOrUpdate(key, async _ => await state(this),
                        async (_, _) => await state(this));
                    return dictionary;
                }));
        }

        internal async Task<T> GetState<T>(object key)
        {
            var dictionary = Holder<T>.Store.TryGetValue(this, out var lazy)
                ? lazy.Value
                : throw new InvalidOperationException($"State for {typeof(T).Name} is not available");
            return dictionary.TryGetValue(key, out var state)
                ? await state
                : throw new InvalidOperationException($"State for {typeof(T).Name}({key}) is not available");
        }
    }
}