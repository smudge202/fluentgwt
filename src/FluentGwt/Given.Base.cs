namespace FluentGwt;

public abstract record GivenBase<T> : State<T>
{
	public When<T> When(Action<T> act) => new(this, act);
}
