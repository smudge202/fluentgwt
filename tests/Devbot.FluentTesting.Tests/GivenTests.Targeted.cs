using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        private object State { get; } = new();
        
        [Fact]
        public void TargetedGivenCanInstantiate() =>
            this.Given();

        [Fact]
        public void TargetGivenCanInstantiateWithState() =>
            this.Given(State);

        [Fact]
        public void TargetedGivenCanInstantiateWithNamedState() => 
            this.Given(Random.String2(10), State);

        [Fact]
        public void TargetedGivenCanInstantiateWithKeyedState() => 
            this.Given(new object(), State);

        [Fact]
        public void TargetedGivenCanInstantiateWithTransition() =>
            this.Given(x => x._foo = new Foo());

        [Fact]
        public void TargetedGivenCanInstantiateWithAsyncTransition() =>
            this.Given(async x => x._foo = await AsyncFoo());

        [Fact]
        public void TargetedGivenCanReturnTarget() =>
            this.Given()
                .Get<GivenTests>()
                .Should().Be(this);
        
        [Fact]
        public void TargetedGivenCanReturnState() =>
            this.Given(State)
                .Get<object>()
                .Should().Be(State);

        [Fact]
        public void TargetedGivenCanNotUpdateTarget()
        {
            var first = new Foo();
            var second = new Foo();

            Action act = () => first.Given(second);

            act.Should().Throw<InvalidOperationException>();
        }
        
        [Fact]
        public void TargetedGivenThrowsWhenNameIsNull()
        {
            string? name = null;
        
            Action act = () => this.Given(name!, State);
        
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void TargetedGivenCanReturnNamedState()
        {
            var name = Random.String2(10);
            
            this.Given(name, State)
                .Get<object>(name)
                .Should().Be(State);
        }

        [Fact]
        public void TargetedGivenCanUpdateNamedState()
        {
            var name = Random.String2(10);
            var first = new Foo();
            var second = new Foo();
            
            this.Given(name, first)
                .Given(name, second)
                .Get<Foo>(name)
                .Should().Be(second);
        }
        
        [Fact]
        public void TargetedGivenThrowsWhenKeyIsNull()
        {
            object? key = null;
        
            Action act = () => this.Given(key!, State);
        
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void TargetedGivenCanReturnKeyedState()
        {
            var key = new object();
            
            this.Given(key, State)
                .Get<object>(key)
                .Should().Be(State);
        }

        [Fact]
        public void TargetedGivenCanUpdateKeyedState()
        {
            var key = new object();
            var first = new Foo();
            var second = new Foo();
            
            this.Given(key, first)
                .Given(key, second)
                .Get<Foo>(key)
                .Should().Be(second);
        }

        [Fact]
        public async Task TargetedGivenCanExecute() =>
            await this.Given().Execute();
        
        [Fact]
        public void TargetedGivenCanDeferTransition()
        {
            this.Given(x => x._foo = new Foo());
            _foo.Should().BeNull();
        }

        [Fact]
        public void TargetedGivenCanDeferAsyncTransition()
        {
            this.Given(async x => x._foo = await AsyncFoo());
            _foo.Should().BeNull();
        }
        
        [Fact]
        public async Task TargetedGivenWithTransitionCanExecute()
        {
            var given = this.Given(x => x._foo = new Foo());
            await given.Execute();
            _foo.Should().NotBeNull();
        }
        
        [Fact]
        public void TargetedGivenWithTransitionCanBeExtended() =>
            this.Given(x => x._foo = new Foo())
                .Given(x => x._foo!.Bar = Random.Int());
        
        [Fact]
        public void TargetedGivenWithAsyncTransitionCanBeExtended() =>
            this.Given(async x => x._foo = await AsyncFoo())
                .Given(x => x._foo!.Bar = Random.Int());
        
        [Fact]
        public void TargetedGivenWithTransitionCanBeAsyncExtended() =>
            this.Given(x => x._foo = new Foo())
                .Given(async x => x._foo!.Bar = await Task.FromResult(Random.Int()));
        
        [Fact]
        public void TargetedGivenWithAsyncTransitionCanBeAsyncExtended() =>
            this.Given(async x => x._foo = await AsyncFoo())
                .Given(async x => x._foo!.Bar = await Task.FromResult(Random.Int()));
        
        [Fact]
        public async Task TargetedGivenWithExtendedTransitionCanExecute()
        {
            var bar = Random.Int();
            await this.Given(x => x._foo = new Foo())
                .Given(x => x._foo!.Bar = bar)
                .Execute();
            _foo!.Bar.Should().Be(bar);
        }
        
        [Fact]
        public async Task TargetedGivenWithExtendedAsyncTransitionCanExecute()
        {
            var bar = Random.Int();
            await this.Given(async x => x._foo = await AsyncFoo())
                .Given(async x => x._foo!.Bar = await Task.FromResult(bar))
                .Execute();
            _foo!.Bar.Should().Be(bar);
        }
    }
}