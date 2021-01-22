using System.Threading.Tasks;

namespace FluentGwt
{
    public static class StateHolderExtensions
    {
        public static async Task<T> Get<T>(this StateHolder state) =>
            await state.GetState<T>(StateHolder.DefaultKey);
    }
}