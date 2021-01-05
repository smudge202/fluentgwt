using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public static class GivenExtensions
    {
        public static Given<T> Given<T>(this T target) =>
            new(target);
        
        public static Given<T> Given<T>(this T target, Action<T> state) => 
            new (target, state.AsAsync());

        public static Given<T> Given<T>(this Given<T> given, Action<T> state)
        {
             given.AddState(state.AsAsync());
             return given;
        }
        
        public static Given Given<T>(this State _, T state)
        {
            var given = new Given();
            given.AddState(_ => Task.FromResult(state));
            return given;
        }

        public static void Given<T>(this Given given, Action<T> state) =>
            given.AddState(async x =>
            {
                state(await x.GetState<T>());
            });
    }
}