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
    /// <summary>
    /// The single entry point for ErikwnkWFUI's control factories and styling
    /// - start here rather than the individual classes under
    /// <see cref="ErikwnkWFUI.Factories"/>/<see cref="ErikwnkWFUI.Styles"/>
    /// (most of which are internal). Each nested class below (<see
    /// cref="Buttons"/>, <see cref="Labels"/>, ...) mirrors one control type
    /// with a set of <c>Create*</c> factory methods that return a
    /// pre-themed, ready-to-add WinForms control. See <see cref="Colors"/>
    /// for how to customize the color scheme (accent color and/or full
    /// light/dark theme) and <see cref="Language"/>/<see cref="SetLanguage"/>
    /// for switching the language of built-in dialog text.
    /// </summary>
    public static partial class UIStyles
    {
        /// <summary>
        /// Language used by built-in dialogs/controls that ship their own text
        /// (update prompt, title bar tooltips). Set once at startup, before any
        /// ErikwnkWFUI form is created, to switch away from the English default.
        /// Separate from <see cref="ErikwnkCore.AppLocalization"/>.Language (a
        /// consuming app's OWN strings) - see <see cref="SetLanguage"/> to set
        /// both together, which is what most apps actually want.
        /// </summary>
        public static UILanguage Language
        {
            get { return UIStrings.Language; }
            set { UIStrings.Language = value; }
        }

        /// <summary>
        /// Sets this library's own <see cref="Language"/> and a consuming
        /// app's <see cref="ErikwnkCore.AppLocalization"/>.Language together,
        /// from one <see cref="ErikwnkCore.AppLanguage"/> value. The two are
        /// separate properties, in separate assemblies with separate enums,
        /// because AppLocalization has no WinForms dependency at all and
        /// doesn't know this library exists - but almost every app that uses
        /// both wants them switched in lockstep, and forgetting to set one of
        /// the two is an easy way to end up with the app's own text in one
        /// language and ErikwnkWFUI's built-in dialogs (update prompt, title
        /// bar tooltips) in another. Prefer this over setting
        /// <see cref="Language"/> and AppLocalization.Language individually
        /// unless an app specifically wants them to diverge.
        /// </summary>
        public static void SetLanguage(AppLanguage language)
        {
            AppLocalization.Language = language;
            Language = language == AppLanguage.German ? UILanguage.German : UILanguage.English;
        }

        /// <summary>
        /// Read-only access to every color used across ErikwnkWFUI's controls,
        /// plus <see cref="SetAccent"/>/<see cref="ApplyTheme"/> to customize
        /// them. Defaults to a dark gray theme with a blue accent; nothing
        /// here needs to be set unless you want to change that.
        /// </summary>
        public static class Colors
        {
            public static Color Black { get { return UIColors.Black; } }

            public static Color BackgroundBlack { get { return UIColors.BackgroundBlack; } }
            public static Color BackgroundDark { get { return UIColors.BackgroundDark; } }
            public static Color BackgroundDarkElevated { get { return UIColors.BackgroundDarkElevated; } }
            public static Color BackgroundMedium { get { return UIColors.BackgroundMedium; } }
            public static Color BackgroundMediumElevated { get { return UIColors.BackgroundMediumElevated; } }
            public static Color BackgroundLight { get { return UIColors.BackgroundLight; } }
            public static Color BackgroundLighter { get { return UIColors.BackgroundLighter; } }

            public static Color PrimaryDarkDark { get { return UIColors.PrimaryDarkDark; } }
            public static Color PrimaryDark { get { return UIColors.PrimaryDark; } }
            public static Color Primary { get { return UIColors.Primary; } }
            public static Color PrimaryLight { get { return UIColors.PrimaryLight; } }

            public static Color SecondaryDark { get { return UIColors.SecondaryDark; } }
            public static Color Secondary { get { return UIColors.Secondary; } }
            public static Color SecondaryLight { get { return UIColors.SecondaryLight; } }

            public static Color GreenDark { get { return UIColors.GreenDark; } }
            public static Color Green { get { return UIColors.Green; } }
            public static Color GreenLight { get { return UIColors.GreenLight; } }
            public static Color GreenLighter { get { return UIColors.GreenLighter; } }

            public static Color YellowDark { get { return UIColors.YellowDark; } }
            public static Color Yellow { get { return UIColors.Yellow; } }
            public static Color YellowLight { get { return UIColors.YellowLight; } }
            public static Color YellowLighter { get { return UIColors.YellowLighter; } }

            public static Color RedDark { get { return UIColors.RedDark; } }
            public static Color Red { get { return UIColors.Red; } }
            public static Color RedLight { get { return UIColors.RedLight; } }

            public static Color White { get { return UIColors.White; } }
            public static Color TextPrimary { get { return UIColors.TextPrimary; } }
            public static Color TextPrimaryDim { get { return UIColors.TextPrimaryDim; } }
            public static Color TextSecondary { get { return UIColors.TextSecondary; } }
            public static Color TextTertiary { get { return UIColors.TextTertiary; } }
            public static Color TextDisabled { get { return UIColors.TextDisabled; } }
            public static Color TextMuted { get { return UIColors.TextMuted; } }

            public static Color BorderDark { get { return UIColors.BorderDark; } }
            public static Color BorderMedium { get { return UIColors.BorderMedium; } }
            public static Color BorderLight { get { return UIColors.BorderLight; } }
            public static Color BorderPrimary { get { return UIColors.BorderPrimary; } }
            public static Color BorderRed { get { return UIColors.BorderRed; } }

            public static Color AccentForeColor { get { return UIColors.AccentForeColor; } }

            /// <summary>
            /// A color set made from any color: the color is what a control shows
            /// while hovered, the fill at rest is a darker shade and pressed a
            /// lighter one (a light color such as yellow keeps its look at rest and
            /// gets darker instead). Text and disabled colors are worked out so the
            /// text stays readable on every fill. Pass the result to a control that
            /// takes a <see cref="UIColorSet"/>.
            /// </summary>
            public static UIColorSet CreateSet(Color color)
            {
                return UIColors.CreateSet(color);
            }

            /// <summary>
            /// A color set from the three fills you want (at rest, hovered, pressed);
            /// the text and disabled colors are worked out from them.
            /// </summary>
            public static UIColorSet CreateSet(Color basic, Color hover, Color pressed)
            {
                return UIColors.CreateSet(basic, hover, pressed);
            }

            /// <summary>
            /// Like <see cref="CreateSet(Color, Color, Color)"/>, but the text has the one
            /// color you give in every state (muted when disabled) instead of the best one
            /// for each fill - for a look whose text color is fixed.
            /// </summary>
            public static UIColorSet CreateSet(Color basic, Color hover, Color pressed, Color text)
            {
                return UIColors.CreateSet(basic, hover, pressed, text);
            }

            /// <summary>
            /// Ready-made color sets for the common colors, for when a control should
            /// not use the standard or the primary look. Each one has a fill at rest,
            /// hovered, pressed and disabled plus matching text colors (see
            /// <see cref="UIColorSet"/>). All but <see cref="Primary"/> are the same in
            /// Dark and Light and ignore the accent. To tune one, replace it with your
            /// own <see cref="CreateSet(Color, Color, Color)"/>.
            /// </summary>
            public static class Sets
            {
                /// <summary>The accent color's own look; changes with <see cref="SetAccent"/>.</summary>
                public static UIColorSet Primary
                {
                    get { return UIColors.PrimarySet; }
                }

                public static UIColorSet Red { get; } = UIColors.CreateSet(UIColors.RedDark, UIColors.Red, UIColors.RedLight);
                public static UIColorSet Orange { get; } = UIColors.CreateSet(Color.FromArgb(154, 81, 15), Color.FromArgb(210, 110, 20), Color.FromArgb(236, 141, 55));
                // White text on every state, even on the bright fills.
                public static UIColorSet Yellow { get; } = UIColors.YellowSet;
                public static UIColorSet Green { get; } = UIColors.CreateSet(UIColors.GreenDark, UIColors.Green, UIColors.GreenLight);
                public static UIColorSet Teal { get; } = UIColors.CreateSet(Color.FromArgb(0, 69, 60), Color.FromArgb(0, 130, 114), Color.FromArgb(0, 191, 168));
                public static UIColorSet Blue { get; } = UIColors.CreateSet(Color.FromArgb(0, 55, 97), Color.FromArgb(0, 90, 158), Color.FromArgb(0, 125, 219));
                public static UIColorSet Purple { get; } = UIColors.CreateSet(Color.FromArgb(81, 44, 125), Color.FromArgb(110, 60, 170), Color.FromArgb(141, 93, 198));
                public static UIColorSet Pink { get; } = UIColors.CreateSet(Color.FromArgb(155, 44, 83), Color.FromArgb(200, 60, 110), Color.FromArgb(213, 108, 145));
                public static UIColorSet Brown { get; } = UIColors.CreateSet(Color.FromArgb(75, 47, 28), Color.FromArgb(120, 75, 45), Color.FromArgb(165, 103, 62));
                /// <summary>
                /// The dark gray of <see cref="Buttons.CreateStandard"/> in the dark theme - but fixed:
                /// it stays dark in the light theme too and ignores the accent.
                /// </summary>
                public static UIColorSet Dark { get; } = UIColors.CreateSet(Color.FromArgb(35, 35, 35), Color.FromArgb(60, 60, 60), Color.FromArgb(90, 90, 90));

                public static UIColorSet Gray { get; } = UIColors.CreateSet(Color.FromArgb(75, 75, 75), Color.FromArgb(90, 90, 90), Color.FromArgb(121, 121, 121));
            }

            public static Color HoverOverlay { get { return UIColors.HoverOverlay; } }
            public static Color ActiveOverlay { get { return UIColors.ActiveOverlay; } }
            public static Color Selection { get { return UIColors.Selection; } }

            public static Color Transparent { get { return UIColors.Transparent; } }
            public static Color OverlayDark { get { return UIColors.OverlayDark; } }
            public static Color OverlayMedium { get { return UIColors.OverlayMedium; } }
            public static Color OverlayLight { get { return UIColors.OverlayLight; } }

            /// <summary>
            /// Replaces the blue accent used throughout every built-in
            /// control (buttons, toggle switches, selection highlights,
            /// ...) with shades computed from a single color. Set once, as
            /// early as possible - before building any UI - same as
            /// <see cref="Language"/>; a couple of controls only read their
            /// accent color once at construction time, so they won't
            /// retroactively pick up a change made after they're built.
            /// </summary>
            public static void SetAccent(Color accent)
            {
                UIColors.SetAccent(accent);
            }

            /// <summary>
            /// Switches the base theme (backgrounds/text/borders), e.g.
            /// UIThemes.Light to move off the dark default. Same "call once,
            /// before building any UI" caveat as <see cref="SetAccent"/>. Combine
            /// freely with SetAccent - the two are independent (e.g. a light
            /// theme with a purple accent).
            /// </summary>
            public static void ApplyTheme(UIColorTheme theme)
            {
                UIColors.ApplyTheme(theme);
            }
        }

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

        /// <summary>A themed <see cref="System.Windows.Forms.DataGridView"/> for data bound via <see cref="System.Windows.Forms.DataGridView.DataSource"/> - the control to reach for when rows come from a bound source rather than being added by hand.</summary>
        public static class DataGridViews
        {
            /// <summary>The editable variant - row adding/deleting, an optional delete-row column, clipboard cut/copy/paste, and a right-click context menu. Use <see cref="CreateReadOnlyStandard"/> instead for a grid that only ever shows data.</summary>
            /// <param name="dataSource">The data to bind, or none.</param>
            /// <param name="colors">
            /// A color set to frame and highlight the grid in instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>. The border, the selection, the column drag line and the
            /// context menu's selection then use its color.
            /// </param>
            public static System.Windows.Forms.DataGridView CreateStandard(object dataSource = null, UIColorSet colors = null)
            {
                return UIDataGridViewFactory.CreateStandard(dataSource, colors);
            }

            /// <summary>The display-only variant - column-header sorting, but no editing. Use <see cref="CreateStandard"/> instead for a grid that needs to be editable.</summary>
            /// <param name="dataSource">The data to bind, or none.</param>
            /// <param name="colors">A color set for the border, selection and menu instead of the neutral gray; see <see cref="CreateStandard"/>.</param>
            public static System.Windows.Forms.DataGridView CreateReadOnlyStandard(object dataSource = null, UIColorSet colors = null)
            {
                return UIDataGridViewFactory.CreateReadOnlyStandard(dataSource, colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but framed in the current accent color instead of the fixed neutral border - mirrors <see cref="ListViews.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.DataGridView CreatePrimary(object dataSource = null)
            {
                return UIDataGridViewFactory.CreatePrimary(dataSource);
            }

            /// <summary>Like <see cref="CreateReadOnlyStandard"/> but framed in the current accent color instead of the fixed neutral border - the read-only counterpart to <see cref="CreatePrimary"/>.</summary>
            public static System.Windows.Forms.DataGridView CreateReadOnlyPrimary(object dataSource = null)
            {
                return UIDataGridViewFactory.CreateReadOnlyPrimary(dataSource);
            }
        }

        /// <summary>Themed <see cref="Label"/>s: <see cref="CreateTitle"/> (bold/large), <see cref="CreateNormal"/> (body text), <see cref="CreateMuted"/> (de-emphasized).</summary>
        public static class Labels
        {
            public static Label CreateTitle(string text = "")
            {
                return UILabelFactory.CreateTitle(text);
            }

            public static Label CreateNormal(string text = "")
            {
                return UILabelFactory.CreateNormal(text);
            }

            public static Label CreateMuted(string text = "")
            {
                return UILabelFactory.CreateMuted(text);
            }
        }

        /// <summary>Themed <see cref="TextBox"/>es.</summary>
        public static class TextBoxes
        {
            public static TextBox CreateStandard(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateStandard(
                    text,
                    placeholder);
            }

            public static TextBox CreateBorderstyleNone(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateBorderstyleNone(
                    text,
                    placeholder);
            }
        }

        /// <summary>A themed <see cref="ComboBox"/>: the closed box, the rows and the frame of the open list follow the theme (the scroll bars of the list are the system's dark ones in a dark theme).</summary>
        public static class ComboBoxes
        {
            /// <summary>A themed combo box; the selected (or hovered) row of the list is the neutral gray.</summary>
            /// <param name="comboBoxStyle">The standard WinForms style; a drop-down list the user only picks from by default.</param>
            /// <param name="colors">
            /// A color set for the selected (or hovered) row of the list instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>.
            /// </param>
            public static System.Windows.Forms.ComboBox CreateStandard(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList,
                UIColorSet colors = null)
            {
                return UIComboBoxFactory.CreateStandard(
                    comboBoxStyle,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selected row in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.ComboBox CreatePrimary(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
            {
                return UIComboBoxFactory.CreatePrimary(
                    comboBoxStyle);
            }

            /// <summary>
            /// Clears <paramref name="comboBox"/>'s items and repopulates it
            /// from <paramref name="items"/> - e.g. because a language switch
            /// changed the item text - restoring whatever was selected
            /// before by its POSITION in the list (not by value/identity, so
            /// this assumes the same N choices in the same order, just
            /// possibly relabeled). Saves callers from having to remember
            /// SelectedIndex and re-set it by hand every time they rebuild a
            /// combo box's items.
            /// </summary>
            public static void ReplaceItems(
                System.Windows.Forms.ComboBox comboBox,
                System.Collections.Generic.IEnumerable<object> items,
                bool keepSelection = true)
            {
                UIComboBoxFactory.ReplaceItems(
                    comboBox,
                    items,
                    keepSelection);
            }
        }

        /// <summary>Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards.</summary>
        public static class Panels
        {
            public static Panel CreateDark()
            {
                return UIPanelFactory.CreateDark();
            }

            public static Panel CreateMedium()
            {
                return UIPanelFactory.CreateMedium();
            }

            public static Panel CreateElevated()
            {
                return UIPanelFactory.CreateElevated();
            }

            public static Panel CreateTransparent()
            {
                return UIPanelFactory.CreateTransparent();
            }
            public static Panel CreatePrimary()
            {
                return UIPanelFactory.CreatePrimary();
            }
        }

        /// <summary>
        /// Themed <see cref="CheckBox"/>es, drawn by the library: <see cref="CreateStandard"/> (neutral
        /// gray when checked) and <see cref="CreatePrimary"/> (the accent color), each also as a
        /// label-less <see cref="CreateStandardCompact"/> / <see cref="CreatePrimaryCompact"/> for table
        /// cells. A color set can be given to any of the standard ones. Hover, pressed, keyboard
        /// focus, disabled and the undetermined third state (<c>ThreeState</c>) are built in.
        /// </summary>
        public static class CheckBoxes
        {
            /// <param name="text">The label next to the box.</param>
            /// <param name="isChecked">Whether the box starts checked.</param>
            /// <param name="colors">
            /// The colors of the filled box (rest, hovered, pressed), for example one of
            /// <c>UIStyles.Colors.Sets</c>; left out, the neutral gray. The mark and the
            /// hover and pressed looks follow from the set.
            /// </param>
            public static System.Windows.Forms.CheckBox CreateStandard(
                string text = "",
                bool isChecked = true,
                UIColorSet colors = null)
            {
                return UICheckBoxFactory.CreateStandard(
                    text,
                    isChecked,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the box filled in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.CheckBox CreatePrimary(
                string text = "",
                bool isChecked = true)
            {
                return UICheckBoxFactory.CreatePrimary(
                    text,
                    isChecked);
            }

            /// <summary>A box without a label, 25 x 25 (for table cells and the like).</summary>
            /// <inheritdoc cref="CreateStandard"/>
            public static System.Windows.Forms.CheckBox CreateStandardCompact(
                bool isChecked = true,
                UIColorSet colors = null)
            {
                return UICheckBoxFactory.CreateStandardCompact(
                    isChecked,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandardCompact"/> but with the box filled in the current accent instead of the neutral gray - the compact counterpart to <see cref="CreatePrimary"/>.</summary>
            public static System.Windows.Forms.CheckBox CreatePrimaryCompact(
                bool isChecked = true)
            {
                return UICheckBoxFactory.CreatePrimaryCompact(
                    isChecked);
            }
        }


        /// <summary>iOS-style on/off <see cref="Controls.ToggleSwitch"/>es in three sizes. Colors can be overridden per-instance - see <see cref="Controls.ToggleSwitch.CheckedBackColor"/>.</summary>
        public static class ToggleSwitches
        {
            public static ToggleSwitch CreateStandard(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateStandard(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateSmall(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateSmall(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateLarge(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateLarge(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }
        }
        /// <summary>Themed <see cref="TableLayoutPanel"/>s, pre-sized to the given column/row count.</summary>
        public static class TableLayoutPanels
        {
            public static TableLayoutPanel CreateStandard(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateStandard(
                    columnCount,
                    rowCount);
            }

            public static TableLayoutPanel CreateDark(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateDark(
                    columnCount,
                    rowCount);
            }
        }

        /// <summary>Creates a plain <see cref="ToolTip"/> component - note it isn't owned by any control, so dispose it yourself if you're not letting it live for the app's whole lifetime.</summary>
        public static class ToolTips
        {
            public static ToolTip CreateToolTip(
                string text = "")
            {
                return UIToolTipFactory.CreateToolTip(text);
            }
        }

        /// <summary>A small, borderless popup with extra info next to a control or the mouse: blocks of an optional header and an optional text. Set its content, then show it. Reuse one popup for many show calls.</summary>
        public static class InfoPopups
        {
            /// <summary>A popup in the neutral gray.</summary>
            /// <param name="colors">
            /// A color set for the background and the text instead of the neutral gray: one of
            /// <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>.
            /// </param>
            /// <param name="fixedSize">A client size the popup keeps whatever its content, with the content centered (a short value that changes while shown, like a percentage over a slider); none sizes it to the content.</param>
            /// <param name="transparency">How see-through the popup is; medium by default.</param>
            /// <param name="duration">How long the popup stays before it hides itself (3, 5 or 8 seconds); medium by default. Every show call starts the time again.</param>
            /// <param name="delay">How long a delayed show (hover) waits before the popup appears: not at all, 0.2, 0.4 (the default) or 0.8 seconds.</param>
            public static Forms.InfoPopupForm CreateStandard(UIColorSet colors = null, Size? fixedSize = null, Forms.PopupTransparency transparency = Forms.PopupTransparency.Medium, Forms.PopupDuration duration = Forms.PopupDuration.Medium, Forms.PopupDelay delay = Forms.PopupDelay.Medium)
            {
                return UIInfoPopupFactory.CreateStandard(colors, fixedSize, transparency, duration, delay);
            }

            /// <summary>Like <see cref="CreateStandard"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static Forms.InfoPopupForm CreatePrimary(Size? fixedSize = null, Forms.PopupTransparency transparency = Forms.PopupTransparency.Medium, Forms.PopupDuration duration = Forms.PopupDuration.Medium, Forms.PopupDelay delay = Forms.PopupDelay.Medium)
            {
                return UIInfoPopupFactory.CreatePrimary(fixedSize, transparency, duration, delay);
            }
        }

        /// <summary>A <see cref="Controls.ContextMenuStrip"/> themed to match the rest of ErikwnkWFUI instead of the plain OS-default popup menu - functionally identical to the native control.</summary>
        public static class ContextMenuStrips
        {
            /// <summary>A themed context menu; the hovered item is the neutral gray.</summary>
            /// <param name="colors">
            /// A color set for the highlighted item (hover) and the item with an open submenu
            /// (pressed) instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>,
            /// <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>.
            /// </param>
            public static Controls.ContextMenuStrip CreateStandard(UIColorSet colors = null)
            {
                return UIContextMenuStripFactory.CreateStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the highlight in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static Controls.ContextMenuStrip CreatePrimary()
            {
                return UIContextMenuStripFactory.CreatePrimary();
            }
        }

        /// <summary>A dark-themed, multi-column <see cref="Controls.ListView"/> (Details view) with spreadsheet-style cell-range selection, hand-rolled column reordering/resizing, and Ctrl+C/Ctrl+Shift+C copy - see the class itself for the full behavior.</summary>
        public static class ListViews
        {
            /// <summary>The themed list view in neutral gray.</summary>
            /// <param name="colors">
            /// A color set to frame and highlight the list in instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>. The border, the selection, the column drag line and the
            /// context menu's selection then use its color.
            /// </param>
            public static System.Windows.Forms.ListView CreateStandard(UIColorSet colors = null)
            {
                return UIListViewFactory.CreateStandard(colors);
            }

            public static System.Windows.Forms.ListView CreatePrimary()
            {
                return UIListViewFactory.CreatePrimary();
            }
        }

        /// <summary>A themed, owner-drawn, optionally drag-reorderable <see cref="Controls.ListBox"/> - a heavier alternative to the native ListBox with icons, custom item colors, and enumeration support.</summary>
        public static class ListBoxes
        {
            public static Controls.ListBox CreateStandard(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreateStandard(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }

            /// <summary>Like <see cref="CreateStandard"/> but selection/drag-indicator-colored in the current accent instead of the fixed neutral gray - mirrors <see cref="ListViews.CreatePrimary"/>.</summary>
            public static Controls.ListBox CreatePrimary(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreatePrimary(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }
        }

        /// <summary>A themed <see cref="Controls.TabControl"/> - behaves like the standard one, drawn in the library's own look.</summary>
        public static class TabControls
        {
            /// <summary>The editable variant - right-click a tab to add, rename or close tabs. Use <see cref="CreateReadOnlyStandard"/> instead for tabs the user cannot edit.</summary>
            /// <param name="colors">
            /// A color set for the bar under the selected tab and the selection of the
            /// right-click menu instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>,
            /// <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>.
            /// </param>
            public static Controls.TabControl CreateStandard(UIColorSet colors = null)
            {
                return UITabControlFactory.CreateStandard(colors);
            }

            /// <summary>The display-only variant - tabs can be switched, not added, renamed or closed. Use <see cref="CreateStandard"/> instead for editable tabs.</summary>
            /// <param name="colors">A color set for the bar under the selected tab instead of the neutral gray; see <see cref="CreateStandard"/>.</param>
            public static Controls.ReadOnlyTabControl CreateReadOnlyStandard(UIColorSet colors = null)
            {
                return UITabControlFactory.CreateReadOnlyStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selected tab's bar and the menu selection in the current accent instead of neutral gray - mirrors <see cref="DataGridViews.CreatePrimary"/>.</summary>
            public static Controls.TabControl CreatePrimary()
            {
                return UITabControlFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreateReadOnlyStandard"/> but with the selected tab's bar in the current accent - the read-only counterpart to <see cref="CreatePrimary"/>.</summary>
            public static Controls.ReadOnlyTabControl CreateReadOnlyPrimary()
            {
                return UITabControlFactory.CreateReadOnlyPrimary();
            }
        }

        /// <summary>A <see cref="Controls.ListBox"/> wrapped with an optional header bar - see <see cref="ListBoxes"/> for the list box on its own.</summary>
        public static class ListBoxControls
        {
            public static ListBoxControl CreateStandard(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UIListBoxControlFactory.CreateStandard(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }

            /// <summary>Like <see cref="CreateStandard"/> but selection/drag-indicator-colored in the current accent instead of the fixed neutral gray - mirrors <see cref="ListBoxes.CreatePrimary"/>.</summary>
            public static ListBoxControl CreatePrimary(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UIListBoxControlFactory.CreatePrimary(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }
        }

        /// <summary>A themed key/value <see cref="Controls.PropertyTable"/> (label + editor per row, grouped into sections) - typical use is a settings/options panel.</summary>
        public static class PropertyTables
        {
            public static PropertyTable CreateStandard()
            {
                return UIPropertyTableFactory.CreateStandard();
            }
        }

        /// <summary>A themed <see cref="NumericUpDown"/>. Note: the up/down spinner buttons themselves stay system-colored - only the field's own colors follow the theme.</summary>
        public static class NumericUpDowns
        {
            public static NumericUpDown CreateStandard(
                decimal minimum = 0,
                decimal maximum = 100,
                decimal increment = 1,
                decimal value = 0)
            {
                return UINumericUpDownFactory.CreateStandard(
                    minimum,
                    maximum,
                    increment,
                    value);
            }
        }

        /// <summary>A plain, transparent <see cref="FlowLayoutPanel"/> with no padding/margin - a layout helper, not itself themed.</summary>
        public static class FlowLayoutPanels
        {
            public static FlowLayoutPanel CreateStandard()
            {
                return UIFlowLayoutPanelFactory.CreateStandard();
            }
        }

        /// <summary>A themed <see cref="ProgressBar"/> (continuous style, not the native blocky one).</summary>
        public static class ProgressBars
        {
            /// <summary>Neutral grey fill by default; set <see cref="Control.ForeColor"/> for any other colour.</summary>
            public static ProgressBar CreateStandard()
            {
                return UIProgressBarFactory.CreateStandard();
            }

            /// <summary>Like <see cref="CreateStandard"/> but without the border, and BackColor left for you to match your own panel.</summary>
            public static ProgressBar CreateStandardTransparent()
            {
                return UIProgressBarFactory.CreateStandardTransparent();
            }

            /// <summary>Like <see cref="CreateStandard"/> but a fixed green fill.</summary>
            public static ProgressBar CreateGreen()
            {
                return UIProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but without the border, and BackColor left for you to match your own panel - the closest a ProgressBar can get to a transparent background.</summary>
            public static ProgressBar CreateGreenTransparent()
            {
                return UIProgressBarFactory.CreateGreenTransparent();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static ProgressBar CreatePrimary()
            {
                return UIProgressBarFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreatePrimary"/> but without the border, and BackColor left for you to match your own panel - the accent-following equivalent of <see cref="CreateGreenTransparent"/>.</summary>
            public static ProgressBar CreatePrimaryTransparent()
            {
                return UIProgressBarFactory.CreatePrimaryTransparent();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static ProgressBar CreateStatus()
            {
                return UIProgressBarFactory.CreateStatus();
            }

            /// <summary>Like <see cref="CreateStatus"/> but without the border, and BackColor left for you to match your own panel - the status-gradient equivalent of <see cref="CreateGreenTransparent"/>.</summary>
            public static ProgressBar CreateStatusTransparent()
            {
                return UIProgressBarFactory.CreateStatusTransparent();
            }
        }

        /// <summary>
        /// A thin, fully custom-drawn <see cref="SlimProgressBar"/> - no
        /// native Win32 control underneath (unlike <see cref="ProgressBars"/>),
        /// so no border/theming quirks to work around. For slim status-strip-
        /// style progress indicators rather than a prominent, full-size bar.
        /// </summary>
        public static class SlimProgressBars
        {
            /// <summary>Neutral grey fill by default; set <see cref="Control.ForeColor"/> for any other colour.</summary>
            public static SlimProgressBar CreateStandard()
            {
                return UISlimProgressBarFactory.CreateStandard();
            }

            /// <summary>Like <see cref="CreateStandard"/> but a fixed green fill.</summary>
            public static SlimProgressBar CreateGreen()
            {
                return UISlimProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static SlimProgressBar CreatePrimary()
            {
                return UISlimProgressBarFactory.CreatePrimary();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static SlimProgressBar CreateStatus()
            {
                return UISlimProgressBarFactory.CreateStatus();
            }
        }

        /// <summary>A slim, theme-matching horizontal <see cref="Controls.SliderBar"/> (WinForms has none of its own).</summary>
        public static class SliderBars
        {
            /// <summary>A fixed neutral-grey slider. Any other colour: set <see cref="SliderBar.FillColor"/> on one of these.</summary>
            public static SliderBar CreateStandard(double value = 0, double maximum = 1.0)
            {
                return UISliderBarFactory.CreateStandard(value, maximum);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static SliderBar CreatePrimary(double value = 0, double maximum = 1.0)
            {
                return UISliderBarFactory.CreatePrimary(value, maximum);
            }
        }

        /// <summary>A <see cref="Controls.VolumeSlider"/> - a <see cref="Controls.SliderBar"/> fixed to 0..1 that shows the current percentage in a small popup while dragged.</summary>
        public static class VolumeSliders
        {
            /// <summary>A fixed neutral-grey volume slider. Any other colour: set <see cref="SliderBar.FillColor"/> on one of these.</summary>
            public static VolumeSlider CreateStandard(double value = 0)
            {
                return UIVolumeSliderFactory.CreateStandard(value);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static VolumeSlider CreatePrimary(double value = 0)
            {
                return UIVolumeSliderFactory.CreatePrimary(value);
            }
        }

        /// <summary>
        /// A rotating <see cref="Controls.Spinner"/> for "loading …" states (only
        /// animates while visible). Plain variants are neutral grey, fixed green,
        /// or the theme accent; the <c>CreateProgress*</c> versions also show a
        /// percentage in the centre - raise <see cref="Spinner.Progress"/> as the
        /// load advances - and <see cref="CreateProgressStatus"/> additionally
        /// blends the arc red → yellow → green by that value, like
        /// <see cref="SlimProgressBars"/>.
        /// </summary>
        public static class Spinners
        {
            /// <summary>A neutral-grey spinner; set <see cref="Spinner.ArcColor"/> for any other colour.</summary>
            /// <param name="size">Width and height in px (the spinner is always square).</param>
            /// <param name="thickness">Stroke width in px; 0 scales it to <paramref name="size"/>.</param>
            public static Spinner CreateStandard(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateStandard(size, thickness);
            }

            /// <summary>A fixed green spinner.</summary>
            public static Spinner CreateGreen(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateGreen(size, thickness);
            }

            /// <summary>The arc follows the theme accent.</summary>
            public static Spinner CreatePrimary(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreatePrimary(size, thickness);
            }

            /// <summary>A grey spinner that also shows a percentage in the centre (starts at 0); set <see cref="Spinner.ArcColor"/> for any other colour.</summary>
            public static Spinner CreateProgressStandard(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressStandard(size, thickness);
            }

            /// <summary>A green spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressGreen(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressGreen(size, thickness);
            }

            /// <summary>An accent spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressPrimary(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressPrimary(size, thickness);
            }

            /// <summary>Shows a percentage in the centre; the arc is a solid colour blended red (0) → yellow → green (100) by that value.</summary>
            public static Spinner CreateProgressStatus(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressStatus(size, thickness);
            }
        }

        /// <summary>
        /// ErikwnkWFUI's own bundled icons (Web/Folder/Document/Application),
        /// plus <see cref="LoadEmbedded"/> for loading your own app's icon
        /// from its embedded resources the same way - see
        /// ErikwnkWFUI/README.md for why that's preferable to a loose file.
        /// </summary>
        public static class Icons
        {
            public static Image Web { get { return UIIcons.Web; } }
            public static Image Folder { get { return UIIcons.Folder; } }
            public static Image Document { get { return UIIcons.Document; } }
            public static Image Application { get { return UIIcons.Application; } }

            public static Image LoadEmbedded(Assembly assembly, string resourceName)
            {
                return UIIcons.LoadEmbedded(assembly, resourceName);
            }
        }
    }

}
