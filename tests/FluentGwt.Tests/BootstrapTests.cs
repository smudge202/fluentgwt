using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed class BootstrapTests
{
	[Fact]
	public async Task WhenThenIsAwaitedThenAssertionRuns()
	{
		var asserted = false;

		await new Probe()
			.Given()
			.When(x => x.Act())
			.Then(_ => asserted = true);

		asserted.Should().BeTrue();
	}

	[Fact]
	public async Task WhenAssertionFailsThenAwaitingTheChainThrowsIt()
	{
		var failure = new InvalidOperationException();

		var act = async () => await new Probe()
			.Given()
			.When(x => x.Act())
			.Then(_ => Fail(failure));

		(await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
	}

	private static void Fail(Exception failure) => throw failure;
}
