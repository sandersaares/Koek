using System;
using System.Diagnostics;

namespace Koek
{
    /// <summary>
    /// An IStopwatch implementation that simply wraps a Stopwatch instance.
    /// </summary>
    public sealed class RealStopwatch : IStopwatch
    {
        private readonly Stopwatch _wrapped = Stopwatch.StartNew();

        public TimeSpan Elapsed => _wrapped.Elapsed;

        [DebuggerStepThrough]
        public void Reset() => _wrapped.Reset();

        [DebuggerStepThrough]
        public void Restart() => _wrapped.Restart();

        [DebuggerStepThrough]
        public void Start() => _wrapped.Start();

        [DebuggerStepThrough]
        public void Stop() => _wrapped.Stop();

        public long Frequency => Stopwatch.Frequency;

        [DebuggerStepThrough]
        public long GetTimestamp() => Stopwatch.GetTimestamp();
    }
}
