using System;

namespace Koek
{
    /// <summary>
    /// Returns whatever the local machine considers to be the current time.
    /// </summary>
    public sealed class LocalTimeSource : ITimeSource
    {
        public DateTimeOffset GetCurrentTime() => DateTimeOffset.UtcNow;
    }
}
