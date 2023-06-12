namespace FluentGwt
{
    public static class StateHolderExtensions
    {
        public static T Get<T>(this StateHolder state) =>
            state.GetState<T>(StateHolder.DefaultKey);
        
        public static T Get<T>(this StateHolder state, string name) =>
            state.GetState<T>(name);
        
        public static T Get<T>(this StateHolder state, object key) =>
            state.GetState<T>(key);
    }
}