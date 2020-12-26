using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Koek
{
    /// <summary>
    /// A collection where entries expire some time after they are added.
    /// </summary>
    /// <remarks>
    /// This collection is not thread-safe.
    /// 
    /// For ease of testability and memory management the expiration must be explicitly triggered
    /// by calling RemoveOlderThan(). Without this call, items never expire.
    /// </remarks>
    public sealed class ExpiringCollection<T> : ICollection<T>
    {
        public ExpiringCollection(Func<IStopwatch> stopwatchFactory)
        {
            _stopwatchFactory = stopwatchFactory;
        }

        private readonly Func<IStopwatch> _stopwatchFactory;

        private readonly List<(T item, IStopwatch age)> _records = new List<(T item, IStopwatch age)>();

        /// <summary>
        /// Removes and returns items that have an age older than the provided timespan.
        /// </summary>
        public IEnumerable<T> RemoveOlderThan(TimeSpan age)
        {
            var removed = new List<T>();
            _records.RemoveAll(candidate =>
            {
                if (candidate.age.Elapsed <= age)
                    return false;

                removed.Add(candidate.item);

                return true;
            });

            return removed;
        }

        public void Refresh(T item)
        {
            foreach (var record in _records)
            {
                if (!Equals(record.item, item))
                    continue;

                record.age.Restart();
                return;
            }
        }

        public void Add(T item)
        {
            _records.Add((item, _stopwatchFactory()));
        }

        public int Count => _records.Count;

        public bool IsReadOnly => false;

        public void Clear() => _records.Clear();

        public bool Contains(T item) => _records.Any(r => Equals(r.item, item));

        public bool Remove(T item)
        {
            foreach (var record in _records)
            {
                if (Equals(record.item, item))
                {
                    _records.Remove(record);
                    return true;
                }
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator() => _records.Select(record => record.item).GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[i + arrayIndex] = _records[i].item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }
}
