namespace FluentGwt;

public abstract record State<Target> : StateHolder
{
	protected abstract Func<Target> Subject { get; }
	private Transitions<Target> Transitions =>
		GetState<Transitions<Target>>(this);

	protected State() =>
		AddState(this, () => new Transitions<Target>());

	internal Target Current => Subject();

	internal void AddTransition(Func<Target, Task> transition) =>
		Transitions.Add(transition);

	internal void DeferLast() =>
		Transitions.DeferLast();

	internal async Task<Target> Arrange()
	{
		var target = Subject();
		foreach (var transition in Transitions.TakeInRunOrder())
			await transition(target);
		if (target is ServiceFixture fixture)
		{
			fixture.ChooseSeedNow();
			await fixture.StartHostedServices();
		}
		return target;
	}
}
