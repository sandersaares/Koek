using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Koek
{
    /// <summary>
    /// Easily calculate a moving average over time.
    /// </summary>
    public sealed class MovingAverageOverTime
    {
        public MovingAverageOverTime(TimeSpan windowSize)
        {
            _windowSize = windowSize;
        }

        private readonly TimeSpan _windowSize;

        private readonly ConcurrentQueue<Entry> _entries = new();

        // Protects against concurrent pruning. Other operations are lock-free.
        private readonly object _pruneLock = new();

        private sealed record Entry(double Value, IStopwatch Lifetime);

        /// <summary>
        /// Gets the moving average.
        /// 
        /// Returns null if there are no entries in the window.
        /// </summary>
        public double? CalculateMovingAverage()
        {
            // Before reading, prune any old items.
            Prune();

            return CalculateMovingAverageInternal();
        }

        /// <summary>
        /// Purely does the moving average calculation, without the pruning logic.
        /// Internal for testing purposes only.
        /// </summary>
        internal double? CalculateMovingAverageInternal()
        {
            return _entries.DefaultIfEmpty().Select(x => x?.Value).Average();
        }

        public void Observe(double value)
        {
            // It makes little sense to calculate a moving average using NaN or Infinity so throw when given them.
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Calculating a moving average requires an actual number, not NaN or Infinity.");

            _entries.Enqueue(new Entry(value, _stopwatchFactory()));

            // Due to concurrently, this might not hit 100% of times but that's fine - it is just a backstop to prevent runaway allocation.
            if (_entries.Count % ObservePruneThreshold == 0)
                Prune();
        }

        private void Prune()
        {
            lock (_pruneLock)
            {
                while (_entries.TryPeek(out var first) && first.Lifetime.Elapsed > _windowSize)
                    _entries.TryDequeue(out _);
            }
        }

        // Internal for testing purposes only.
        internal Func<IStopwatch> _stopwatchFactory = () => new RealStopwatch();

        /// <summary>
        /// For every multiple of this, we prune even on Observe().
        /// Typical behavior is only to prune when calculating but if no calculation is ever done, we still need to prune occasionally!
        /// </summary>
        internal const int ObservePruneThreshold = 100;
    }
}
