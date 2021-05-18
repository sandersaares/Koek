using System.Threading;

namespace Koek
{
    public sealed class RealSleeper : ISleeper
    {
        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
