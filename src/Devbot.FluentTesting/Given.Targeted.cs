using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public sealed record Given<T> : GivenBase<T>
    {
        protected override Func<T> Target => 
            () => GetState<T>(DefaultKey);

        internal Given(T target) =>
            AddState(DefaultKey, () => target);

        internal Given(T target, Func<T, Task> transition)
            : this(target) => AddTransition(transition);
    }
}