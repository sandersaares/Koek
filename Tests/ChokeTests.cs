using Koek;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

namespace Tests
{
    [TestClass]
    public sealed class ChokeTests
    {
        private readonly ITimeSource _timeSource = Substitute.For<ITimeSource>();

        [TestMethod]
        public void Choke_SatisfiesRequestsWheNotDepleted()
        {
            var startTime = DateTimeOffset.UtcNow;
            _timeSource.GetCurrentTime().Returns(startTime);

            var choke = new Choke(DataRate.FromBytesPerSecond(10000), new TimeSourceStopwatch(_timeSource));

            // The test is ignorant of the internal buffers but this must surely deplete it.
            for (var i = 0; i < 100; i++)
                choke.RequestBytes(100);

            Assert.IsFalse(choke.RequestBytes(100));

            // After a second there should be some capacity available gain.
            _timeSource.GetCurrentTime().Returns(startTime.AddSeconds(1));

            Assert.IsTrue(choke.RequestBytes(100));
        }

        [DataRow(5_000_000 / 8, 5, 1, 1500)]
        [DataRow(5_000_000 / 8, 5, 0.1, 1500)]
        // We choose an annoying speed so that calculations would be as difficult as possible.
        // Still divisible by 8 because we are not monsters, of course.
        [DataRow(1_866_666 / 8, 5, 0.1, 1500)]
        [DataRow(1_866_666 / 8, 5, 1, 1500)]
        [DataTestMethod]
        public void Choke_ProvidesExpectedRate(int speedBytesPerSecond, int testDurationSeconds, double stepSizeMs, int requestSize)
        {
            // Choke algorithm deliberately rounds speeds down when fractions are encountered, to avoid overspeed.
            const int tolerancePackets = 3;

            var startTime = DateTimeOffset.UtcNow;
            var currentTime = startTime;
            _timeSource.GetCurrentTime().Returns(ci => currentTime);

            var bytesAllowed = 0;

            var choke = new Choke(DataRate.FromBytesPerSecond(speedBytesPerSecond), new TimeSourceStopwatch(_timeSource));

            for (double i = 0; i < testDurationSeconds * 1000; i += stepSizeMs)
            {
                // Every N milliseconds we request more data.
                while (choke.RequestBytes((ushort)requestSize))
                    bytesAllowed += requestSize;

                currentTime = startTime.AddMilliseconds(i);
            }

            Assert.AreEqual(speedBytesPerSecond * testDurationSeconds + choke.BucketSizeBytes, bytesAllowed, delta: requestSize * tolerancePackets);
        }

        [TestMethod]
        public void Choke_WithHugeRequest_ThrowsException()
        {
            var choke = new Choke(DataRate.FromBytesPerSecond(1234), new TimeSourceStopwatch(_timeSource));

            Assert.ThrowsException<NotSupportedException>(() => choke.RequestBytes(12345));
        }
    }
}
