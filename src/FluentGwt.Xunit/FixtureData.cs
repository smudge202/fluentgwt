using Xunit;

namespace FluentGwt;

public sealed class FixtureData<Fixture, Value> : TheoryData<FixtureRow<Value>>
{
	public void Add(string label, Func<Fixture, Value> row) => Add(FixtureRow<Value>.For(label, row));
}
