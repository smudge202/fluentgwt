using static FluentGwt.Tests.TestHostTests;

namespace FluentGwt.Tests;

internal static class TestHostTestsFluent
{
	public static When<Fixture, Reply> WhenGetting(this Given<Fixture> given, Func<Fixture, ApplicationHost> host, string path)
		=> given.When((x, cancellationToken) => host(x).Get(path, cancellationToken));

	public static async Task<Reply> Get(this ApplicationHost host, string path, CancellationToken cancellationToken)
	{
		using var client = host.CreateClient();
		using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
		return new(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken), response.Headers);
	}

	public static When<Fixture, string[]> WhenAFreshFixtureRunsItsChain(this Given<Fixture> given, Func<Fixture, Given<Fixture>> arrange, bool failing = false)
		=> given.When(async x =>
		{
			var inner = new Fixture();
			var chain = arrange(inner).When(_ => { }).Then(_ =>
			{
				if (failing)
					throw new InvalidOperationException("Failing on purpose");
			});
			await chain.ToTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			return inner.Journal.Entries.ToArray();
		});
}
