namespace FluentGwt;

public sealed record Given<Target> : GivenBase<Target>
{
	protected override Func<Target> Subject =>
		() => GetState<Target>(DefaultKey);

	internal Given(Target target) =>
		AddState(DefaultKey, () => target);

	internal Given(Target target, Func<Target, Task> transition)
		: this(target) => AddTransition(transition);

	public Given<Target> Deferred()
	{
		DeferLast();
		return this;
	}
}
