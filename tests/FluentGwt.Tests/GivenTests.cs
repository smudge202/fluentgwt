using System.Threading.Tasks;
using Bogus;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        private static Randomizer Randomizer { get; } = new();
        private Randomizer Random => Randomizer;
        
        private Foo? _foo;

        private static Task<Foo> AsyncFoo() => Task.FromResult(new Foo());

        private sealed class Foo
        {
            public int? Bar { get; set; }
            public int? Baz { get; set; }
        }
    }
}