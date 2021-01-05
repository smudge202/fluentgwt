using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    internal static class AsyncExtensions
    {
        public static Func<T, Task> AsAsync<T>(this Action<T> state) => x =>
        {
            state(x);
            return Task.CompletedTask;
        };

        public static void AddState(this GivenBase<Given> given, Action<Given> state) =>
            given.AddState(state.AsAsync());
    }
}