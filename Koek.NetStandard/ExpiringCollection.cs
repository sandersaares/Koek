using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Koek
{
    /// <summary>
    /// A FIFO collection that holds items for a certain duration, after which they are evicted.
    /// </summary>
    /// <remarks>
    /// Thread-safe.
    /// 
    /// Evictions typically happen inline with read operations.
    /// </remarks>
    public sealed class ExpiringCollection<T> : IEnumerable<T>
    {
        public ExpiringCollection(TimeSpan itemLifetime)
        {
            _itemLifetime = itemLifetime;
        }

        private readonly TimeSpan _itemLifetime;

        private readonly ConcurrentQueue<Entry> _entries = new();

        // Protects against concurrent pruning. Other operations are lock-free.
        private readonly object _pruneLock = new();

        private sealed class Entry
        {
            public T Value { get; }
            public IStopwatch Lifetime { get; }

            public Entry(T value, IStopwatch lifetime)
            {
                Value = value;
                Lifetime = lifetime;
            }
        }

        private void Prune()
        {
            lock (_pruneLock)
            {
                while (_entries.TryPeek(out var first) && first.Lifetime.Elapsed > _itemLifetime)
                    _entries.TryDequeue(out _);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            // Before read operation, prune expired items.
            Prune();

            return GetValuesInternal().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Direct access to the values set, exposed for testing purposes only.
        /// </summary>
        internal IEnumerable<T> GetValuesInternal()
        {
            return _entries.Select(x => x.Value);
        }

        public void Add(T item)
        {
            _entries.Enqueue(new Entry(item, _stopwatchFactory()));

            // Due to concurrently, this might not hit 100% of times but that's fine - it is just a backstop to prevent runaway allocation.
            if (_entries.Count % AddPruneThreshold == 0)
                Prune();
        }

        public void Clear()
        {
            while (_entries.TryDequeue(out _))
            {
            }
        }

        public int GetCount()
        {
            // Before read operation, prune expired items.
            Prune();

            return _entries.Count;
        }

        // Internal for testing purposes only.
        internal Func<IStopwatch> _stopwatchFactory = () => new RealStopwatch();

        /// <summary>
        /// For every multiple of this, we prune even on Add().
        /// Typical behavior is only to prune when reading but if no read operation is ever done, we still need to prune occasionally!
        /// </summary>
        internal const int AddPruneThreshold = 100;
    }
}
