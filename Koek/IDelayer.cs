using System;
using System.Threading;
using System.Threading.Tasks;

namespace Koek
{
    /// <summary>
    /// For delay-based timing logic, we would like to use Task.Delay() but this complicates testing of delaying code
    /// as delays in tests make for bad tests. This interface exists to be able to replace the Task.Delay implementation
    /// during test execution with a variant that can be configured with different delay behavior for testing purposes.
    /// </summary>
    public interface IDelayer
    {
        Task Delay(TimeSpan duration);
        Task Delay(TimeSpan duration, CancellationToken cancel);
    }
}
