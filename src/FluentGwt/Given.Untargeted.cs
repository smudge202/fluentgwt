namespace FluentGwt;

public sealed record Given : GivenBase<Given>
{
	protected override Func<Given> Subject =>
		() => this;

	public Given() { }


	public static GivenBase<Given> With<Value>(Value state) =>
		new Given().Given(state);
	public static GivenBase<Given> With<Value>(string name, Value state) =>
		new Given().Given(name, state);

	public static GivenBase<Given> With<Value>(object key, Value state) =>
		new Given().Given(key, state);

	public static Given With(Action transition) =>
		new Given().Given(transition);

	public static Given With(Func<Task> transition) =>
		new Given().Given(transition);
}
