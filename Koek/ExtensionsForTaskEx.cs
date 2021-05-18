using System.Threading.Tasks;

namespace Koek
{
    public static class ExtensionsForTaskEx
    {
        public static async ValueTask IgnoreExceptionsAsync(this ValueTask t)
        {
            try
            {
                await t;
            }
            catch
            {
            }
        }

        public static async ValueTask IgnoreExceptionsAsync<T>(this ValueTask<T> t)
        {
            try
            {
                await t;
            }
            catch
            {
            }
        }
    }
}
