using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FluentGwt
{
    public static class State
    {
        public static Given Given<T>(T value) =>
            Given(StateHolder.DefaultKey, value);

        public static Given Given<T>(string name, T value) => 
            Given((object)name, value);

        public static Given Given<T>(object key, T value)
        {
            var given = new Given();
            given.AddState(key, _ => Task.FromResult(value));
            return given;
        }

        public static Given Given(Action action)
        {
            var given = new Given();
            given.AddState(_ => action.AsCompletedTask());
            return given;
        }
    }
    
    public abstract record State<T> : StateHolder
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