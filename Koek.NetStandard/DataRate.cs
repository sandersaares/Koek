using System;

namespace Koek
{
    /// <summary>
    /// Data rates are often measured in bytes per second. But also bits per second!
    /// Use this struct to make it impossible to confuse them - the user can always choose whether to use bits or bytes.
    /// </summary>
    public struct DataRate : IEquatable<DataRate>, IComparable<DataRate>
    {
        public long BitsPerSecond => _bitsPerSecond;
        public long BytesPerSecond => _bitsPerSecond / 8;

        private readonly long _bitsPerSecond;

        private DataRate(long bitsPerSecond)
        {
            _bitsPerSecond = bitsPerSecond;
        }

        public static DataRate FromBitsPerSecond(long bitsPerSecond)
        {
            return new DataRate(bitsPerSecond);
        }

        public static DataRate FromBytesPerSecond(long bytesPerSecond) => FromBitsPerSecond(bytesPerSecond * 8);

        #region Operators
        public override bool Equals(object? obj)
        {
            return obj is DataRate && Equals((DataRate)obj);
        }

        public bool Equals(DataRate other)
        {
            return _bitsPerSecond == other._bitsPerSecond;
        }

        public override int GetHashCode()
        {
            var hashCode = 478336789;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + _bitsPerSecond.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(DataRate rate1, DataRate rate2) => rate1.Equals(rate2);
        public static bool operator !=(DataRate rate1, DataRate rate2) => !(rate1 == rate2);
        public static bool operator <(DataRate a, DataRate b) => a._bitsPerSecond < b._bitsPerSecond;
        public static bool operator >(DataRate a, DataRate b) => a._bitsPerSecond < b._bitsPerSecond;

        public int CompareTo(DataRate other) => _bitsPerSecond.CompareTo(other.BitsPerSecond);
        #endregion
    }
}
