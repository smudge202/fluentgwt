using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public static class GivenExtensions
    {
        public static Given<T> Given<T>(this T target) =>
            new(target);
        
        public static Given<T> Given<T>(this T target, Action<T> state) => 
            new (target, state.AsCompletedTask());

        public static Given<T> Given<T>(this Given<T> given, Action<T> state)
        {
             given.AddState(state.AsCompletedTask());
             return given;
        }

        public static Given Given<T>(this Given given, Action<T> state) =>
            given.Given(StateHolder.DefaultKey, state);

        public static Given Given<T>(this Given given, string name, Action<T> state) =>
            given.Given((object) name, state);
        
        public static Given Given<T>(this Given given, object key, Action<T> state)
        {
            given.AddState(async x => { state(await x.GetState<T>(key)); });
            return given;
        }

        public static Given Given<T>(this Given given, T state) =>
            given.Given(StateHolder.DefaultKey, state);

        public static Given Given(this Given given, Action state)
        {
            given.AddState(_ => state.AsCompletedTask());
            return given;
        }

        public static Given Given<T>(this Given given, string name, T state) =>
            given.Given((object) name, state);
        
        public static Given Given<T>(this Given given, object key, T state)
        {
            given.AddState(key, _ => Task.FromResult(state));
            return given;
        }
    }
}