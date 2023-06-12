using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public static class GivenExtensions
    {
        public static Given<T> Given<T>(this T target) =>
            new(target);
        
        public static Given<TTarget> Given<TTarget, TState>(this TTarget target, TState state)
        {
            if (typeof(TTarget) == typeof(TState))
                throw new InvalidOperationException(
                    $"The target of the Given cannot be replaced by adding another default {typeof(TTarget)} - consider using a named or keyed instance");
            return target.Given(StateHolder.DefaultKey, state);
        }

        public static  Given<TTarget> Given<TTarget, TState>(this TTarget target, string name, TState state) =>
            target.Given((object)name, state);

        public static  Given<TTarget> Given<TTarget, TState>(this TTarget target, object key, TState state)
        {
            var given = target.Given();
            given.AddState(key, () => state);
            return given;
        }

        public static Given<T> Given<T>(this T target, Action<T> transition) => 
            new (target, transition.AsCompletedTask());

        public static Given<T> Given<T>(this T target, Func<T, Task> transition) =>
            new(target, transition);

        public static GivenBase<TTarget> Given<TTarget, TState>(this GivenBase<TTarget> given, TState state)  =>
            given.Given(StateHolder.DefaultKey, state);

        public static GivenBase<TTarget> Given<TTarget, TState>(this GivenBase<TTarget> given, string name, TState state) =>
            given.Given((object)name, state);

        public static GivenBase<TTarget> Given<TTarget, TState>(this GivenBase<TTarget> given, object key, TState state)
        {
            given.AddState(key, () => state);
            return given;
        }

        public static Given<T> Given<T>(this Given<T> given, Action<T> transition) => 
            given.Given(transition.AsCompletedTask());

        public static Given<T> Given<T>(this Given<T> given, Func<T, Task> transition)
        {
            given.AddTransition(transition);
            return given;
        }
        //
        // public static Given<T> Given<T>(this Given<T> given, Action<T> state)
        // {
        //      given.AddTransition(state.AsCompletedTask());
        //      return given;
        // }
        //
        // public static Given Given<T>(this Given given, Action<T> state) =>
        //     given.Given(StateHolder.DefaultKey, state);
        //
        // public static Given Given<T>(this Given given, string name, Action<T> state) =>
        //     given.Given((object) name, state);
        //
        // public static Given Given<T>(this Given given, object key, Action<T> state)
        // {
        //     given.AddTransition(async x => { state(await x.GetState<T>(key)); });
        //     return given;
        // }
        //
        // public static Given Given<T>(this Given given, T state) =>
        //     given.Given(StateHolder.DefaultKey, state);
        //
        // public static Given Given(this Given given, Action state)
        // {
        //     given.AddTransition(_ => state.AsCompletedTask());
        //     return given;
        // }
        //
        // public static Given Given<T>(this Given given, string name, T state) =>
        //     given.Given((object) name, state);
        //
        // public static Given Given<T>(this Given given, object key, T state)
        // {
        //     given.AddTransition(key, _ => Task.FromResult(state));
        //     return given;
        // }
    }
}