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
        /// Show them in <see cref="Fonts.Icon"/> through a control that draws with
        /// <c>TextRenderer</c> (a <c>Button</c>, a <c>Label</c> from
        /// <see cref="Labels"/>, a menu item) - Windows then fills in the symbols that
        /// are not in that font itself. Drawing them with <c>Graphics.DrawString</c>
        /// can show empty boxes instead.
        ///
        /// The symbols whose character is in the private-use range (written
        /// <c>"\uE..."</c> or <c>"\uF..."</c> in the source: Delete, Print, Filter, Info,
        /// Warning, the chevrons and others) come from the Windows icon font and have to be
        /// drawn in that one: <see cref="Symbols.GetFontName"/> says which font a symbol
        /// needs. They are clear, bold shapes where an emoji or a text character would be
        /// thin, colored or hard to tell apart when small.
        ///
        /// See the Symbols tab of the Showcase for what each looks like. A button that
        /// carries one is made with <c>UIStyles.Buttons.CreateSymbol</c>, and every symbol
        /// has a ready-made design in <c>UIStyles.Buttons.Presets</c>.
        /// </remarks>
        public static class Symbols
        {
            private const string IconFontName = "Segoe MDL2 Assets";

            // Windows 11's newer icon font. Only symbols that are much better there
            // use it (Copy: two rounded sheets, not the old pair of pages).
            private const string FluentFontName = "Segoe Fluent Icons";

            // The emoji font: a symbol that ends with the emoji selector (U+FE0F) is drawn in it.
            private const string EmojiFontName = "Segoe UI Emoji";
            private const char EmojiSelector = '\uFE0F';

            private static readonly bool _isIconFontInstalled;
            private static readonly bool _isFluentFontInstalled;
            private static readonly bool _isEmojiFontInstalled;

            // Which of the two fonts Windows has, looked up once (the list of
            // installed fonts is not cheap to read). If it cannot be read at all, the
            // fonts count as missing: a symbol then falls back to the usual font
            // instead of every symbol button failing.
            static Symbols()
            {
                try
                {
                    using (System.Drawing.Text.InstalledFontCollection fonts = new System.Drawing.Text.InstalledFontCollection())
                    {
                        foreach (System.Drawing.FontFamily family in fonts.Families)
                        {
                            if (family.Name == IconFontName)
                                _isIconFontInstalled = true;
                            else if (family.Name == FluentFontName)
                                _isFluentFontInstalled = true;
                            else if (family.Name == EmojiFontName)
                                _isEmojiFontInstalled = true;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // The installed fonts cannot be listed: both symbol fonts count as not installed.
                }
            }

            /// <summary>
            /// The name of the font a symbol has to be drawn in. Most are drawn in
            /// <see cref="Fonts.Icon"/>; the ones that come from the Windows icon font
            /// (<c>Delete</c>, <c>Print</c>, <c>Filter</c>, <c>Lightning</c> ... - every symbol
            /// whose character is in the private-use range) need "Segoe MDL2 Assets", which
            /// Windows 10 and 11 have; <c>Copy</c> uses "Segoe Fluent Icons" where Windows has it
            /// (Windows 11). Without the icon font the usual font is returned, so nothing
            /// throws - but those symbols then show as boxes.
            /// <c>UIStyles.Buttons.CreateSymbol</c> picks the right one by itself; use this
            /// when you draw a symbol yourself.
            /// </summary>
            public static string GetFontName(string symbol)
            {
                if (!string.IsNullOrEmpty(symbol) && symbol[symbol.Length - 1] == EmojiSelector && _isEmojiFontInstalled)
                    return EmojiFontName;

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

            // How large a symbol button draws a symbol compared to the usual size for
            // its height: 1 for nearly all, less for the few that look too big.
            internal static float GetSizeFactor(string symbol)
            {
                if (symbol == Lightning)
                    return LightningSizeFactor;

                if (symbol == ChevronLeft || symbol == ChevronRight || symbol == ChevronUp || symbol == ChevronDown)
                    return ChevronSizeFactor;

                return 1f;
            }

            // ---- actions ----
            /// <summary>A plus.</summary>
            public const string Add = "\u2795";
            /// <summary>A minus.</summary>
            public const string Remove = "\u2796";
            /// <summary>
            /// A trash can, from the Windows icon font: the trash can emoji is a color glyph
            /// that ignores the text color and comes out thin and dim, and no other character
            /// looks like deleting.
            /// </summary>
            public const string Delete = "\uE74D";
            /// <summary>A pencil.</summary>
            public const string Edit = "\u270E";
            /// <summary>A floppy disk.</summary>
            public const string Save = "\U0001F4BE";
            /// <summary>A circular arrow.</summary>
            public const string Refresh = "\uE72C";
            /// <summary>Two arrows chasing each other.</summary>
            public const string Sync = "\uE895";
            /// <summary>A circular arrow turning back.</summary>
            public const string Update = "\uE777";
            /// <summary>A magnifier.</summary>
            public const string Search = "\U0001F50D";
            /// <summary>A gear.</summary>
            public const string Settings = "\u2699";
            /// <summary>A cross.</summary>
            public const string Close = "\u2715";
            /// <summary>A heavy cross.</summary>
            public const string Cancel = "\u2716";
            /// <summary>A check mark.</summary>
            public const string Check = "\u2714";
            /// <summary>A curved arrow pointing back.</summary>
            public const string Undo = "\u21B6";
            /// <summary>A curved arrow pointing forward.</summary>
            public const string Redo = "\u21B7";
            /// <summary>Scissors.</summary>
            public const string Cut = "\u2702";
            /// <summary>A clipboard with a sheet.</summary>
            public const string Paste = "\uE77F";
            /// <summary>Two overlapping sheets (rounded ones in Windows 11, the older pair of pages in Windows 10).</summary>
            public const string Copy = "\uE8C8";
            /// <summary>A printer.</summary>
            public const string Print = "\uE749";
            /// <summary>An arrow down onto a line.</summary>
            public const string Download = "\uE896";
            /// <summary>An arrow up from a line.</summary>
            public const string Upload = "\uE898";
            /// <summary>A box with an arrow out of it.</summary>
            public const string Share = "\uE72D";
            /// <summary>A funnel.</summary>
            public const string Filter = "\uE71C";
            /// <summary>Two arrows, one up and one down.</summary>
            public const string Sort = "\uE8CB";
            /// <summary>A magnifier with a plus.</summary>
            public const string ZoomIn = "\uE8A3";
            /// <summary>A magnifier with a minus.</summary>
            public const string ZoomOut = "\uE71F";
            /// <summary>Arrows to the corners.</summary>
            public const string Fullscreen = "\uE740";
            /// <summary>A paper plane.</summary>
            public const string Send = "\uE724";

            // ---- files ----
            /// <summary>A page.</summary>
            public const string File = "\U0001F4C4";
            /// <summary>
            /// A page with lines of text, drawn in the emoji font (Segoe UI Emoji): the outlined
            /// page that Windows shows for a document, where <see cref="File"/> is a plain
            /// filled page. The emoji font brings its own colors for the lines.
            /// </summary>
            public const string Document = "\U0001F4C4\uFE0F";
            /// <summary>A closed folder.</summary>
            public const string Folder = "\U0001F4C1";
            /// <summary>An open folder.</summary>
            public const string FolderOpen = "\U0001F4C2";

            // ---- navigation ----
            /// <summary>An arrow to the left.</summary>
            public const string Back = "\u2B05";
            /// <summary>An arrow to the right.</summary>
            public const string Forward = "\u27A1";
            /// <summary>An arrow up.</summary>
            public const string Up = "\u2B06";
            /// <summary>An arrow down.</summary>
            public const string Down = "\u2B07";
            /// <summary>A house.</summary>
            public const string Home = "\u2302";
            /// <summary>Three lines.</summary>
            public const string Menu = "\u2630";
            /// <summary>A solid triangle pointing up - for sort arrows and markers.</summary>
            public const string TriangleUp = "\u25B2";
            /// <summary>A solid triangle pointing down - for sort arrows and markers.</summary>
            public const string TriangleDown = "\u25BC";
            /// <summary>A small arrow to the left.</summary>
            public const string ChevronLeft = "\uE76B";
            /// <summary>A small arrow to the right.</summary>
            public const string ChevronRight = "\uE76C";
            /// <summary>A small arrow up.</summary>
            public const string ChevronUp = "\uE70E";
            /// <summary>A small arrow down.</summary>
            public const string ChevronDown = "\uE70D";

            // ---- media ----
            /// <summary>A triangle pointing right.</summary>
            public const string Play = "\u25B6";
            /// <summary>Two bars.</summary>
            public const string Pause = "\u23F8";
            /// <summary>A square.</summary>
            public const string Stop = "\u23F9";
            /// <summary>A circle.</summary>
            public const string Record = "\u23FA";
            /// <summary>A bar with a double triangle pointing left.</summary>
            public const string SkipBack = "\u23EE";
            /// <summary>A bar with a double triangle pointing right.</summary>
            public const string SkipForward = "\u23ED";
            /// <summary>A loudspeaker with sound waves.</summary>
            public const string Volume = "\U0001F50A";
            /// <summary>A crossed-out loudspeaker.</summary>
            public const string Mute = "\U0001F507";
            /// <summary>A musical note.</summary>
            public const string Music = "\u266B";
            /// <summary>Two crossing arrows.</summary>
            public const string Shuffle = "\uE8B1";
            /// <summary>Arrows in a loop.</summary>
            public const string Repeat = "\uE8EE";

            // ---- status ----
            /// <summary>An i in a circle.</summary>
            public const string Info = "\uE946";
            /// <summary>A triangle with an exclamation mark.</summary>
            public const string Warning = "\uE7BA";
            /// <summary>A no-entry sign.</summary>
            public const string Error = "\u26D4";
            /// <summary>A question mark.</summary>
            public const string Help = "\u2753";
            /// <summary>A lightning bolt.</summary>
            public const string Lightning = "\uE945";
            /// <summary>A heart.</summary>
            public const string Heart = "\u2665";

            // ---- window (the buttons of the title bar) ----
            /// <summary>A bar at the bottom, as on the title bar of a window.</summary>
            public const string Minimize = "\U0001F5D5";
            /// <summary>A single frame, as on the title bar of a window.</summary>
            public const string Maximize = "\U0001F5D6";
            /// <summary>Two overlapping frames, as on the title bar of a maximized window.</summary>
            public const string Restore = "\u2750";

            // ---- people and security ----
            /// <summary>A person.</summary>
            public const string User = "\U0001F464";
            /// <summary>A closed padlock.</summary>
            public const string Lock = "\U0001F512";
            /// <summary>An open padlock.</summary>
            public const string Unlock = "\U0001F513";
            /// <summary>An arrow leaving a frame.</summary>
            public const string Logout = "\uF3B1";
            /// <summary>The power sign.</summary>
            public const string Power = "\u23FB";

            // ---- things ----
            /// <summary>An envelope.</summary>
            public const string Mail = "\u2709";
            /// <summary>A calendar page.</summary>
            public const string Calendar = "\U0001F4C5";
            /// <summary>An alarm clock.</summary>
            public const string Clock = "\u23F0";
            /// <summary>A bell.</summary>
            public const string Bell = "\U0001F514";
            /// <summary>A chain link.</summary>
            public const string Link = "\U0001F517";
            /// <summary>A pushpin.</summary>
            public const string Pin = "\U0001F4CC";
            /// <summary>A globe.</summary>
            public const string Globe = "\uE774";
            /// <summary>An eye.</summary>
            public const string Eye = "\U0001F441";
            /// <summary>A sun (brightness), from the Windows icon font: the light theme.</summary>
            public const string Sun = "";
            /// <summary>A crescent moon (quiet hours), from the Windows icon font: the dark theme.</summary>
            public const string Moon = "";
        }
    }
}
