using System;

namespace Koek
{
    public sealed class Choke : IChoke
    {
        /// <summary>
        /// Maximum amount of data that we can accumulate budget for after a refill.
        /// Any accumulated budget beyond this is discarded.
        /// </summary>
        /// <remarks>
        /// Thread-safe.
        /// 
        /// Ideally we'll never fill the bucket in active use (it will always be depleted).
        /// This just matters for the first burst after a pause in transmissions.
        /// This pause may be intentional (ran out of data to send) or forced (CPU overload - big interval).
        /// </remarks>
        private static readonly TimeSpan BucketSize = TimeSpan.FromSeconds(0.05);

        /// <summary>
        /// We refill the bucket only in steps this big, for easier calculations (avoid messing with fractions).
        /// </summary>
        private const int RefillStepSizeBytes = 256;

        public DataRate RateLimit { get; }

        public long BucketSizeBytes { get; }

        public Choke(DataRate targetRate, IStopwatch? stopwatch = null)
        {
            Helpers.Argument.ValidateRange(targetRate.BytesPerSecond, nameof(targetRate.BytesPerSecond), min: 1);

            RateLimit = targetRate;

            _stopwatch = stopwatch ?? new RealStopwatch();

            BucketSizeBytes = (long)(targetRate.BytesPerSecond * BucketSize.TotalSeconds);
            _availableCapacity = BucketSizeBytes;
            _lastRefillTime = _stopwatch.GetTimestamp();

            // How many ticks does it take to increase budget by one refill step?

            // Rounds down - safer is to send less often.
            var refillStepsPerSecond = targetRate.BytesPerSecond / RefillStepSizeBytes;

            if (refillStepsPerSecond == 0)
                throw new ArgumentException("Target rate is too low for meaningful operation.", nameof(targetRate));

            _ticksPerRefillStep = _stopwatch.Frequency / refillStepsPerSecond;

            // Rounds up - safer is to send less often (pay more ticks per refill step).
            if (_stopwatch.Frequency % refillStepsPerSecond != 0)
                _ticksPerRefillStep++;
        }

        private readonly IStopwatch _stopwatch;

        private long _availableCapacity;
        private long _lastRefillTime;
        private readonly long _ticksPerRefillStep;

        private readonly object _lock = new();

        /// <summary>
        /// Requests the choke to allow usage of a certain amount of data transfer capacity in bytes.
        /// The size of this data should be small (on the range of 1 packet).
        /// </summary>
        /// <returns>True if allowed, false if caller must wait and try again.</returns>
        public bool RequestBytes(ushort count)
        {
            if (count > BucketSizeBytes)
                throw new NotSupportedException($"The requested capacity ({count}) is more than can ever be supplied ({BucketSizeBytes}).");

            lock (_lock)
            {
                TryRefill();

                if (_availableCapacity >= count)
                {
                    _availableCapacity -= count;
                    return true;
                }

                return false;
            }
        }

        private void TryRefill()
        {
            // High-resolution timestamps can experience negative time on some CPUs during frequency switching.
            // Our penalty logic may also drive the "last refill" value into the future intentionally. Just ignore time travel.
            var elapsedTicks = Math.Max(0, _stopwatch.GetTimestamp() - _lastRefillTime);

            var refillSteps = elapsedTicks / _ticksPerRefillStep;

            if (refillSteps == 0)
                return; // No refill happening, too not enough time passed since last try.

            var refillBytes = refillSteps * RefillStepSizeBytes;

            // This refunds any remainder from the calculation.
            _lastRefillTime += refillSteps * _ticksPerRefillStep;

            _availableCapacity += refillBytes;

            if (_availableCapacity > BucketSizeBytes)
                _availableCapacity = BucketSizeBytes;
        }
    }
}
