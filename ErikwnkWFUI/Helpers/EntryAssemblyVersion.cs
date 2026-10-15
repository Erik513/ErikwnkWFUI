using System;
using System.Reflection;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The version of the program that uses the library. The entry assembly, not the executing one - that would be ErikwnkWFUI.dll
    /// itself, never the consuming app. Falls back to the executing assembly only for the unusual hosts (some test runners)
    /// where there is no entry assembly.
    /// </summary>
    internal static class EntryAssemblyVersion
    {
        public static Version Get()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            return assembly.GetName().Version;
        }

        /// <summary>The version as shown to the user, like "v1.2.3"; null when the assembly has none.</summary>
        public static string GetText()
        {
            Version version = Get();

            return version != null ? "v" + version.ToString(3) : null;
        }
    }
}
