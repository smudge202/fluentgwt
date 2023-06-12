using System.Reflection;
using System.Threading.Tasks;

namespace FluentGwt.Tests
{
    // Internals intentionally not made visible to tests
    // to ensure only public scope utilised
    internal static class InternalExtensions
    {
        public static async Task Execute<T>(this State<T> state) =>
            await (Task)typeof(State<T>)
                .GetMethod(nameof(Execute), BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(state, null)!;
    }
}