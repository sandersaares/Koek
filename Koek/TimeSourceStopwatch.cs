using System;

namespace Koek
{
    /// <summary>
    /// A stopwatch implementation that returns differences in timestamps returned from an ITimeSource implementation.
    /// This allows stopwatch operations to be easily tied to a fake timeline in test code.
    /// </summary>
    public sealed class TimeSourceStopwatch : IStopwatch
    {
        public TimeSourceStopwatch(ITimeSource timeSource)
        {
            _timeSource = timeSource;

            Start();
        }

        private readonly ITimeSource _timeSource;

        private TimeSpan _elapsedInPreviousRuns;
        private DateTimeOffset? _startTime;

        public TimeSpan Elapsed
        {
            get
            {
                if (_startTime.HasValue)
                    return _elapsedInPreviousRuns + (_timeSource.GetCurrentTime() - _startTime.Value);
                else
                    return _elapsedInPreviousRuns;
            }
        }

        public void Reset()
        {
            _elapsedInPreviousRuns = default;
            _startTime = null;
        }

        public void Restart()
        {
            _elapsedInPreviousRuns = default;
            _startTime = _timeSource.GetCurrentTime();
        }

        public void Start()
        {
            _elapsedInPreviousRuns = Elapsed;
            _startTime = _timeSource.GetCurrentTime();
        }

        public void Stop()
        {
            _elapsedInPreviousRuns = Elapsed;
            _startTime = null;
        }

        public long GetTimestamp() => _timeSource.GetCurrentTime().Ticks;

        public long Frequency => 10000000;
    }
}
