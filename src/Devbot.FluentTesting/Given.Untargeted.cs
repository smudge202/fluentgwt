using System;

namespace FluentGwt
{
    public sealed record Given : GivenBase<Given>
    {
        protected override Func<Given> Target => () => this;
    }
    
}