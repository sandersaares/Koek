using Koek;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public sealed class MovingAverageOverTimeTests
    {
        private static readonly TimeSpan WindowSize = TimeSpan.FromSeconds(1);

        private readonly ITimeSource _timeSource = Substitute.For<ITimeSource>();
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        private IStopwatch StopwatchFactory() => new TimeSourceStopwatch(_timeSource);

        private MovingAverageOverTime CreateInstance()
        {
            var instance = new MovingAverageOverTime(WindowSize);
            instance._stopwatchFactory = StopwatchFactory;

            return instance;
        }

        public MovingAverageOverTimeTests()
        {
            _timeSource.GetCurrentTime().Returns(ci => _now);
        }

        [TestMethod]
        public void Calculate_WithNoEntries_ReturnsNull()
        {
            var instance = CreateInstance();

            Assert.IsFalse(instance.CalculateMovingAverage().HasValue);
        }

        [TestMethod]
        public void Calculate_WithSomeEntries_ReturnsExpectedResult()
        {
            var instance = CreateInstance();

            instance.Observe(10);
            Assert.AreEqual(10, instance.CalculateMovingAverage());

            instance.Observe(10);
            Assert.AreEqual(10, instance.CalculateMovingAverage());

            instance.Observe(40);
            Assert.AreEqual(20, instance.CalculateMovingAverage());
        }

        [TestMethod]
        public void Calculate_WithExpiredEntries_PrunesThemBeforeCalculating()
        {
            var instance = CreateInstance();

            instance.Observe(10);
            Assert.AreEqual(10, instance.CalculateMovingAverage());

            _now = _now.AddSeconds(0.4);

            instance.Observe(10);
            Assert.AreEqual(10, instance.CalculateMovingAverage());

            _now = _now.AddSeconds(0.4);

            instance.Observe(40);
            Assert.AreEqual(20, instance.CalculateMovingAverage());

            _now = _now.AddSeconds(0.4);

            // First one should be expired by now.
            Assert.AreEqual(25, instance.CalculateMovingAverage());
        }

        [TestMethod]
        public void Observe_WithoutCalculations_StillPrunesOccasionally()
        {
            var instance = CreateInstance();

            // We create the first N items at the same timestamp (nothing should be expired yet).
            for (var i = 0; i < MovingAverageOverTime.ObservePruneThreshold; i++)
                instance.Observe(10);

            _now = _now.AddSeconds(1.5);

            // Then we create the second N items at a new timestamp (entire first batch should be expired after this).
            for (var i = 0; i < MovingAverageOverTime.ObservePruneThreshold; i++)
                instance.Observe(20);

            // This one bypasses the pruning on calculation, so we can check what Observe() really did.
            Assert.AreEqual(20, instance.CalculateMovingAverageInternal());
        }
    }
}
