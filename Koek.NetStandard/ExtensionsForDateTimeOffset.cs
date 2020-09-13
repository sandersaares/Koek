using System;

namespace Koek
{
    public static class ExtensionsForDateTimeOffset
    {
        public static bool IsInRange(this DateTimeOffset date, DateTimeOffset minDate, DateTimeOffset maxDate)
        {
            return minDate <= date && date <= maxDate;
        }
    }
}