using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Where a window was (position, size, whether it was maximized), kept in a small text file per window name in the application data
    /// folder of the current user: <c>ErikwnkWFUI\&lt;program&gt;\&lt;name&gt;.txt</c>. Failing to read or write is never an error - the window
    /// just opens where it would anyway.
    /// </summary>
    internal static class WindowPlacementStore
    {
        // The least of a window that has to be on a screen for the place to be taken (the title bar can be reached then).
        private const int MinimumVisibleWidth = 100;
        private const int MinimumVisibleHeight = 40;

        /// <summary>The folder of the files; the seam of the tests (null: the default one).</summary>
        internal static string Directory;

        public struct Placement
        {
            public Rectangle Bounds;
            public bool Maximized;
        }

        public static void Save(string key, Rectangle bounds, bool maximized)
        {
            try
            {
                string folder = GetDirectory();
                System.IO.Directory.CreateDirectory(folder);

                string text = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3};{4}", bounds.X, bounds.Y, bounds.Width, bounds.Height, maximized ? 1 : 0);
                File.WriteAllText(GetPath(folder, key), text);
            }
            catch (Exception)
            {
                // Nothing to remember it in: the next start is as the first.
            }
        }

        /// <summary>The place that was saved, when there is one and the window would still be on a screen that is there (<paramref name="workingAreas"/>: the work areas of the screens).</summary>
        public static bool TryLoad(string key, Rectangle[] workingAreas, out Placement placement)
        {
            placement = new Placement();

            try
            {
                string path = GetPath(GetDirectory(), key);

                if (!File.Exists(path))
                    return false;

                string[] parts = File.ReadAllText(path).Trim().Split(';');

                if (parts.Length != 5)
                    return false;

                int[] numbers = parts.Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
                Rectangle bounds = new Rectangle(numbers[0], numbers[1], numbers[2], numbers[3]);

                if (bounds.Width <= 0 || bounds.Height <= 0 || !IsReachable(bounds, workingAreas))
                    return false;

                placement = new Placement { Bounds = bounds, Maximized = numbers[4] == 1 };

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Whether enough of the window is on one of the screens to move it by its title bar.</summary>
        internal static bool IsReachable(Rectangle bounds, Rectangle[] workingAreas)
        {
            foreach (Rectangle area in workingAreas)
            {
                Rectangle visible = Rectangle.Intersect(bounds, area);

                if (visible.Width >= MinimumVisibleWidth && visible.Height >= MinimumVisibleHeight)
                    return true;
            }

            return false;
        }

        public static Rectangle[] GetWorkingAreas()
        {
            return Screen.AllScreens.Select(screen => screen.WorkingArea).ToArray();
        }

        private static string GetDirectory()
        {
            if (Directory != null)
                return Directory;

            string program = EntryAssemblyName.Get();

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ErikwnkWFUI", program);
        }

        private static string GetPath(string folder, string key)
        {
            string safe = new string(key.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());

            return Path.Combine(folder, safe + ".txt");
        }
    }
}
