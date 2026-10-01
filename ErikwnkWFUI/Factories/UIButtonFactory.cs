using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIButtonFactory
    {
        private static readonly Size DefaultButtonSize = new Size(30, 30);
        private const double DisabledColorFactor = 0.65;

        // A light fill (Standard on a light theme) only dims a little when
        // disabled: darkening it as far as a dark fill would make a heavy
        // gray block that stands out more than the enabled button.
        private const double LightDisabledColorFactor = 0.9;

        // The outline of every button except Standard. A fixed color, so
        // they look the same on both themes (only Standard follows the theme).
        private static readonly Color FixedBorderColor = Color.FromArgb(50, 50, 50);

        // The tooltip each button got, so UpdateTooltip can change its text
        // later (after a language switch, say).
        private static readonly ConditionalWeakTable<Button, ToolTip> _tooltips =
            new ConditionalWeakTable<Button, ToolTip>();

        public static Button CreateStandard(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon,
                UIColors.BackgroundMedium, UIColors.TextPrimary, GetStandardBorderColor(), 1,
                UIColors.BackgroundLighter, UIColors.Primary);
        }

        // On a dark theme the fill is told apart by a quiet outline. A light
        // theme needs a stronger one, or the button melts into the page.
        private static Color GetStandardBorderColor()
        {
            return UIColors.IsLight(UIColors.BackgroundMedium) ? UIColors.BorderLight : UIColors.BorderDark;
        }

        public static Button CreatePrimary(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon,
                UIColors.PrimaryDark, UIColors.GetContrastingForeColor(UIColors.PrimaryDark), FixedBorderColor, 0,
                UIColors.Primary, UIColors.PrimaryLight);
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon,
                UIColors.GreenDark, UIColors.GetContrastingForeColor(UIColors.GreenDark), FixedBorderColor, 1,
                UIColors.Green, UIColors.GreenLight);
        }

        public static Button CreateRed(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon,
                UIColors.RedDark, UIColors.GetContrastingForeColor(UIColors.RedDark), FixedBorderColor, 1,
                UIColors.Red, UIColors.RedLight);
        }

        public static Button CreateBrowse(string tooltip = "", Size? size = null, bool isIcon = true)
        {
            // A font glyph on purpose: the OpenFolder PNG scaled down to button size
            // looks blurry next to the flat yellow.
            //
            // The glyph stays white (fixedForeColor): this button keeps its yellow
            // in every theme, so its text color is fixed as well.
            return CreateButton("📁", tooltip, size, isIcon,
                UIColors.Yellow, UIColors.White, FixedBorderColor, 1,
                UIColors.YellowLight, UIColors.YellowLighter,
                fixedForeColor: true);
        }

        private static Button CreateButton(
            string text,
            string tooltip,
            Size? size,
            bool isIcon,
            Color backColor,
            Color foreColor,
            Color borderColor,
            int borderSize,
            Color mouseOverBackColor,
            Color mouseDownBackColor,
            bool fixedForeColor = false)
        {
            Button button = new Button
            {
                Text = text ?? "",
                Size = size ?? DefaultButtonSize,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = isIcon ? UIFonts.Icon : UIFonts.Normal,
                TabStop = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                Padding = isIcon ? new Padding(0) : new Padding(6, 0, 6, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            button.FlatAppearance.BorderSize = borderSize;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = mouseOverBackColor;
            button.FlatAppearance.MouseDownBackColor = mouseDownBackColor;

            SetEnabledStyle(button, backColor, foreColor, fixedForeColor);
            SetPressedForeColor(button, foreColor, fixedForeColor);
            AddToolTip(button, tooltip);

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

            if (button.Enabled || string.IsNullOrEmpty(button.Text))
                return;

            Rectangle textArea = Rectangle.Inflate(button.ClientRectangle, -2, -2);

            using (SolidBrush backBrush = new SolidBrush(button.BackColor))
                e.Graphics.FillRectangle(backBrush, textArea);

            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                button.ClientRectangle,
                button.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // FlatAppearance can only change the fill per mouse state, not the text
        // color. So while the button is pressed, its text gets whichever color
        // reads best on the pressed fill (dark or light, depending on the accent).
        //
        // Only a real press is undone on MouseLeave and MouseUp, and what comes
        // back is the color from just before the press - callers may recolor a
        // button after creating it. The pressed fill is read at press time, not
        // at creation, because TitleBarControl.RefreshTheme changes it later.
        private static void SetPressedForeColor(Button button, Color idleForeColor, bool fixedForeColor)
        {
            bool isPressed = false;
            Color restoreForeColor = idleForeColor;

            button.MouseDown += delegate
            {
                if (!button.Enabled)
                    return;

                restoreForeColor = button.ForeColor;
                isPressed = true;

                Color pressedForeColor = fixedForeColor
                    ? restoreForeColor
                    : UIColors.GetContrastingForeColor(button.FlatAppearance.MouseDownBackColor);

                if (restoreForeColor != pressedForeColor)
                    button.ForeColor = pressedForeColor;
            };

            button.MouseUp += delegate
            {
                if (!isPressed)
                    return;

                isPressed = false;
                button.ForeColor = restoreForeColor;
            };

            button.MouseLeave += delegate
            {
                if (!isPressed)
                    return;

                isPressed = false;
                button.ForeColor = restoreForeColor;
            };
        }

        private static void AddToolTip(Button button, string tooltip)
        {
            if (button == null || string.IsNullOrWhiteSpace(tooltip))
                return;

            ToolTip toolTip = UIToolTipFactory.CreateToolTip();
            toolTip.SetToolTip(button, tooltip);
            toolTip.ReviveOnFormActivate(button);
            _tooltips.Add(button, toolTip);

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
            if (_tooltips.TryGetValue(button, out toolTip))
                toolTip.SetToolTip(button, tooltip ?? "");
        }

        // Disabled means a darker fill and muted text. The colors in use right
        // before disabling are remembered and put back on enabling, so a
        // caller's own BackColor/ForeColor survives the round trip.
        private static void SetEnabledStyle(Button button, Color enabledBackColor, Color enabledForeColor, bool fixedForeColor)
        {
            bool lightFill = !fixedForeColor && UIColors.IsLight(enabledBackColor);
            Color disabledBackColor = Darken(enabledBackColor, lightFill ? LightDisabledColorFactor : DisabledColorFactor);

            // The text is the contrasting color blended 35% toward the disabled fill:
            // still readable, but visibly muted. One fixed gray did not work - on
            // Browse's yellow it came out at a contrast of about 1.0. Buttons with a
            // fixed text color (Browse) blend that color instead.
            Color disabledForeColor = fixedForeColor
                ? BlendTowardColor(enabledForeColor, disabledBackColor, 0.35)
                : BlendTowardColor(UIColors.GetContrastingForeColor(disabledBackColor), disabledBackColor, 0.35);

            Color restoreBackColor = enabledBackColor;
            Color restoreForeColor = enabledForeColor;

            button.EnabledChanged += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = restoreBackColor;
                    button.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreBackColor = button.BackColor;
                    restoreForeColor = button.ForeColor;
                    button.BackColor = disabledBackColor;
                    button.ForeColor = disabledForeColor;
                }
            };
        }

        private static Color Darken(Color color, double factor)
        {
            factor = Math.Max(0, Math.Min(1, factor));

            return Color.FromArgb(
                color.A,
                Math.Max(0, Math.Min(255, (int)(color.R * factor))),
                Math.Max(0, Math.Min(255, (int)(color.G * factor))),
                Math.Max(0, Math.Min(255, (int)(color.B * factor))));
        }

        private static Color BlendTowardColor(Color color, Color target, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));

            return Color.FromArgb(
                color.A,
                color.R + (int)((target.R - color.R) * amount),
                color.G + (int)((target.G - color.G) * amount),
                color.B + (int)((target.B - color.B) * amount));
        }
    }
}