using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    internal static class AsyncExtensions
    {
        public static Task AsCompletedTask(this Action state)
        {
            // ReSharper disable once ConstantConditionalAccessQualifier
            state?.Invoke();
            return Task.CompletedTask;
        }
        
        public static Func<T, Task> AsCompletedTask<T>(this Action<T> state) => x =>
        {
            // ReSharper disable once ConstantConditionalAccessQualifier
            state?.Invoke(x);
            return Task.CompletedTask;
        };
    }
}