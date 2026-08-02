namespace FluentGwt;

public interface FixtureHost : IAsyncDisposable
{
	ValueTask Start(CancellationToken cancellationToken);
}
