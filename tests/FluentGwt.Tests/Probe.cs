namespace FluentGwt.Tests;

internal sealed class Probe
{
	public int Acts { get; private set; }

	public void Act() => Acts++;
}
