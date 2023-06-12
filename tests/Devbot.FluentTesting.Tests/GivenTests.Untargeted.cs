using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        [Fact]
        public void UntargetedGivenCanInstantiateBlank() =>
            // ReSharper disable once ObjectCreationAsStatement
            new Given();

        [Fact]
        public void UntargetedGivenCanInstantiateWithState() =>
            Given.With(this);

        [Fact]
        public void UntargetedGivenCanInstantiateWithNamedState() => 
            Given.With(Random.String2(10), this);

        [Fact]
        public void UntargetedGivenCanInstantiateWithKeyedState() => 
            Given.With(new object(), this);


        [Fact]
        public void UntargetedGivenCanInstantiateWithTransition() => 
            Given.With(() => _foo = new Foo());

        [Fact]
        public void UntargetedGivenCanInstantiateWithAsyncTransition() =>
            Given.With(async () => _foo = await AsyncFoo());

        [Fact]
        public void UntargetedGivenCanReturnState() =>
            Given.With(this)
                .Get<GivenTests>()
                .Should().Be(this);

        [Fact]
        public void UntargetedGivenCanUpdateState()
        {
            var first = new Foo();
            var second = new Foo();
            
            Given.With(first)
                .Given(second)
                .Get<Foo>()
                .Should().Be(second);
        }
        
        [Fact]
        public void UntargetedGivenThrowsWhenNameIsNull()
        {
            string? name = null;
        
            Action act = () => Given.With(name!, this);
        
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void UntargetedGivenCanReturnNamedState()
        {
            var name = Random.String2(10);
            
            Given.With(name, this)
                .Get<GivenTests>(name)
                .Should().Be(this);
        }

        [Fact]
        public void UntargetedGivenCanUpdateNamedState()
        {
            var name = Random.String2(10);
            var first = new Foo();
            var second = new Foo();
            
            Given.With(name, first)
                .Given(name, second)
                .Get<Foo>(name)
                .Should().Be(second);
        }
        
        [Fact]
        public void UntargetedGivenThrowsWhenKeyIsNull()
        {
            object? key = null;
        
            Action act = () => Given.With(key!, this);
        
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void UntargetedGivenCanReturnKeyedState()
        {
            var key = new object();
            
            Given.With(key, this)
                .Get<GivenTests>(key)
                .Should().Be(this);
        }

        [Fact]
        public void UntargetedGivenCanUpdateKeyedState()
        {
            var key = new object();
            var first = new Foo();
            var second = new Foo();
            
            Given.With(key, first)
                .Given(key, second)
                .Get<Foo>(key)
                .Should().Be(second);
        }

        [Fact]
        public async Task UntargetedGivenCanExecute() =>
            await new Given().Execute();
        
        [Fact]
        public void UntargetedGivenCanDeferTransition()
        {
            Given.With(() => _foo = new Foo());
            _foo.Should().BeNull();
        }

        [Fact]
        public void UntargetedGivenCanDeferAsyncTransition()
        {
            Given.With(async () => _foo = await AsyncFoo());
            _foo.Should().BeNull();
        }
        
        [Fact]
        public async Task UntargetedGivenWithTransitionCanExecute()
        {
            var given = Given.With(() => _foo = new Foo());
            await given.Execute();
            _foo.Should().NotBeNull();
        }
        
        [Fact]
        public void UntargetedGivenWithTransitionCanBeExtended() =>
            Given.With(() => _foo = new Foo())
                .Given(() => _foo!.Bar = Random.Int());
        
        [Fact]
        public void UntargetedGivenWithAsyncTransitionCanBeExtended() =>
            Given.With(async () => _foo = await AsyncFoo())
                .Given(() => _foo!.Bar = Random.Int());
        
        [Fact]
        public void UntargetedGivenWithTransitionCanBeAsyncExtended() =>
            Given.With(() => _foo = new Foo())
                .Given(async () => _foo!.Bar = await Task.FromResult(Random.Int()));
        
        [Fact]
        public void UntargetedGivenWithAsyncTransitionCanBeAsyncExtended() =>
            Given.With(async () => _foo = await AsyncFoo())
                .Given(async () => _foo!.Bar = await Task.FromResult(Random.Int()));
        
        [Fact]
        public async Task UntargetedGivenWithExtendedTransitionCanExecute()
        {
            var bar = Random.Int();
            await Given.With(() => _foo = new Foo())
                .Given(() => _foo!.Bar = bar)
                .Execute();
            _foo!.Bar.Should().Be(bar);
        }
        
        [Fact]
        public async Task UntargetedGivenWithExtendedAsyncTransitionCanExecute()
        {
            var bar = Random.Int();
            await Given.With(async () => _foo = await AsyncFoo())
                .Given(async () => _foo!.Bar = await Task.FromResult(bar))
                .Execute();
            _foo!.Bar.Should().Be(bar);
        }
    }
}