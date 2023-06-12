using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace FluentGwt
{
    public abstract record StateHolder
    {
        internal static readonly object DefaultKey = new();

        private static class Holder<T>
        {
            public static readonly ConditionalWeakTable<StateHolder, Lazy<ConcurrentDictionary<object, T>>>
                Store = new();
        }

        // TODO: try to provide access to derived type
        internal void AddState<T>(object key, Func<T> state)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            
            if (Holder<T>.Store.TryGetValue(this, out var lazy))
                lazy.Value.AddOrUpdate(key, 
                    _ => state(),
                    (_, _) => state());
            else
                Holder<T>.Store.Add(this, new Lazy<ConcurrentDictionary<object, T>>(() =>
                {
                    // TODO: consider something more lightweight
                    var dictionary = new ConcurrentDictionary<object, T>();
                    dictionary.AddOrUpdate(key, 
                        _ => state(),
                        (_, _) => state());
                    return dictionary;
                }));
        }

        internal T GetState<T>(object key)
        {
            var dictionary = Holder<T>.Store.TryGetValue(this, out var lazy)
                ? lazy.Value
                : throw new InvalidOperationException($"State for {typeof(T).Name} is not available");
            return dictionary.TryGetValue(key, out var state)
                ? state
                : throw new InvalidOperationException($"State for {typeof(T).Name}({key}) is not available");
        }
    }
}