using AwesomeAssertions;
using Xunit;

namespace FluentGwt.Tests;

public sealed partial class TestHostTests
{
	private static string Output => TestContext.Current.TestOutputHelper!.Output;

	[Fact]
	public Task WhenHostLogsWarningThenItAppearsInTestOutputPrefixedWithHostName()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/warn")
			.Then(_ => Output.Should().Contain("[Program] [Warning] Web: Something odd in web"));

	[Fact]
	public Task WhenComposedHostLogsWarningThenItAppearsPrefixedWithItsName()
		=> Context
			.Given()
			.WhenGetting(x => x.Composed, "/warn")
			.Then(_ => Output.Should().Contain("[composed] [Warning] Web: Something odd in web"));

	[Fact]
	public Task WhenHostLogsThenTheRecordIsAvailableForAssertion()
		=> Context
			.Given()
			.WhenGetting(x => x.Api, "/warn")
			.Then((x, _) => x.Logs.GetSnapshot().Should().Contain(r => r.Message == "Something odd in web"));
}
