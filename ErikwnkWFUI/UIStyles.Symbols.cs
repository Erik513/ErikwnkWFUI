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
        /// can show empty boxes instead. See the Symbols tab of the Showcase for
        /// what each looks like. A button that carries one is made with
        /// <see cref="Buttons.CreateSymbol"/>.
        /// </remarks>
        public static class Symbols
        {
            // ---- actions ----
            public const string Add = "➕";
            public const string Remove = "➖";
            // The recycle sign: the trash can emoji is a color glyph that ignores
            // the text color and comes out thin and dim on a button.
            public const string Delete = "\u267B";
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
            public const string Clipboard = "\U0001F4CB";
            public const string Print = "\U0001F5A8";

            // ---- files ----
            public const string File = "\U0001F4C4";
            public const string Folder = "\U0001F4C1";
            public const string FolderOpen = "\U0001F4C2";
            public const string Image = "\U0001F5BC";

            // ---- navigation ----
            public const string Back = "⬅";
            public const string Forward = "➡";
            public const string Up = "⬆";
            public const string Down = "⬇";
            public const string Home = "⌂";
            public const string Menu = "☰";
            public const string ChevronLeft = "‹";
            public const string ChevronRight = "›";

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

            // ---- status ----
            public const string Info = "ℹ";
            public const string Warning = "⚠";
            public const string Error = "⛔";
            public const string Help = "❓";
            public const string Star = "★";
            public const string StarOutline = "☆";
            public const string Heart = "♥";

            // ---- people and security ----
            public const string User = "\U0001F464";
            public const string Key = "\U0001F511";
            public const string Lock = "\U0001F512";
            public const string Unlock = "\U0001F513";
            public const string Power = "⏻";

            // ---- things ----
            public const string Mail = "✉";
            public const string Calendar = "\U0001F4C5";
            public const string Clock = "⏰";
            public const string Bell = "\U0001F514";
            public const string Link = "\U0001F517";
            public const string Pin = "\U0001F4CC";
            public const string Globe = "\U0001F310";
            public const string Eye = "\U0001F441";
        }
    }
}
