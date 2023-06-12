using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public static class UntargetedGivenExtensions
    {
        public static Given Given(this Given given, Action transition) =>
            given.Given(transition.AsCompletedTask);
        
        public static Given Given(this Given given, Func<Task> transition)
        {
            given.AddTransition(_ => transition());
            return given;
        }
    }
}