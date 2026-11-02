using System.Reflection;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The name of the program that uses the library: its entry assembly, not the library itself. Where there is no entry assembly (some test
    /// runners) it is "Program". The folders and keys the library keeps for a program (window places, the appearance, the entry in the Run key) are named by it.
    /// </summary>
    internal static class EntryAssemblyName
    {
        /// <summary>The simple name of the entry assembly, or "Program".</summary>
        public static string Get()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "Program";
        }
    }
}
