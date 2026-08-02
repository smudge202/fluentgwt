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
		var fixture = target as ServiceFixture;
		var (immediate, deferred) = Transitions.Take();
		foreach (var transition in immediate)
			await transition(target);
		if (fixture is not null)
			await fixture.StartHosts();
		foreach (var transition in deferred)
			await transition(target);
		if (fixture is not null)
		{
			fixture.ChooseSeedNow();
			await fixture.StartHostedServices();
		}
		return target;
	}
}
