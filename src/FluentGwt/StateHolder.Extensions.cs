namespace FluentGwt;

public static class StateHolderExtensions
{
	public static T Get<T>(this StateHolder state)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<T>(StateHolder.DefaultKey);
	}

	public static T Get<T>(this StateHolder state, string name)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<T>(name);
	}

	public static T Get<T>(this StateHolder state, object key)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<T>(key);
	}
}
