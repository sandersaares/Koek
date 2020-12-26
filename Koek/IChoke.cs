namespace Koek
{
    /// <summary>
    /// Limits data transfer rate to achieve the expected result in the desired time.
    /// Uses TBF model (http://lartc.org/manpages/tc-tbf.html).
    /// </summary>
    /// <remarks>
    /// Thread-safe.
    /// </remarks>
    public interface IChoke
    {
        /// <summary>
        /// Gets the configured rate limit.
        /// </summary>
        DataRate RateLimit { get; }

        /// <summary>
        /// Requests a number of bytes from the choke.
        /// </summary>
        bool RequestBytes(ushort count);

        /// <summary>
        /// Max size of a request that can be accommodated.
        /// </summary>
        long BucketSizeBytes { get; }
    }
}
