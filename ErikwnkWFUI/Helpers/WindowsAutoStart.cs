using System;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Whether the program starts when the user signs in to Windows: the entry of the program in the "Run" key of the user
    /// (<c>HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run</c>), named like the program. It needs no rights of an
    /// administrator and is the same list Windows shows in Task Manager, "Startup apps". The state lives in Windows, not in the program's
    /// own settings. A program that the user has switched off in Task Manager ("Startup apps") is not started with Windows, whatever the Run key
    /// says: <see cref="IsEnabled"/> says so, and <see cref="Set"/> switches it on again.
    /// </summary>
    public static class WindowsAutoStart
    {
        // The seams of the tests: another key (so the real one is never touched) and another path.
        internal static string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        internal static string ApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        internal static Func<string> ExecutablePath = () => Application.ExecutablePath;

        /// <summary>The name of the entry: the name of the program (its entry assembly).</summary>
        public static string ApplicationName
        {
            get { return Assembly.GetEntryAssembly()?.GetName().Name ?? "Program"; }
        }

        /// <summary>Whether the program starts with Windows: an entry that is there and points to this program, and that the user has not switched off in Task Manager.</summary>
        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    string value = key?.GetValue(ApplicationName) as string;

                    return !string.IsNullOrEmpty(value)
                        && value.IndexOf(ExecutablePath(), StringComparison.OrdinalIgnoreCase) >= 0
                        && !IsSwitchedOffByTheUser();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Task Manager keeps its switch for an entry of the Run key in StartupApproved: a first byte with the lowest bit set (03, 07) is "disabled".
        private static bool IsSwitchedOffByTheUser()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath))
            {
                byte[] state = key?.GetValue(ApplicationName) as byte[];

                return state != null && state.Length > 0 && (state[0] & 1) == 1;
            }
        }

        /// <summary>
        /// Sets or removes the entry. Returns false when Windows would not let it (a policy, no access to the key): nothing is changed
        /// then, and there is no exception. <paramref name="arguments"/> (null: none) are what the program is started with, so that it can tell
        /// that Windows started it. Switching it on also takes back a switch-off in Task Manager.
        /// </summary>
        public static bool Set(bool enabled, string arguments = null)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                        return false;

                    if (enabled)
                    {
                        key.SetValue(ApplicationName, "\"" + ExecutablePath() + "\"" + (string.IsNullOrWhiteSpace(arguments) ? "" : " " + arguments.Trim()));

                        using (RegistryKey approved = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, true))
                            approved?.DeleteValue(ApplicationName, false);
                    }
                    else
                        key.DeleteValue(ApplicationName, false);

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
