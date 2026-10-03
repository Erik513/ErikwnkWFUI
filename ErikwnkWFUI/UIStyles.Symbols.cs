using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Ready-made symbols as text characters - plus, trash can, arrows, gear,
        /// lock and so on - for buttons, labels and menu items. Being text, they
        /// take the color of the control they are on and scale with its font.
        /// </summary>
        /// <remarks>
        /// Show them in <see cref="Fonts.Icon"/> (but see <see cref="Symbols.GetFontName"/>
        /// for the few that need another font) through a control that draws with
        /// <c>TextRenderer</c> (a <c>Button</c>, a <c>Label</c> from
        /// <see cref="Labels"/>, a menu item) - Windows then fills in the symbols that
        /// are not in that font itself. Drawing them with <c>Graphics.DrawString</c>
        /// can show empty boxes instead. See the Symbols tab of the Showcase for
        /// what each looks like. A button that carries one is made with
        /// <c>UIStyles.Buttons.CreateSymbol</c>.
        /// </remarks>
        public static class Symbols
        {
            private const string IconFontName = "Segoe MDL2 Assets";

            // Windows 11's newer icon font. Only symbols that are much better there
            // use it (Copy: two rounded sheets, not the old pair of pages).
            private const string FluentFontName = "Segoe Fluent Icons";

            private static readonly bool _isIconFontInstalled = IsFontInstalled(IconFontName);
            private static readonly bool _isFluentFontInstalled = IsFontInstalled(FluentFontName);

            /// <summary>
            /// The name of the font a symbol has to be drawn in. Nearly all are drawn in
            /// <see cref="Fonts.Icon"/>; the few that come from the Windows icon font
            /// (<c>Delete</c>, <c>Copy</c>, <c>Print</c>, <c>Filter</c> ... - everything whose character is in the private-use range) need "Segoe MDL2 Assets", which Windows 10 and 11 have.
            /// <c>UIStyles.Buttons.CreateSymbol</c> picks the right one by itself; use this
            /// when you draw a symbol yourself.
            /// </summary>
            public static string GetFontName(string symbol)
            {
                bool isIconFontSymbol = !string.IsNullOrEmpty(symbol) && symbol[0] >= '\uE000' && symbol[0] <= '\uF8FF';

                if (isIconFontSymbol && symbol == Copy && _isFluentFontInstalled)
                    return FluentFontName;

                return isIconFontSymbol && _isIconFontInstalled
                    ? IconFontName
                    : UIFonts.Icon.FontFamily.Name;
            }

            // The arrows of the chevrons fill the whole character cell and look too
            // big next to the other symbols, so a button draws them a bit smaller.
            // The lightning bolt does too, a little less.
            private const float ChevronSizeFactor = 0.7f;
            private const float LightningSizeFactor = 0.8f;

            internal static float GetSizeFactor(string symbol)
            {
                bool isChevron = symbol == ChevronLeft || symbol == ChevronRight
                    || symbol == ChevronUp || symbol == ChevronDown;

                if (symbol == Lightning)
                    return LightningSizeFactor;

                return isChevron ? ChevronSizeFactor : 1f;
            }

            private static bool IsFontInstalled(string name)
            {
                using (System.Drawing.Text.InstalledFontCollection fonts = new System.Drawing.Text.InstalledFontCollection())
                {
                    foreach (System.Drawing.FontFamily family in fonts.Families)
                    {
                        if (family.Name == name)
                            return true;
                    }
                }

                return false;
            }

            // ---- actions ----
            public const string Add = "➕";
            public const string Remove = "➖";
            // A trash can from the Windows icon font (see GetFontName): the trash
            // can emoji is a color glyph that ignores the text color and comes out
            // thin and dim, and no other character looks like deleting.
            public const string Delete = "\uE74D";
            public const string Edit = "✎";
            public const string Save = "\U0001F4BE";
            public const string Refresh = "↻";
            public const string Search = "\U0001F50D";
            public const string Settings = "⚙";
            public const string Close = "✕";
            public const string Cancel = "✖";
            public const string Check = "✔";
            public const string Undo = "↶";
            public const string Redo = "↷";
            public const string Cut = "✂";
            public const string Paste = "\uE77F";
            // Print, Info, Warning, Globe, Paste, Download, Copy and the other private-use characters below come from the Windows icon font
            // (see GetFontName): bold, clear shapes where the emoji or text
            // characters are thin, colored or hard to tell apart when small.
            // Copy: two rounded sheets in Windows 11's icon font, the older pair of pages in Windows 10's.
            public const string Copy = "\uE8C8";
            public const string Print = "\uE749";
            public const string Download = "\uE896";
            public const string Upload = "\uE898";
            public const string Share = "\uE72D";
            public const string Filter = "\uE71C";
            public const string Sort = "\uE8CB";
            public const string ZoomIn = "\uE8A3";
            public const string ZoomOut = "\uE71F";
            public const string Fullscreen = "\uE740";
            public const string Send = "\uE724";

            // ---- files ----
            public const string File = "\U0001F4C4";
            public const string Folder = "\U0001F4C1";
            public const string FolderOpen = "\U0001F4C2";

            // ---- navigation ----
            public const string Back = "⬅";
            public const string Forward = "➡";
            public const string Up = "⬆";
            public const string Down = "⬇";
            public const string Home = "⌂";
            public const string Menu = "☰";
            // Solid triangles: sort arrows, "something is up" markers.
            public const string TriangleUp = "\u25B2";
            public const string TriangleDown = "\u25BC";
            public const string ChevronLeft = "\uE76B";
            public const string ChevronRight = "\uE76C";
            public const string ChevronUp = "\uE70E";
            public const string ChevronDown = "\uE70D";

            // ---- media ----
            public const string Play = "▶";
            public const string Pause = "⏸";
            public const string Stop = "⏹";
            public const string Record = "⏺";
            public const string SkipBack = "⏮";
            public const string SkipForward = "⏭";
            public const string Volume = "\U0001F50A";
            public const string Mute = "\U0001F507";
            public const string Music = "♫";
            public const string Shuffle = "\uE8B1";
            public const string Repeat = "\uE8EE";

            // ---- status ----
            public const string Info = "\uE946";
            public const string Warning = "\uE7BA";
            public const string Error = "⛔";
            public const string Help = "❓";
            public const string Lightning = "\uE945";
            public const string Heart = "♥";

            // ---- window (the buttons of the title bar) ----
            public const string Minimize = "\U0001F5D5";
            public const string Maximize = "\U0001F5D6";
            public const string Restore = "\u2750";

            // ---- people and security ----
            public const string User = "\U0001F464";
            public const string Lock = "\U0001F512";
            public const string Unlock = "\U0001F513";
            public const string Logout = "\uF3B1";
            public const string Power = "⏻";

            // ---- things ----
            public const string Mail = "✉";
            public const string Calendar = "\U0001F4C5";
            public const string Clock = "⏰";
            public const string Bell = "\U0001F514";
            public const string Link = "\U0001F517";
            public const string Pin = "\U0001F4CC";
            public const string Globe = "\uE774";
            public const string Eye = "\U0001F441";
        }
    }
}
