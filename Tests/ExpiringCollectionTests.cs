using Koek;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq;

namespace Tests
{
    [TestClass]
    public sealed class ExpiringCollectionTests
    {
        private static readonly TimeSpan WindowSize = TimeSpan.FromSeconds(1);

        private readonly ITimeSource _timeSource = Substitute.For<ITimeSource>();
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        private IStopwatch StopwatchFactory() => new TimeSourceStopwatch(_timeSource);

        private ExpiringCollection<double> CreateInstance()
        {
            var instance = new ExpiringCollection<double>(WindowSize);
            instance._stopwatchFactory = StopwatchFactory;

            return instance;
        }

        public ExpiringCollectionTests()
        {
            _timeSource.GetCurrentTime().Returns(ci => _now);
        }

        private double CalculateAverage(ExpiringCollection<double> collection)
        {
            return collection.DefaultIfEmpty().Average();
        }

        [TestMethod]
        public void Enumerator_WithExpiredEntries_PrunesThemBeforeEnumerating()
        {
            var instance = CreateInstance();

            instance.Add(10);
            Assert.AreEqual(10, CalculateAverage(instance));

            _now = _now.AddSeconds(0.4);

            instance.Add(10);
            Assert.AreEqual(10, CalculateAverage(instance));

            _now = _now.AddSeconds(0.4);

            instance.Add(40);
            Assert.AreEqual(20, CalculateAverage(instance));

            _now = _now.AddSeconds(0.4);

            // First one should be expired by now.
            Assert.AreEqual(25, CalculateAverage(instance));
        }

        [TestMethod]
        public void Observe_WithoutCalculations_StillPrunesOccasionally()
        {
            var instance = CreateInstance();

            // We create the first N items at the same timestamp (nothing should be expired yet).
            for (var i = 0; i < ExpiringCollection<double>.AddPruneThreshold; i++)
                instance.Add(10);

            _now = _now.AddSeconds(1.5);

            // Then we create the second N items at a new timestamp (entire first batch should be expired after this).
            for (var i = 0; i < ExpiringCollection<double>.AddPruneThreshold; i++)
                instance.Add(20);

            // This one bypasses the pruning on calculation, so we can check what Observe() really did.
            Assert.AreEqual(20, CalculateAverage(instance));
        }
    }
}
