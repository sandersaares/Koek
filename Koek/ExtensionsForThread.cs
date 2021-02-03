using System.Threading;

namespace Koek
{
    public static class ExtensionsForThread
    {
        /// <summary>
        /// Whether the thread has ever been started.
        /// </summary>
        public static bool HasBeenStarted(this Thread thread)
        {
            return (thread.ThreadState & ThreadState.Unstarted) != ThreadState.Unstarted;
        }
    }
}
