namespace FluentGwt;

public static class StateHolderExtensions
{
	public static Value Get<Value>(this StateHolder state)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<Value>(StateHolder.DefaultKey);
	}

	public static Value Get<Value>(this StateHolder state, string name)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<Value>(name);
	}

	public static Value Get<Value>(this StateHolder state, object key)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state.GetState<Value>(key);
	}
}
