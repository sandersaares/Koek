namespace Koek
{
    /// <summary>
    /// A choke implementation that applies no limits to throughput.
    /// </summary>
    public sealed class UnlimitedChoke : IChoke
    {
        public static UnlimitedChoke Instance { get; } = new UnlimitedChoke();

        public DataRate RateLimit { get; } = DataRate.FromBitsPerSecond(long.MaxValue);
        public long BucketSizeBytes => long.MaxValue;
        public bool RequestBytes(ushort count) => true;
    }
}
