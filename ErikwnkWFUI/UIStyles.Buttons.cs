using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Flat, themed <see cref="Button"/>s with a caption: <see cref="CreateStandard"/> (neutral),
        /// <see cref="CreatePrimary"/> (accent color), <see cref="CreateGreen"/> (confirm) and
        /// <see cref="CreateRed"/> (delete). The disabled, hover and pressed looks are built in.
        /// A button that carries a symbol instead of a caption always comes from
        /// <c>CreateSymbol</c>, in one of two ways: with a ready-made <see cref="Presets">preset</see>
        /// (<c>CreateSymbol(Presets.Add, "Add")</c>), or by choosing the symbol and the colors
        /// yourself (<c>CreateSymbol(Symbols.Folder, "Choose folder", size, Colors.Sets.Yellow)</c>).
        /// </summary>
        public static class Buttons
        {
            /// <summary>The neutral button.</summary>
            /// <param name="text">The caption.</param>
            /// <param name="tooltip">Tooltip text; empty for none.</param>
            /// <param name="size">The size; 30 x 30 when left out.</param>
            /// <param name="isIcon">True to use the symbol font without inner padding, for a glyph instead of text. For a symbol, <c>CreateSymbol</c> is the better choice: it centers the glyph exactly.</param>
            /// <param name="colors">
            /// A color set to use instead of the neutral look, for a button in a color of
            /// your own: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c>,
            /// or simply a <see cref="Color"/>. Fill, hover, pressed and disabled looks and the
            /// text on each are then worked out for you, the same in Dark and Light.
            /// </param>
            public static Button CreateStandard(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false,
                UIColorSet colors = null)
            {
                if (colors != null)
                {
                    return UIButtonFactory.CreateColored(
                        text,
                        tooltip,
                        size,
                        isIcon,
                        colors);
                }

                return UIButtonFactory.CreateStandard(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            /// <summary>The main button of a screen, in the current accent color.</summary>
            /// <param name="text">The caption.</param>
            /// <param name="tooltip">Tooltip text; empty for none.</param>
            /// <param name="size">The size; 30 x 30 when left out.</param>
            /// <param name="isIcon">True to use the symbol font without inner padding, for a glyph instead of text. For a symbol, <c>CreateSymbol</c> is the better choice: it centers the glyph exactly.</param>
            public static Button CreatePrimary(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreatePrimary(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            /// <summary>A green button, for confirming.</summary>
            /// <param name="text">The caption.</param>
            /// <param name="tooltip">Tooltip text; empty for none.</param>
            /// <param name="size">The size; 30 x 30 when left out.</param>
            /// <param name="isIcon">True to use the symbol font without inner padding, for a glyph instead of text. For a symbol, <c>CreateSymbol</c> is the better choice: it centers the glyph exactly.</param>
            public static Button CreateGreen(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateGreen(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            /// <summary>A red button, for deleting or other destructive actions.</summary>
            /// <param name="text">The caption.</param>
            /// <param name="tooltip">Tooltip text; empty for none.</param>
            /// <param name="size">The size; 30 x 30 when left out.</param>
            /// <param name="isIcon">True to use the symbol font without inner padding, for a glyph instead of text. For a symbol, <c>CreateSymbol</c> is the better choice: it centers the glyph exactly.</param>
            public static Button CreateRed(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateRed(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            /// <summary>
            /// Makes a symbol button from a ready-made design - the simplest way. The
            /// <paramref name="preset"/> already says which symbol the button carries and in
            /// which colors; you only add what the button is for (the tooltip) and, if the
            /// standard 30 x 30 is not what you want, its size.
            /// </summary>
            /// <remarks>
            /// The ready-made presets are in <see cref="Presets"/> (Add, Delete, Save, Browse ...).
            /// Need a design they do not have? Make your own <see cref="UIButtonPreset"/> once and
            /// use it everywhere, or choose symbol and colors directly with the other
            /// <c>CreateSymbol</c> overload. A preset is nothing but a symbol and a color set, so both
            /// ways give the same button.
            /// </remarks>
            /// <example>
            /// <code>
            /// // A preset brings the symbol and the colors:
            /// var save = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Save, "Save");
            ///
            /// // The same, larger:
            /// var big = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Delete, "Delete", new Size(48, 48));
            /// </code>
            /// </example>
            /// <param name="preset">The design, for example <c>UIStyles.Buttons.Presets.Add</c>.</param>
            /// <param name="tooltip">The tooltip text (the button has no caption, so this is what tells the user what it does); empty for none.</param>
            /// <param name="size">The size of the button; 30 x 30 when left out. The symbol grows with the height.</param>
            public static Button CreateSymbol(
                UIButtonPreset preset,
                string tooltip = "",
                Size? size = null)
            {
                if (preset == null)
                    throw new ArgumentNullException(nameof(preset));

                // A preset is only a symbol plus colors - the work is the same as below.
                return CreateSymbol(
                    preset.Symbol,
                    tooltip,
                    size,
                    preset.Colors);
            }

            /// <summary>
            /// Makes a symbol button when you want to choose the symbol and the colors yourself:
            /// any symbol from <see cref="Symbols"/> (or a symbol of your own), in the neutral look
            /// or in the colors of a color set.
            /// </summary>
            /// <remarks>
            /// The button draws its symbol itself, exactly in the middle, and the symbol grows with
            /// the height of the button. For that reason an image, <c>TextAlign</c>, <c>Padding</c>
            /// and the other layout properties of a button are ignored on it (a button with a caption
            /// from <c>CreateStandard</c> and the others keeps them). Hover, pressed and disabled looks and the color of the
            /// symbol in each of them come from the color set, so they are the same in Dark and
            /// Light. Without <paramref name="colors"/> the button has the neutral look and follows
            /// the theme. For a ready-made combination of symbol and colors use the
            /// <see cref="CreateSymbol(UIButtonPreset, string, Size?)">preset overload</see> instead.
            /// </remarks>
            /// <example>
            /// <code>
            /// // Symbol and colors chosen by hand (this is what Presets.Browse is):
            /// var browse = UIStyles.Buttons.CreateSymbol(
            ///     UIStyles.Symbols.Folder, "Choose folder", new Size(50, 30), UIStyles.Colors.Sets.Yellow);
            ///
            /// // Neutral look, no color set:
            /// var back = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Back, "Back");
            ///
            /// // Any color works - a plain Color is turned into a color set:
            /// var heart = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Heart, "Like", colors: Color.Orange);
            /// </code>
            /// </example>
            /// <param name="symbol">The symbol, for example <c>UIStyles.Symbols.Delete</c>, or any character of your own.</param>
            /// <param name="tooltip">The tooltip text (the button has no caption, so this is what tells the user what it does); empty for none.</param>
            /// <param name="size">The size of the button; 30 x 30 when left out. The symbol grows with the height.</param>
            /// <param name="colors">
            /// The colors of the button: one of <c>UIStyles.Colors.Sets</c>, a set you made with
            /// <c>UIStyles.Colors.CreateSet(...)</c>, or simply a <see cref="Color"/>. Left out, the
            /// button has the neutral look of <see cref="CreateStandard"/>.
            /// </param>
            public static Button CreateSymbol(
                string symbol,
                string tooltip = "",
                Size? size = null,
                UIColorSet colors = null)
            {
                return UIButtonFactory.CreateSymbol(
                    symbol,
                    tooltip,
                    size,
                    colors);
            }

            /// <summary>
            /// Ready-made designs - there is one for every symbol of <see cref="Symbols"/>, and the
            /// buttons most programs need (Add, Delete, Save, Browse ...) come first. Each one is a symbol with
            /// its own fixed colors (green to add, save or confirm, red to delete, close or stop, blue to edit, reload
            /// or send, yellow to choose a file or folder, dark gray for navigation and everything
            /// without a color of its own), the same in Dark and
            /// Light and with any accent. Hand one to
            /// <see cref="CreateSymbol(UIButtonPreset, string, Size?)"/> to get the button. A preset
            /// you would like a little different is a copy away
            /// (<see cref="UIButtonPreset.WithColors"/>, <see cref="UIButtonPreset.WithSymbol"/>), and
            /// your own presets are made with <see cref="UIButtonPreset"/>.
            /// </summary>
            public static class Presets
            {
                // The symbol of a preset is white in every state. The fills are those of
                // the set, only a fill too bright for a white symbol (3:1) is darkened a
                // little. The yellow keeps its bright fills: white on yellow is its look.
                private static UIColorSet WhiteOn(UIColorSet set)
                {
                    return ReferenceEquals(set, Colors.Sets.Yellow)
                        ? UIColors.WithText(set, UIColors.White)
                        : UIColors.WithText(set, UIColors.White, 3.0);
                }

                /// <summary>Adding an entry: a plus on green.</summary>
                public static UIButtonPreset Add { get; } = new UIButtonPreset(Symbols.Add, WhiteOn(Colors.Sets.Green));

                /// <summary>Saving: a disk on green.</summary>
                public static UIButtonPreset Save { get; } = new UIButtonPreset(Symbols.Save, WhiteOn(Colors.Sets.Green));

                /// <summary>Deleting an entry: a trash can on red.</summary>
                public static UIButtonPreset Delete { get; } = new UIButtonPreset(Symbols.Delete, WhiteOn(Colors.Sets.Red));

                /// <summary>Closing a window or dialog: a cross on red.</summary>
                public static UIButtonPreset Close { get; } = new UIButtonPreset(Symbols.Close, WhiteOn(Colors.Sets.Red));

                /// <summary>Editing an entry: a pencil on blue.</summary>
                public static UIButtonPreset Edit { get; } = new UIButtonPreset(Symbols.Edit, WhiteOn(Colors.Sets.Blue));

                /// <summary>Reloading: a circular arrow on blue.</summary>
                public static UIButtonPreset Refresh { get; } = new UIButtonPreset(Symbols.Refresh, WhiteOn(Colors.Sets.Blue));

                /// <summary>Logging in: a person on blue.</summary>
                public static UIButtonPreset Login { get; } = new UIButtonPreset(Symbols.User, WhiteOn(Colors.Sets.Blue));

                /// <summary>Syncing two sides: two arrows chasing each other, on blue.</summary>
                public static UIButtonPreset Sync { get; } = new UIButtonPreset(Symbols.Sync, WhiteOn(Colors.Sets.Blue));

                /// <summary>Updating or restoring: a circular arrow against the clock, on green.</summary>
                public static UIButtonPreset Update { get; } = new UIButtonPreset(Symbols.Update, WhiteOn(Colors.Sets.Green));

                /// <summary>Searching: a magnifier on dark gray.</summary>
                public static UIButtonPreset Search { get; } = new UIButtonPreset(Symbols.Search, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening the settings: a gear on dark gray.</summary>
                public static UIButtonPreset Settings { get; } = new UIButtonPreset(Symbols.Settings, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going back: an arrow to the left on dark gray.</summary>
                public static UIButtonPreset Back { get; } = new UIButtonPreset(Symbols.Back, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going forward: an arrow to the right on dark gray.</summary>
                public static UIButtonPreset Forward { get; } = new UIButtonPreset(Symbols.Forward, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one level up: an arrow up on dark gray.</summary>
                public static UIButtonPreset Up { get; } = new UIButtonPreset(Symbols.Up, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one level down: an arrow down on dark gray.</summary>
                public static UIButtonPreset Down { get; } = new UIButtonPreset(Symbols.Down, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going to the start: a house on dark gray.</summary>
                public static UIButtonPreset Home { get; } = new UIButtonPreset(Symbols.Home, WhiteOn(Colors.Sets.Dark));

                /// <summary>Choosing a file: a document on yellow.</summary>
                public static UIButtonPreset OpenFile { get; } = new UIButtonPreset(Symbols.File, WhiteOn(Colors.Sets.Yellow));

                /// <summary>Choosing a folder: a folder on yellow.</summary>
                public static UIButtonPreset Browse { get; } = new UIButtonPreset(Symbols.Folder, WhiteOn(Colors.Sets.Yellow));

                // ---- one for every other symbol, named like the symbol ----

                /// <summary>Taking an item out of a list: a minus, on red.</summary>
                public static UIButtonPreset Remove { get; } = new UIButtonPreset(Symbols.Remove, WhiteOn(Colors.Sets.Red));

                /// <summary>Cancelling: a heavy cross, on red.</summary>
                public static UIButtonPreset Cancel { get; } = new UIButtonPreset(Symbols.Cancel, WhiteOn(Colors.Sets.Red));

                /// <summary>Confirming: a check mark, on green.</summary>
                public static UIButtonPreset Check { get; } = new UIButtonPreset(Symbols.Check, WhiteOn(Colors.Sets.Green));

                /// <summary>Undoing the last step: a curved arrow back, on dark gray.</summary>
                public static UIButtonPreset Undo { get; } = new UIButtonPreset(Symbols.Undo, WhiteOn(Colors.Sets.Dark));

                /// <summary>Redoing a step: a curved arrow forward, on dark gray.</summary>
                public static UIButtonPreset Redo { get; } = new UIButtonPreset(Symbols.Redo, WhiteOn(Colors.Sets.Dark));

                /// <summary>Cutting: scissors, on dark gray.</summary>
                public static UIButtonPreset Cut { get; } = new UIButtonPreset(Symbols.Cut, WhiteOn(Colors.Sets.Dark));

                /// <summary>Copying: two overlapping sheets, on dark gray.</summary>
                public static UIButtonPreset Copy { get; } = new UIButtonPreset(Symbols.Copy, WhiteOn(Colors.Sets.Dark));

                /// <summary>Pasting: a clipboard with a sheet, on dark gray.</summary>
                public static UIButtonPreset Paste { get; } = new UIButtonPreset(Symbols.Paste, WhiteOn(Colors.Sets.Dark));

                /// <summary>Printing: a printer, on dark gray.</summary>
                public static UIButtonPreset Print { get; } = new UIButtonPreset(Symbols.Print, WhiteOn(Colors.Sets.Dark));

                /// <summary>Downloading: an arrow down onto a line, on blue.</summary>
                public static UIButtonPreset Download { get; } = new UIButtonPreset(Symbols.Download, WhiteOn(Colors.Sets.Blue));

                /// <summary>Uploading: an arrow up from a line, on blue.</summary>
                public static UIButtonPreset Upload { get; } = new UIButtonPreset(Symbols.Upload, WhiteOn(Colors.Sets.Blue));

                /// <summary>Sharing: a box with an arrow out of it, on blue.</summary>
                public static UIButtonPreset Share { get; } = new UIButtonPreset(Symbols.Share, WhiteOn(Colors.Sets.Blue));

                /// <summary>Filtering: a funnel, on dark gray.</summary>
                public static UIButtonPreset Filter { get; } = new UIButtonPreset(Symbols.Filter, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sorting: lines of different length, on dark gray.</summary>
                public static UIButtonPreset Sort { get; } = new UIButtonPreset(Symbols.Sort, WhiteOn(Colors.Sets.Dark));

                /// <summary>Zooming in: a magnifier with a plus, on dark gray.</summary>
                public static UIButtonPreset ZoomIn { get; } = new UIButtonPreset(Symbols.ZoomIn, WhiteOn(Colors.Sets.Dark));

                /// <summary>Zooming out: a magnifier with a minus, on dark gray.</summary>
                public static UIButtonPreset ZoomOut { get; } = new UIButtonPreset(Symbols.ZoomOut, WhiteOn(Colors.Sets.Dark));

                /// <summary>Full screen: arrows to the corners, on dark gray.</summary>
                public static UIButtonPreset Fullscreen { get; } = new UIButtonPreset(Symbols.Fullscreen, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sending: a paper plane, on blue.</summary>
                public static UIButtonPreset Send { get; } = new UIButtonPreset(Symbols.Send, WhiteOn(Colors.Sets.Blue));

                /// <summary>A document: the outlined page with lines of text (emoji font), on dark gray.</summary>
                public static UIButtonPreset Document { get; } = new UIButtonPreset(Symbols.Document, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening a folder: an open folder, on dark gray.</summary>
                public static UIButtonPreset FolderOpen { get; } = new UIButtonPreset(Symbols.FolderOpen, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening a menu: three lines, on dark gray.</summary>
                public static UIButtonPreset Menu { get; } = new UIButtonPreset(Symbols.Menu, WhiteOn(Colors.Sets.Dark));

                /// <summary>Marking something that goes up, or sorting upward: a solid triangle, on dark gray.</summary>
                public static UIButtonPreset TriangleUp { get; } = new UIButtonPreset(Symbols.TriangleUp, WhiteOn(Colors.Sets.Dark));

                /// <summary>Marking something that goes down, or sorting downward: a solid triangle, on dark gray.</summary>
                public static UIButtonPreset TriangleDown { get; } = new UIButtonPreset(Symbols.TriangleDown, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one page back: a small arrow to the left, on dark gray.</summary>
                public static UIButtonPreset ChevronLeft { get; } = new UIButtonPreset(Symbols.ChevronLeft, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one page on: a small arrow to the right, on dark gray.</summary>
                public static UIButtonPreset ChevronRight { get; } = new UIButtonPreset(Symbols.ChevronRight, WhiteOn(Colors.Sets.Dark));

                /// <summary>Collapsing: a small arrow up, on dark gray.</summary>
                public static UIButtonPreset ChevronUp { get; } = new UIButtonPreset(Symbols.ChevronUp, WhiteOn(Colors.Sets.Dark));

                /// <summary>Expanding: a small arrow down, on dark gray.</summary>
                public static UIButtonPreset ChevronDown { get; } = new UIButtonPreset(Symbols.ChevronDown, WhiteOn(Colors.Sets.Dark));

                /// <summary>Starting: a triangle, on green.</summary>
                public static UIButtonPreset Play { get; } = new UIButtonPreset(Symbols.Play, WhiteOn(Colors.Sets.Green));

                /// <summary>Pausing: two bars, on dark gray.</summary>
                public static UIButtonPreset Pause { get; } = new UIButtonPreset(Symbols.Pause, WhiteOn(Colors.Sets.Dark));

                /// <summary>Stopping: a square, on red.</summary>
                public static UIButtonPreset Stop { get; } = new UIButtonPreset(Symbols.Stop, WhiteOn(Colors.Sets.Red));

                /// <summary>Recording: a circle, on red.</summary>
                public static UIButtonPreset Record { get; } = new UIButtonPreset(Symbols.Record, WhiteOn(Colors.Sets.Red));

                /// <summary>Skipping to the previous item, on dark gray.</summary>
                public static UIButtonPreset SkipBack { get; } = new UIButtonPreset(Symbols.SkipBack, WhiteOn(Colors.Sets.Dark));

                /// <summary>Skipping to the next item, on dark gray.</summary>
                public static UIButtonPreset SkipForward { get; } = new UIButtonPreset(Symbols.SkipForward, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sound on: a loudspeaker, on dark gray.</summary>
                public static UIButtonPreset Volume { get; } = new UIButtonPreset(Symbols.Volume, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sound off: a crossed loudspeaker, on dark gray.</summary>
                public static UIButtonPreset Mute { get; } = new UIButtonPreset(Symbols.Mute, WhiteOn(Colors.Sets.Dark));

                /// <summary>Music: a note, on purple.</summary>
                public static UIButtonPreset Music { get; } = new UIButtonPreset(Symbols.Music, WhiteOn(Colors.Sets.Purple));

                /// <summary>Random order: crossing arrows, on dark gray.</summary>
                public static UIButtonPreset Shuffle { get; } = new UIButtonPreset(Symbols.Shuffle, WhiteOn(Colors.Sets.Dark));

                /// <summary>Repeating: arrows in a loop, on dark gray.</summary>
                public static UIButtonPreset Repeat { get; } = new UIButtonPreset(Symbols.Repeat, WhiteOn(Colors.Sets.Dark));

                /// <summary>Information: an i, on blue.</summary>
                public static UIButtonPreset Info { get; } = new UIButtonPreset(Symbols.Info, WhiteOn(Colors.Sets.Blue));

                /// <summary>A warning: a triangle with an exclamation mark, on orange.</summary>
                public static UIButtonPreset Warning { get; } = new UIButtonPreset(Symbols.Warning, WhiteOn(Colors.Sets.Orange));

                /// <summary>An error: a no-entry sign, on red.</summary>
                public static UIButtonPreset Error { get; } = new UIButtonPreset(Symbols.Error, WhiteOn(Colors.Sets.Red));

                /// <summary>Help: a question mark, on blue.</summary>
                public static UIButtonPreset Help { get; } = new UIButtonPreset(Symbols.Help, WhiteOn(Colors.Sets.Blue));

                /// <summary>Starting something at once: a lightning bolt, on dark gray.</summary>
                public static UIButtonPreset Lightning { get; } = new UIButtonPreset(Symbols.Lightning, WhiteOn(Colors.Sets.Dark));

                /// <summary>Liking: a heart, on pink.</summary>
                public static UIButtonPreset Heart { get; } = new UIButtonPreset(Symbols.Heart, WhiteOn(Colors.Sets.Pink));

                /// <summary>Locked: a closed padlock, on dark gray.</summary>
                public static UIButtonPreset Lock { get; } = new UIButtonPreset(Symbols.Lock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Unlocked: an open padlock, on dark gray.</summary>
                public static UIButtonPreset Unlock { get; } = new UIButtonPreset(Symbols.Unlock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Signing out: an arrow leaving a frame, on dark gray.</summary>
                public static UIButtonPreset Logout { get; } = new UIButtonPreset(Symbols.Logout, WhiteOn(Colors.Sets.Dark));

                /// <summary>Switching off: the power sign, on red.</summary>
                public static UIButtonPreset Power { get; } = new UIButtonPreset(Symbols.Power, WhiteOn(Colors.Sets.Red));

                /// <summary>Mail: an envelope, on blue.</summary>
                public static UIButtonPreset Mail { get; } = new UIButtonPreset(Symbols.Mail, WhiteOn(Colors.Sets.Blue));

                /// <summary>Dates: a calendar, on blue.</summary>
                public static UIButtonPreset Calendar { get; } = new UIButtonPreset(Symbols.Calendar, WhiteOn(Colors.Sets.Blue));

                /// <summary>Time: an alarm clock, on dark gray.</summary>
                public static UIButtonPreset Clock { get; } = new UIButtonPreset(Symbols.Clock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Notifications: a bell, on orange.</summary>
                public static UIButtonPreset Bell { get; } = new UIButtonPreset(Symbols.Bell, WhiteOn(Colors.Sets.Orange));

                /// <summary>A link: a chain, on blue.</summary>
                public static UIButtonPreset Link { get; } = new UIButtonPreset(Symbols.Link, WhiteOn(Colors.Sets.Blue));

                /// <summary>Pinning: a pin, on dark gray.</summary>
                public static UIButtonPreset Pin { get; } = new UIButtonPreset(Symbols.Pin, WhiteOn(Colors.Sets.Dark));

                /// <summary>The web: a globe, on blue.</summary>
                public static UIButtonPreset Globe { get; } = new UIButtonPreset(Symbols.Globe, WhiteOn(Colors.Sets.Blue));

                /// <summary>Showing or watching: an eye, on dark gray.</summary>
                public static UIButtonPreset Eye { get; } = new UIButtonPreset(Symbols.Eye, WhiteOn(Colors.Sets.Dark));

                /// <summary>Minimizing a window: a bar at the bottom, on dark gray.</summary>
                public static UIButtonPreset Minimize { get; } = new UIButtonPreset(Symbols.Minimize, WhiteOn(Colors.Sets.Dark));

                /// <summary>Maximizing a window: a single frame, on dark gray.</summary>
                public static UIButtonPreset Maximize { get; } = new UIButtonPreset(Symbols.Maximize, WhiteOn(Colors.Sets.Dark));

                /// <summary>Bringing a maximized window back: two frames, on dark gray.</summary>
                public static UIButtonPreset Restore { get; } = new UIButtonPreset(Symbols.Restore, WhiteOn(Colors.Sets.Dark));
            }

            /// <summary>
            /// Changes the tooltip text of a button made by this class, for example
            /// after a language switch. Does nothing for a button created without a
            /// tooltip.
            /// </summary>
            public static void UpdateTooltip(Button button, string tooltip)
            {
                UIButtonFactory.UpdateTooltip(button, tooltip);
            }
        }
    }
}
