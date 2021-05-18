using System;

namespace Koek
{
    /// <summary>
    /// Enables replacing Stopwatch instances with time-shifted ones in tests.
    /// The stopwatch is automatically started on creation.
    /// </summary>
    public interface IStopwatch
    {
        TimeSpan Elapsed { get; }

        long GetTimestamp();
        long Frequency { get; }

        void Reset();
        void Restart();
        void Start();
        void Stop();
    }
}
