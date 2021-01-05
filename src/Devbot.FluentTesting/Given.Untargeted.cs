using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FluentGwt
{
    public sealed record Given : GivenBase<Given>
    {
        private static class Holder<T>
        {
            public static readonly ConditionalWeakTable<Given, Lazy<Task<T>>> Store = new();
        }
        
        protected override Func<Given> Target => () => this;

        internal void AddState<T>(Func<Given, Task<T>> state) =>
            Holder<T>.Store.Add(this, new Lazy<Task<T>>(async () => await state(this)));

        internal async Task<T> GetState<T>() =>
            Holder<T>.Store.TryGetValue(this, out var lazy)
                ? await lazy.Value
                : throw new InvalidOperationException($"State for {typeof(T).Name} is not available");
    }
    
}