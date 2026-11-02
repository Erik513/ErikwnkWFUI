using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// A curated palette of base colors for <see cref="UIColors.SetAccent"/>,
    /// picked at roughly the same brightness/saturation as <see cref="Blue"/>
    /// (the default) so every choice produces a comparably good set of
    /// derived light/dark shades. Not exhaustive - SetAccent takes any Color,
    /// this is just a ready-made "pick one" set for a color-picker UI.
    /// </summary>
    public static class UIAccentColors
    {
        /// <summary>ErikwnkWFUI's original, always-has-been-the-default accent.</summary>
        public static readonly Color Blue = Color.FromArgb(0, 90, 158);

        /// <summary>A red accent.</summary>
        public static readonly Color Red = Color.FromArgb(200, 45, 45);
        /// <summary>An orange accent.</summary>
        public static readonly Color Orange = Color.FromArgb(210, 110, 20);
        /// <summary>An amber accent.</summary>
        public static readonly Color Amber = Color.FromArgb(200, 150, 0);
        /// <summary>A yellow accent (bright: the text on it turns dark).</summary>
        public static readonly Color Yellow = Color.FromArgb(215, 190, 20);
        /// <summary>A green accent.</summary>
        public static readonly Color Green = Color.FromArgb(30, 140, 70);
        /// <summary>A teal accent.</summary>
        public static readonly Color Teal = Color.FromArgb(0, 130, 114);
        /// <summary>A cyan accent.</summary>
        public static readonly Color Cyan = Color.FromArgb(0, 130, 170);
        /// <summary>An indigo accent.</summary>
        public static readonly Color Indigo = Color.FromArgb(65, 80, 180);
        /// <summary>A purple accent.</summary>
        public static readonly Color Purple = Color.FromArgb(110, 60, 170);
        /// <summary>A magenta accent.</summary>
        public static readonly Color Magenta = Color.FromArgb(170, 40, 130);
        /// <summary>A pink accent.</summary>
        public static readonly Color Pink = Color.FromArgb(200, 60, 110);
        /// <summary>A brown accent.</summary>
        public static readonly Color Brown = Color.FromArgb(120, 75, 45);
        // Close to, but not pixel-identical with, CreateStandard's own
        // fixed neutral (UIColors.BorderLight) - CreatePrimary derives its
        // hover/pressed shades from this by percentage, so they land at
        // slightly different values than CreateStandard's own dedicated
        // ones. Good enough for someone who just wants "gray" without
        // knowing CreateStandard already defaults to it.
        /// <summary>A neutral gray accent.</summary>
        public static readonly Color Gray = Color.FromArgb(90, 90, 90);

        // A fixed white goes invisible against a light theme's own
        // near-white background - resolved live off the current theme
        // instead, the same way UIColors.GetContrastingForeColor already
        // picks light-or-dark text for a given background. A property, not
        // a field, so switching themes changes what this returns without
        // needing to be told to.
        /// <summary>A neutral accent that contrasts with the page: near-white on the dark theme, near-black on the light one. Worked out when read, so it follows the theme.</summary>
        public static Color BlackOrWhite => UIColors.GetContrastingForeColor(UIColors.BackgroundDark);

        // A property, not a field, for the same reason as BlackOrWhite -
        // a frozen array would otherwise cache whatever theme was active
        // the first time this class was touched.
        /// <summary>Every accent of the palette in one array, for a color picker. A new array on every read: <see cref="BlackOrWhite"/> follows the theme.</summary>
        public static Color[] All => new[]
        {
            Blue, Red, Orange, Amber, Yellow, Green, Teal,
            Cyan, Indigo, Purple, Magenta, Pink, Brown, Gray, BlackOrWhite
        };
    }
}
