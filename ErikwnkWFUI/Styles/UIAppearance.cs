using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Styles
{
    /// <summary>The two ready-made themes by name, so they can be kept in a settings file.</summary>
    public enum UIThemeKind
    {
        /// <summary>The dark theme.</summary>
        Dark,

        /// <summary>The light theme.</summary>
        Light
    }

    /// <summary>
    /// What the user chose to make the library look like: theme, accent color and language - the part of the settings that belongs to the library.
    /// Only plain values (an enum, a text, an enum), so it can be a member of the program's own settings class and be written with whatever
    /// the program writes its settings with; or it is kept in a file of its own with <see cref="Save"/> and <see cref="Load"/>.
    /// <see cref="Capture"/> reads what is applied now, <see cref="Apply"/> applies it.
    /// </summary>
    public sealed class UIAppearance
    {
        /// <summary>The theme.</summary>
        public UIThemeKind Theme { get; set; }

        /// <summary>The accent color as <c>#RRGGBB</c>; null or not a color: the accent stays as it is when applied.</summary>
        public string Accent { get; set; }

        /// <summary>The language of the library and of the program's own texts (the one language setting).</summary>
        public UILanguage Language { get; set; }

        /// <summary>The appearance that is applied now.</summary>
        public static UIAppearance Capture()
        {
            Color primary = UIColors.Primary;

            return new UIAppearance
            {
                Theme = UIColors.IsLight(UIColors.BackgroundMedium) ? UIThemeKind.Light : UIThemeKind.Dark,
                Accent = string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", primary.R, primary.G, primary.B),
                Language = UIStrings.Language
            };
        }

        /// <summary>The accent as a color, or null when there is none (or it is not a color).</summary>
        public Color? GetAccentColor()
        {
            string text = Accent == null ? null : Accent.Trim();

            if (text == null || text.Length != 7 || text[0] != '#')
                return null;

            int rgb;

            if (!int.TryParse(text.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rgb))
                return null;

            return Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        /// <summary>Applies the theme, the accent (when there is one) and the language; every control of the library follows by itself.</summary>
        public void Apply()
        {
            UIColors.ApplyTheme(Theme == UIThemeKind.Light ? UIThemes.Light : UIThemes.Dark);

            Color? accent = GetAccentColor();

            if (accent.HasValue)
                UIColors.SetAccent(accent.Value);

            UIStrings.Language = Language;
        }

        /// <summary>Writes the appearance to a file (a small JSON object), the folder is created. Failing to write is no error: returns false.</summary>
        public bool Save(string path)
        {
            try
            {
                string folder = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);

                StringBuilder text = new StringBuilder();
                text.Append("{\n  \"Theme\": \"").Append(Theme).Append("\",\n");

                if (GetAccentColor().HasValue)
                    text.Append("  \"Accent\": \"").Append(Accent.Trim().ToUpperInvariant()).Append("\",\n");

                text.Append("  \"Language\": \"").Append(Language).Append("\"\n}\n");
                File.WriteAllText(path, text.ToString());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Reads a file written by <see cref="Save"/>; null when there is none or it cannot be read. What is missing or wrong in it stays at its default.</summary>
        public static UIAppearance Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string text = File.ReadAllText(path);
                UIAppearance appearance = new UIAppearance();

                UIThemeKind theme;
                UILanguage language;

                if (TryReadName(ReadValue(text, "Theme"), out theme))
                    appearance.Theme = theme;

                if (TryReadName(ReadValue(text, "Language"), out language))
                    appearance.Language = language;

                appearance.Accent = ReadValue(text, "Accent");

                return appearance;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // A name only: Enum.TryParse would also accept a number, one that is none of the values too.
        private static bool TryReadName<T>(string text, out T value) where T : struct
        {
            value = default(T);

            return !string.IsNullOrEmpty(text) && !char.IsDigit(text[0]) && text[0] != '-' && Enum.TryParse(text, true, out value) && Enum.IsDefined(typeof(T), value);
        }

        private static string ReadValue(string json, string name)
        {
            Match match = Regex.Match(json, "\"" + name + "\"\\s*:\\s*\"([^\"]*)\"");

            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
