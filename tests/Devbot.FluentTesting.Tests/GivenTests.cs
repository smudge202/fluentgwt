using Bogus;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        private static Randomizer Randomizer { get; } = new();
        private Randomizer Random => Randomizer;
    }
}