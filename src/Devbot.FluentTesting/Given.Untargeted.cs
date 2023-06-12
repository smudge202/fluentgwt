using System;
using System.Threading.Tasks;

namespace FluentGwt
{
    public sealed record Given : GivenBase<Given>
    {
        protected override Func<Given> Target => 
            () => this;

        public Given() { }
        
        
        public static GivenBase<Given> With<T>(T state) =>
            new Given().Given(state);
        public static GivenBase<Given> With<T>(string name, T state) => 
            new Given().Given(name, state);
        
        public static GivenBase<Given> With<T>(object key, T state) => 
            new Given().Given(key, state);

        public static Given With(Action transition) => 
            new Given().Given(transition);

        public static Given With(Func<Task> transition) =>
            new Given().Given(transition);
    }
}