using System.Collections.ObjectModel;

namespace FluentGwt.Tests.Integrated;

public sealed class IntegratedFixture : ServiceFixture
{
	public Collection<string> Log { get; } = [];
}
