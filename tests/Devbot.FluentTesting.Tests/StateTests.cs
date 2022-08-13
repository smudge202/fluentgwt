using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    // ReSharper disable once UnusedType.Global
    public class StateTests
    {
        [Fact]
        public async Task CanExecuteState()
        {
            var executed = false;
            var given = State.Given(() => executed = true);
            await given.Execute();
            executed.Should().BeTrue();
        }
    }
}