using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIButtonFactory
    {
        private static readonly Size DefaultButtonSize = new Size(30, 30);

        // 13 pt symbols on a 30 px button (UIFonts.Icon), scaled with the height.
        private const float SymbolPointsPerPixel = 13f / 30f;

        // The outline of every button except Standard. A fixed color, so
        // they look the same on both themes (only Standard follows the theme).
        private static readonly Color FixedBorderColor = Color.FromArgb(50, 50, 50);

        // The tooltip each button got, so UpdateTooltip can change its text
        // later (after a language switch, say).
        private static readonly ConditionalWeakTable<Button, ToolTip> ToolTipsByButton =
            new ConditionalWeakTable<Button, ToolTip>();

        public static Button CreateStandard(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStandard(text, tooltip, size, isIcon, null);
        }

        private static Button CreateStandard(string text, string tooltip, Size? size, bool isIcon, Button instance)
        {
            return CreateButton(text, tooltip, size, isIcon, () => new ButtonLook(
                UIColors.BackgroundMedium, UIColors.TextPrimary, GetStandardBorderColor(), 1,
                UIColors.BackgroundLighter, UIColors.Primary), instance);
        }

        // The one place a symbol button is made - UIStyles.Buttons.CreateSymbol
        // (with a preset or with a symbol and colors) ends up here.
        //
        // - Look: the colors of the set, or the neutral look when there is none.
        // - Symbol: drawn by SymbolButton, centered on what is actually visible of
        //   the glyph. The font grows with the button: 13 pt at the standard 30 px
        //   height, the size of UIFonts.Icon, proportionally larger or smaller
        //   otherwise - and a bit smaller for the symbols that fill their whole cell
        //   (UIStyles.Symbols.GetSizeFactor). Symbols from the Windows icon font get
        //   that font instead of the usual one (UIStyles.Symbols.GetFontName decides).
        public static Button CreateSymbol(string symbol, string tooltip, Size? size, UIColorSet colors)
        {
            Size buttonSize = size ?? DefaultButtonSize;

            // Its own kind of button: it draws the symbol itself, centered on
            // what is actually visible of it (see SymbolButton).
            SymbolButton instance = new SymbolButton();

            Button button = colors != null
                ? CreateColored(symbol, tooltip, buttonSize, true, () => colors, instance, 1)
                : CreateStandard(symbol, tooltip, buttonSize, true, instance);

            float points = buttonSize.Height * SymbolPointsPerPixel * UIStyles.Symbols.GetSizeFactor(symbol);
            string family = UIStyles.Symbols.GetFontName(symbol);

            // At the standard size, in the usual symbol font and at full size, it is
            // the shared icon font already (and a font equal to the current one would
            // not be taken over anyway).
            if (family == UIFonts.Icon.FontFamily.Name && Math.Abs(points - UIFonts.Icon.Size) < 0.01f)
                return button;

            Font font = new Font(family, points);
            button.Font = font;
            button.Disposed += delegate { font.Dispose(); };

            return button;
        }

        // A button in the colors of a set: the fill and the text for every
        // state come from the set. The outline is the fixed one, so it looks
        // the same on both themes.
        public static Button CreateColored(string text, string tooltip, Size? size, bool isIcon, UIColorSet colors)
        {
            return CreateColored(text, tooltip, size, isIcon, () => colors, null, 1);
        }

        // The set is asked for again whenever the theme or the accent changes (the primary set follows the accent).
        private static Button CreateColored(string text, string tooltip, Size? size, bool isIcon, Func<UIColorSet> getColors, Button instance, int borderSize)
        {
            return CreateButton(text, tooltip, size, isIcon, () =>
            {
                UIColorSet colors = getColors();

                return new ButtonLook(
                    colors.Basic, colors.BasicText, FixedBorderColor, borderSize,
                    colors.Hover, colors.Pressed,
                    hoverForeColor: colors.HoverText,
                    pressedForeColor: colors.PressedText,
                    disabledBackColor: colors.Disabled,
                    disabledForeColor: colors.DisabledText);
            }, instance);
        }

        // On a dark theme the fill is told apart by a quiet outline. A light
        // theme needs a stronger one, or the button melts into the page.
        private static Color GetStandardBorderColor()
        {
            return UIColors.IsLight(UIColors.BackgroundMedium) ? UIColors.BorderLight : UIColors.BorderDark;
        }

        // Primary, Green and Red are the color sets of the same name, so their
        // text on every state (and the disabled look) is worked out by the set -
        // the same as CreateStandard(colors: ...) - and follows the accent for
        // Primary, even a bright one.
        public static Button CreatePrimary(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateColored(text, tooltip, size, isIcon, () => UIColors.PrimarySet, null, 0);
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateColored(text, tooltip, size, isIcon, () => UIStyles.Colors.Sets.Green, null, 1);
        }

        public static Button CreateRed(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateColored(text, tooltip, size, isIcon, () => UIStyles.Colors.Sets.Red, null, 1);
        }

        private static Button CreateButton(string text, string tooltip, Size? size, bool isIcon, Func<ButtonLook> getLook, Button instance = null)
        {
            Button button = instance ?? new Button();
            ButtonLook look = getLook();

            button.Text = text ?? "";
            button.Size = size ?? DefaultButtonSize;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = look.BackColor;
            button.ForeColor = look.ForeColor;
            button.Font = isIcon ? UIFonts.Icon : UIFonts.Normal;
            button.TabStop = true;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(0);
            button.Padding = isIcon ? new Padding(0) : new Padding(6, 0, 6, 0);
            button.TextAlign = ContentAlignment.MiddleCenter;

            button.FlatAppearance.BorderSize = look.BorderSize;
            button.FlatAppearance.BorderColor = look.BorderColor;
            button.FlatAppearance.MouseOverBackColor = look.MouseOverBackColor;
            button.FlatAppearance.MouseDownBackColor = look.MouseDownBackColor;

            ButtonLookBehavior.Attach(button, getLook);
            AddToolTip(button, tooltip);

            // A button that shows only a symbol is read out as that character by a
            // screen reader; what it does is in the tooltip.
            if (isIcon && !string.IsNullOrWhiteSpace(tooltip))
                button.AccessibleName = tooltip;

            button.Paint += OnButtonPaint;

            return button;
        }

        // The framework paints the text of a disabled button in its own embossed
        // colors and ignores ForeColor, which comes out barely readable. So the
        // text is drawn once more on top, in the muted color SetEnabledStyle
        // chose. Enabled buttons draw fine on their own.
        private static void OnButtonPaint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;

            if (button is SymbolButton || button.Enabled || string.IsNullOrEmpty(button.Text))
                return;

            Rectangle textArea = Rectangle.Inflate(button.ClientRectangle, -2, -2);

            using (SolidBrush backBrush = new SolidBrush(button.BackColor))
                e.Graphics.FillRectangle(backBrush, textArea);

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            // A button that does not use mnemonics shows "&" as it is.
            if (!button.UseMnemonic)
                flags |= TextFormatFlags.NoPrefix;

            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                button.ClientRectangle,
                button.ForeColor,
                flags);
        }

        private static void AddToolTip(Button button, string tooltip)
        {
            if (button == null || string.IsNullOrWhiteSpace(tooltip))
                return;

            ToolTip toolTip = UIToolTipFactory.CreateToolTip();
            toolTip.SetToolTip(button, tooltip);
            toolTip.ReviveOnFormActivate(button);
            ToolTipsByButton.Add(button, toolTip);

            button.Disposed += delegate
            {
                toolTip.Dispose();
            };
        }

        /// <summary>
        /// Changes an already-created button's tooltip text - e.g. to
        /// re-translate it after a language switch. No-op if the button was
        /// created without a tooltip in the first place.
        /// </summary>
        public static void UpdateTooltip(Button button, string tooltip)
        {
            if (button == null)
                return;

            ToolTip toolTip;
            if (!ToolTipsByButton.TryGetValue(button, out toolTip))
                return;

            // The accessible name is the tooltip for a symbol button: it follows.
            if (button.AccessibleName == toolTip.GetToolTip(button))
                button.AccessibleName = tooltip ?? "";

            toolTip.SetToolTip(button, tooltip ?? "");
        }
    }
}