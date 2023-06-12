using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    internal static class AsyncExtensions
    {
        public static Task AsCompletedTask(this Action? transition)
        {
            transition?.Invoke();
            return Task.CompletedTask;
        }
        
        public static Func<T, Task> AsCompletedTask<T>(this Action<T>? transition) => x =>
        {
            transition?.Invoke(x);
            return Task.CompletedTask;
        };
    }
}