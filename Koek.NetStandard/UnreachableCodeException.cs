using System;

namespace Koek
{
    /// <summary>
    /// Used in to ensure that a piece of code that should never be reachable will not accidentally execute.
    /// </summary>
    public sealed class UnreachableCodeException : Exception
    {
    }
}