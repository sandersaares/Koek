using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Koek
{
    public sealed class TraceWriter
    {
        private readonly string _category;
        private readonly TraceSwitch _switch;

        private TraceWriter(string category)
        {
            _category = category;
            _switch = new TraceSwitch(category, "");
        }

        public static TraceWriter ForType<T>()
        {
            return new TraceWriter(typeof(T).FullName);
        }

        public static TraceWriter ForTypeAndSubcategory<T>(string subcategory)
        {
            return new TraceWriter(typeof(T).FullName + "/" + subcategory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose(string message)
        {
            if (_switch.TraceVerbose)
                Trace.WriteLine(message, _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message)
        {
            if (_switch.TraceInfo)
                Trace.WriteLine(message, _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message)
        {
            if (_switch.TraceWarning)
                Trace.WriteLine(message, _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message)
        {
            if (_switch.TraceError)
                Trace.WriteLine(message, _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose(FormattableString message)
        {
            if (_switch.TraceVerbose)
                Trace.WriteLine(message.ToString(), _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(FormattableString message)
        {
            if (_switch.TraceInfo)
                Trace.WriteLine(message.ToString(), _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(FormattableString message)
        {
            if (_switch.TraceWarning)
                Trace.WriteLine(message.ToString(), _category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(FormattableString message)
        {
            if (_switch.TraceError)
                Trace.WriteLine(message.ToString(), _category);
        }
    }
}
