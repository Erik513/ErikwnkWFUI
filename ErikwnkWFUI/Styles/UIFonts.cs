using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>The fonts used across ErikwnkWFUI's controls - fixed, not part of the theme system.</summary>
    /// <remarks>Each font is one object shared by every control and for the whole run of the program: do not dispose it, and do not change it (make a copy with <c>new Font(font, style)</c> for another style).</remarks>
    public static class UIFonts
    {
        /// <summary>Bold, for headings/titles.</summary>
        public static readonly Font Title = new Font("Segoe UI", 10, FontStyle.Bold);
        /// <summary>Default body text.</summary>
        public static readonly Font Normal = new Font("Segoe UI", 9);
        /// <summary>Smaller, de-emphasized text.</summary>
        public static readonly Font Small = new Font("Segoe UI", 8);
        /// <summary>Fixed-width, for code/tabular text.</summary>
        public static readonly Font Monospace = new Font("Consolas", 9);
        /// <summary>Segoe UI Symbol glyphs, used for icon buttons.</summary>
        public static readonly Font Icon = new Font("Segoe UI Symbol", 13f);
        /// <summary>Color emoji glyphs.</summary>
        public static readonly Font Emoji = new Font("Segoe UI Emoji", 11);
    }
}
