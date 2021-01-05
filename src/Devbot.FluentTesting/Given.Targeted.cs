using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public record Given<T> : GivenBase<T>
    {
        protected override Func<T> Target { get; }
        
        public Given(T target) => 
            Target = () => target;

        public Given(T target, Func<T, Task> state)
            : this(target) => AddState(state);
    }
}