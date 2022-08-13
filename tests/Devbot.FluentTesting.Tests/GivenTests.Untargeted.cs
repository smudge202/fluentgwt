using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        private Func<Given> UntargetedGiven =>
            () => new Given(() => _foo = new Foo());

        private Action UntargetedAssertion =>
            () => _foo.Should().NotBeNull();
        
        [Fact]
        public void CanInstantiateBlankUntargetedGiven()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new Given();
            act.Should().NotThrow();
        }

        [Fact]
        public void CanInstantiateUntargetedGivenWithDeferredState()
        {
            Action act = () => UntargetedGiven();
            act.Should().NotThrow();
        }

        [Fact]
        public void CanDeferUntargetedGivenState()
        {
            UntargetedGiven();
            _foo.Should().BeNull();
        }

        [Fact]
        public async Task CanExecuteUntargetedGivenWithDeferredState()
        {
            var given = UntargetedGiven();
            await given.Execute();
            UntargetedAssertion();
        }

        [Fact]
        public void CanAddDeferredStateToUntargetedGiven()
        {
            Action act = () => UntargetedGiven().Given(() => _foo!.Bar = ExpectedBar);
            act.Should().NotThrow();
        }

        [Fact]
        public async Task CanExecuteAddedDeferredStateToUntargetedGiven()
        {
            var given = UntargetedGiven().Given(() => _foo!.Bar = ExpectedBar);
            await given.Execute();
            _foo!.Bar.Should().Be(ExpectedBar);
        }
    }
}