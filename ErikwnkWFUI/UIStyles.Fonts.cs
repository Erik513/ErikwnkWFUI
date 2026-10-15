using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>The fonts used across ErikwnkWFUI's controls.</summary>
        public static class Fonts
        {
            public static Font Title { get { return UIFonts.Title; } }
            public static Font Normal { get { return UIFonts.Normal; } }
            public static Font Small { get { return UIFonts.Small; } }
            public static Font Monospace { get { return UIFonts.Monospace; } }
            public static Font Icon { get { return UIFonts.Icon; } }
            public static Font Emoji { get { return UIFonts.Emoji; } }
        }
    }
}
