using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Koek
{
    /// <summary>
    /// A delayer implementation that just forwards to Task.Delay() for production use.
    /// </summary>
    public sealed class RealDelayer : IDelayer
    {
        [DebuggerStepThrough]
        public Task Delay(TimeSpan duration) => Task.Delay(duration);

        [DebuggerStepThrough]
        public Task Delay(TimeSpan duration, CancellationToken cancel) => Task.Delay(duration, cancel);
    }
}
