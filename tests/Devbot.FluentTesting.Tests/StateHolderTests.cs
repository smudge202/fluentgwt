using System;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    public class StateHolderTests
    {
        private static Randomizer Random { get; } = new();

        [Fact]
        public async Task CanInstantiateWithStateObject() =>
            await State.Given(this)
                .Given<StateHolderTests>(x => x.Should().Be(this))
                .Execute();

        [Fact]
        public async Task CanInstantiateWithNamedStateObject()
        {
            var name = Random.String2(10);
            
            await State.Given(name, this)
                .Given<StateHolderTests>(name, x => x.Should().Be(this))
                .Execute();
        }

        [Fact]
        public async Task CanInstantiateWithKeyedStateObject()
        {
            var key = new object();
            
            await State.Given(key, this)
                .Given<StateHolderTests>(key, x => x.Should().Be(this))
                .Execute();
        }

        [Fact]
        public async Task ThrowsArgumentNullExceptionWhenNameIsNull()
        {
            string? name = null;
            
            Func<Task> act = async () => await State.Given(name!, this)
                .Get<StateHolderTests>();

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ThrowsArgumentNullExceptionWhenKeyIsNull()
        {
            object? key = null;
            
            Func<Task> act = async () => await State.Given(key!, this)
                .Get<StateHolderTests>();

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CanReplaceStateObject()
        {
            var initial = new object();
            var replacement = new object();
            
            await State.Given(initial)
                .Given(replacement)
                .Given<object>(x => x.Should().Be(replacement))
                .Execute();
        }

        [Fact]
        public async Task CanReplaceNamedStateObject()
        {
            var name = Random.String2(10);
            var initial = new object();
            var replacement = new object();
            
            await State.Given(name, initial)
                .Given(name, replacement)
                .Given<object>(name, x => x.Should().Be(replacement))
                .Execute();
        }

        [Fact]
        public async Task CanReplaceKeyedStateObject()
        {
            var key = new object();
            var initial = new object();
            var replacement = new object();
            
            await State.Given(key, initial)
                .Given(key, replacement)
                .Given<object>(key, x => x.Should().Be(replacement))
                .Execute();
        }
    }
}