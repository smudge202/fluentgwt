using System;
using FluentAssertions;
using Xunit;

namespace FluentGwt.Tests
{
    public partial class GivenTests
    {
        [Fact]
        public void CanInstantiateBlankGiven()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new Given();
            act.Should().NotThrow();
        }
        
        //new Order().Given(x => x.OrderLines.Add(new OrderLine())).Given(somethingElse);
        //new Order().Given(new Foo()).When<GivenTests>(x => x.Order = foo);
        // context:
        // order: = ^ new Order()
        // GivenTests = ^ this 
        // GivenTests = ^ new Foo()
            
        // await new State()
        //     .Given(this)
        //     .Given<GivenTests>(x => x.ExpectedFoo = Randomizer.Int())
        //     .When<GivenTests>(x => x.DoAThing())
        //     .Then<GivenTests>(x => x.Foo.Should().Be(ExpectedFoo));
    }
}