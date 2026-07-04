using System.Diagnostics.CodeAnalysis;

namespace FluentGwt;

public sealed class FixtureRow<Value>
{
	private readonly Delegate _evaluate;
	private readonly Type _fixture;

	private FixtureRow(string label, Delegate evaluate, Type fixture)
	{
		Label = label;
		_evaluate = evaluate;
		_fixture = fixture;
	}

	public string Label { get; }

	[SuppressMessage("Design", "CA1000", Justification = "Value comes from the type and Fixture from the method, so a row is written FixtureRow<Value>.For<Fixture>(...) with neither repeated.")]
	public static FixtureRow<Value> For<Fixture>(string label, Func<Fixture, Value> evaluate)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(label);
		ArgumentNullException.ThrowIfNull(evaluate);
		return new(label, evaluate, typeof(Fixture));
	}

	public override string ToString() => Label;

	internal Value Evaluate<Target>(Target target) =>
		_evaluate is Func<Target, Value> evaluate
			? evaluate(target)
			: throw new InvalidOperationException(
				$"The row '{Label}' was written for a {_fixture.Name} fixture but was given to a chain on {typeof(Target).Name}.");
}
