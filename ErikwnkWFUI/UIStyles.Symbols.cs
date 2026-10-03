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

            private static readonly bool _isIconFontInstalled = IsFontInstalled(IconFontName);
            private static readonly bool _isFluentFontInstalled = IsFontInstalled(FluentFontName);

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
            /// <summary>A plus.</summary>
            public const string Add = "➕";
            /// <summary>A minus.</summary>
            public const string Remove = "➖";
            /// <summary>
            /// A trash can, from the Windows icon font: the trash can emoji is a color glyph
            /// that ignores the text color and comes out thin and dim, and no other character
            /// looks like deleting.
            /// </summary>
            public const string Delete = "\uE74D";
            /// <summary>A pencil.</summary>
            public const string Edit = "✎";
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
            public const string Settings = "⚙";
            /// <summary>A cross.</summary>
            public const string Close = "✕";
            /// <summary>A heavy cross.</summary>
            public const string Cancel = "✖";
            /// <summary>A check mark.</summary>
            public const string Check = "✔";
            /// <summary>A curved arrow pointing back.</summary>
            public const string Undo = "↶";
            /// <summary>A curved arrow pointing forward.</summary>
            public const string Redo = "↷";
            /// <summary>Scissors.</summary>
            public const string Cut = "✂";
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
            /// <summary>A closed folder.</summary>
            public const string Folder = "\U0001F4C1";
            /// <summary>An open folder.</summary>
            public const string FolderOpen = "\U0001F4C2";

            // ---- navigation ----
            /// <summary>An arrow to the left.</summary>
            public const string Back = "⬅";
            /// <summary>An arrow to the right.</summary>
            public const string Forward = "➡";
            /// <summary>An arrow up.</summary>
            public const string Up = "⬆";
            /// <summary>An arrow down.</summary>
            public const string Down = "⬇";
            /// <summary>A house.</summary>
            public const string Home = "⌂";
            /// <summary>Three lines.</summary>
            public const string Menu = "☰";
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
            public const string Play = "▶";
            /// <summary>Two bars.</summary>
            public const string Pause = "⏸";
            /// <summary>A square.</summary>
            public const string Stop = "⏹";
            /// <summary>A circle.</summary>
            public const string Record = "⏺";
            /// <summary>A bar with a double triangle pointing left.</summary>
            public const string SkipBack = "⏮";
            /// <summary>A bar with a double triangle pointing right.</summary>
            public const string SkipForward = "⏭";
            /// <summary>A loudspeaker with sound waves.</summary>
            public const string Volume = "\U0001F50A";
            /// <summary>A crossed-out loudspeaker.</summary>
            public const string Mute = "\U0001F507";
            /// <summary>A musical note.</summary>
            public const string Music = "♫";
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
            public const string Error = "⛔";
            /// <summary>A question mark.</summary>
            public const string Help = "❓";
            /// <summary>A lightning bolt.</summary>
            public const string Lightning = "\uE945";
            /// <summary>A heart.</summary>
            public const string Heart = "♥";

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
            public const string Power = "⏻";

            // ---- things ----
            /// <summary>An envelope.</summary>
            public const string Mail = "✉";
            /// <summary>A calendar page.</summary>
            public const string Calendar = "\U0001F4C5";
            /// <summary>An alarm clock.</summary>
            public const string Clock = "⏰";
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
        }
    }
}
