using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

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
        public static partial class Buttons
        {
            /// <summary>The neutral button.</summary>
            /// <param name="text">The caption.</param>
            /// <param name="tooltip">Tooltip text; empty for none.</param>
            /// <param name="size">The size; 30 x 30 when left out.</param>
            /// <param name="isIcon">A special case: the button keeps the text you give it but is drawn in the symbol font and without inner padding, for a glyph that is part of a normal button. For a button that only shows a symbol use <c>CreateSymbol</c> instead - it centers the symbol exactly, grows it with the height of the button and has ready-made designs.</param>
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
            /// <param name="isIcon">A special case: the button keeps the text you give it but is drawn in the symbol font and without inner padding, for a glyph that is part of a normal button. For a button that only shows a symbol use <c>CreateSymbol</c> instead - it centers the symbol exactly, grows it with the height of the button and has ready-made designs.</param>
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
            /// <param name="isIcon">A special case: the button keeps the text you give it but is drawn in the symbol font and without inner padding, for a glyph that is part of a normal button. For a button that only shows a symbol use <c>CreateSymbol</c> instead - it centers the symbol exactly, grows it with the height of the button and has ready-made designs.</param>
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
            /// <param name="isIcon">A special case: the button keeps the text you give it but is drawn in the symbol font and without inner padding, for a glyph that is part of a normal button. For a button that only shows a symbol use <c>CreateSymbol</c> instead - it centers the symbol exactly, grows it with the height of the button and has ready-made designs.</param>
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
            /// A button that switches the theme, and does all of it by itself: it shows the symbol of the theme a click leads to (the sun
            /// for the light one, the moon for the dark one), switches the theme with <c>UIColors.ApplyTheme</c> when it is clicked, and
            /// shows the new state - also when the theme is changed by anything else (the options window, your own code). Its tooltip
            /// and its name for the screen reader say what a click does, in the language of the library, and follow a change of it. It
            /// takes no focus, like a button of a tool bar. Add a handler to <c>Click</c> (it runs after the theme has been switched) to
            /// keep the choice, say <c>UIAppearance.Capture().Save(path)</c>.
            /// </summary>
            /// <param name="size">The size of the button; the usual one of a symbol button when left out.</param>
            /// <param name="colors">A color set for the look of the button instead of the neutral one: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or a <c>Color</c>.</param>
            public static Button CreateThemeToggleStandard(Size? size = null, UIColorSet colors = null)
            {
                return UIButtonFactory.CreateThemeToggle(size, ColorSetSource.Of(colors));
            }

            /// <summary>The old name of <see cref="CreateThemeToggleStandard"/>; it is removed once the projects that use it have been changed.</summary>
            [System.Obsolete("Use CreateThemeToggleStandard (the same button; the name of the standard one, as in every factory).")]
            public static Button CreateThemeToggle(Size? size = null, UIColorSet colors = null)
            {
                return CreateThemeToggleStandard(size, colors);
            }

            /// <summary>The button that switches the theme (see <see cref="CreateThemeToggleStandard"/>) in the accent color (it follows a change of the accent).</summary>
            /// <param name="size">The size of the button; the usual one of a symbol button when left out.</param>
            public static Button CreateThemeTogglePrimary(Size? size = null)
            {
                return UIButtonFactory.CreateThemeToggle(size, () => UIColors.PrimarySet);
            }

            /// <summary>
            /// Changes the tooltip text of a button made by this class, for example
            /// after a language switch. A button created without a tooltip gets one with
            /// the first text that is not empty.
            /// </summary>
            public static void UpdateTooltip(Button button, string tooltip)
            {
                UIButtonFactory.UpdateTooltip(button, tooltip);
            }
        }
    }
}
