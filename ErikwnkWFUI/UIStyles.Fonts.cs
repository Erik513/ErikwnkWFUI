using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>The fonts used across ErikwnkWFUI's controls.</summary>
        public static class Fonts
        {
            /// <summary>Bold, for headings/titles.</summary>
            public static Font Title { get { return UIFonts.Title; } }
            /// <summary>Default body text.</summary>
            public static Font Normal { get { return UIFonts.Normal; } }
            /// <summary>Smaller, de-emphasized text.</summary>
            public static Font Small { get { return UIFonts.Small; } }
            /// <summary>Fixed-width, for code/tabular text.</summary>
            public static Font Monospace { get { return UIFonts.Monospace; } }
            /// <summary>Segoe UI Symbol glyphs, used for icon buttons.</summary>
            public static Font Icon { get { return UIFonts.Icon; } }
            /// <summary>Color emoji glyphs.</summary>
            public static Font Emoji { get { return UIFonts.Emoji; } }
        }
    }
}
