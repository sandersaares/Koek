using System;

namespace Koek
{
    /// <summary>
    /// Returns the current time, allowing time logic to be substituted in tests.
    /// </summary>
    public interface ITimeSource
    {
        DateTimeOffset GetCurrentTime();
    }
}
