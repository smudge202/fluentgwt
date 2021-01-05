using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FluentGwt
{
    public sealed record State
    {
    }
    
    public abstract record State<T>
    {
        protected abstract Func<T> Target { get; }
        private ConcurrentQueue<Func<T, Task>> States { get; } = new();

        internal void AddState(Func<T, Task> state) =>
            States.Enqueue(state);
        
        internal async Task Execute()
        {
            var target = Target();
            while (States.TryDequeue(out var state))
                await state(target);
        }
    }
}