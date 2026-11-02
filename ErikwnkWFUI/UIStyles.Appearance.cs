using System;
using System.IO;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        // The file once RememberAppearance was called (null: the program keeps the appearance itself).
        private static string _appearancePath;

        // The folder of the file; the seam of the tests (null: the default one).
        internal static string AppearanceDirectory;

        /// <summary>
        /// The one line for a program that has no settings of its own for it: the theme, accent color and language the user chose are loaded and
        /// applied now (call it at startup, before the first window is made) and kept in <c>%AppData%\ErikwnkWFUI\&lt;program&gt;\appearance.json</c>.
        /// They are written when the user saves in an <see cref="ErikwnkWFUI.Forms.OptionsForm"/> and when the program calls
        /// <see cref="SaveAppearance"/> - not at every change, because an options window takes back what was only tried. A program that keeps
        /// its settings itself uses <see cref="UIAppearance"/> and does not call this. Returns whether something was loaded.
        /// </summary>
        public static bool RememberAppearance()
        {
            string program = EntryAssemblyName.Get();
            string folder = AppearanceDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ErikwnkWFUI", program);

            _appearancePath = Path.Combine(folder, "appearance.json");

            UIAppearance appearance = UIAppearance.Load(_appearancePath);

            if (appearance == null)
                return false;

            appearance.Apply();

            return true;
        }

        /// <summary>Stops <see cref="RememberAppearance"/>: nothing is written any more (the file stays).</summary>
        public static void ForgetAppearance()
        {
            _appearancePath = null;
        }

        /// <summary>Writes the appearance that is applied now to the file of <see cref="RememberAppearance"/>; false when it is not used or the file cannot be written.</summary>
        public static bool SaveAppearance()
        {
            return _appearancePath != null && UIAppearance.Capture().Save(_appearancePath);
        }
    }
}
