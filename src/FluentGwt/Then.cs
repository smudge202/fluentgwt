using System.Runtime.CompilerServices;

namespace FluentGwt;

public sealed class Then<Target>(When<Target> when, Action<Target> assertion)
{
	public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

	private async Task Execute() => assertion(await when.Act());
}
