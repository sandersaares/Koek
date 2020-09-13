using System;
using System.Text;

namespace Koek
{
    /// <summary>
    /// Base class to coordinate global tasks in automated test projects.
    /// </summary>
    public abstract class BaseTestClass
    {
        static BaseTestClass()
        {
            // In VSTS automated build processes, Console.InputEncoding is UTF-8 with byte order mark.
            // This causes this encoding to be propagated in Process.Start() which means we get BOMs
            // whenever we write to stdin anywhere. Terrible idea - we get rid of the BOM here!
            Console.InputEncoding = new UTF8Encoding(false);
        }

        public static void EnsureInitialized()
        {
        }
    }
}