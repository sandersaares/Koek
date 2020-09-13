using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Koek
{
    public static partial class Helpers
    {
        /// <summary>
        /// Automatically does TraceSwitch checking and assigns the type full name as the category for trace messages.
        /// </summary>
        public static class Trace<T>
        {
            private static readonly string TypeFullName = typeof(T).FullName;
            private static TraceSwitch Switch => new TraceSwitch(TypeFullName, "");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Verbose(string message)
            {
                if (Switch.TraceVerbose)
                    Trace.WriteLine(message, TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Info(string message)
            {
                if (Switch.TraceInfo)
                    Trace.WriteLine(message, TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Warning(string message)
            {
                if (Switch.TraceWarning)
                    Trace.WriteLine(message, TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Error(string message)
            {
                if (Switch.TraceError)
                    Trace.WriteLine(message, TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Verbose(FormattableString message)
            {
                if (Switch.TraceVerbose)
                    Trace.WriteLine(message.ToString(), TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Info(FormattableString message)
            {
                if (Switch.TraceInfo)
                    Trace.WriteLine(message.ToString(), TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Warning(FormattableString message)
            {
                if (Switch.TraceWarning)
                    Trace.WriteLine(message.ToString(), TypeFullName);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Error(FormattableString message)
            {
                if (Switch.TraceError)
                    Trace.WriteLine(message.ToString(), TypeFullName);
            }
        }
    }
}
