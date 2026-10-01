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

        // How much darker a disabled fill gets. A light fill (Standard on a
        // light theme) only dims a little: darkening it as far as a dark fill
        // would make a heavy gray block that stands out more than the
        // enabled button.
        private const double DisabledDarkenAmount = 0.35;
        private const double LightDisabledDarkenAmount = 0.1;

        // The outline of every button except Standard. A fixed color, so
        // they look the same on both themes (only Standard follows the theme).
        private static readonly Color FixedBorderColor = Color.FromArgb(50, 50, 50);

        // Everything that makes up a button's look, so each variant is one
        // value instead of seven loose arguments.
        private readonly struct ButtonLook
        {
            public readonly Color BackColor;
            public readonly Color ForeColor;
            public readonly Color BorderColor;
            public readonly int BorderSize;
            public readonly Color MouseOverBackColor;
            public readonly Color MouseDownBackColor;

            // A fixed text color stays as it is instead of being picked for
            // contrast (Browse's white glyph).
            public readonly bool FixedForeColor;

            // Set when the look comes from a UIColorSet, which already knows
            // the text on each state and the disabled look. Left empty, the
            // button works them out as it always did.
            public readonly Color? HoverForeColor;
            public readonly Color? PressedForeColor;
            public readonly Color? DisabledBackColor;
            public readonly Color? DisabledForeColor;

            public ButtonLook(
                Color backColor,
                Color foreColor,
                Color borderColor,
                int borderSize,
                Color mouseOverBackColor,
                Color mouseDownBackColor,
                bool fixedForeColor = false,
                Color? hoverForeColor = null,
                Color? pressedForeColor = null,
                Color? disabledBackColor = null,
                Color? disabledForeColor = null)
            {
                BackColor = backColor;
                ForeColor = foreColor;
                BorderColor = borderColor;
                BorderSize = borderSize;
                MouseOverBackColor = mouseOverBackColor;
                MouseDownBackColor = mouseDownBackColor;
                FixedForeColor = fixedForeColor;
                HoverForeColor = hoverForeColor;
                PressedForeColor = pressedForeColor;
                DisabledBackColor = disabledBackColor;
                DisabledForeColor = disabledForeColor;
            }
        }

        // The tooltip each button got, so UpdateTooltip can change its text
        // later (after a language switch, say).
        private static readonly ConditionalWeakTable<Button, ToolTip> _tooltips =
            new ConditionalWeakTable<Button, ToolTip>();

        public static Button CreateStandard(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                UIColors.BackgroundMedium, UIColors.TextPrimary, GetStandardBorderColor(), 1,
                UIColors.BackgroundLighter, UIColors.Primary));
        }

        // A button in the colors of a set: the fill and the text for every
        // state come from the set. The outline is the fixed one, so it looks
        // the same on both themes.
        public static Button CreateColored(string text, string tooltip, Size? size, bool isIcon, UIColorSet colors)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                colors.Basic, colors.BasicText, FixedBorderColor, 1,
                colors.Hover, colors.Pressed,
                hoverForeColor: colors.HoverText,
                pressedForeColor: colors.PressedText,
                disabledBackColor: colors.Disabled,
                disabledForeColor: colors.DisabledText));
        }

        // On a dark theme the fill is told apart by a quiet outline. A light
        // theme needs a stronger one, or the button melts into the page.
        private static Color GetStandardBorderColor()
        {
            return UIColors.IsLight(UIColors.BackgroundMedium) ? UIColors.BorderLight : UIColors.BorderDark;
        }

        public static Button CreatePrimary(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                UIColors.PrimaryDark, UIColors.GetContrastingForeColor(UIColors.PrimaryDark), FixedBorderColor, 0,
                UIColors.Primary, UIColors.PrimaryLight));
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                UIColors.GreenDark, UIColors.GetContrastingForeColor(UIColors.GreenDark), FixedBorderColor, 1,
                UIColors.Green, UIColors.GreenLight));
        }

        public static Button CreateRed(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                UIColors.RedDark, UIColors.GetContrastingForeColor(UIColors.RedDark), FixedBorderColor, 1,
                UIColors.Red, UIColors.RedLight));
        }

        public static Button CreateBrowse(string tooltip = "", Size? size = null, bool isIcon = true)
        {
            // A font glyph on purpose: the OpenFolder PNG scaled down to button size
            // looks blurry next to the flat yellow.
            //
            // The glyph stays white (fixedForeColor): this button keeps its yellow
            // in every theme, so its text color is fixed as well.
            return CreateButton("📁", tooltip, size, isIcon, new ButtonLook(
                UIColors.Yellow, UIColors.White, FixedBorderColor, 1,
                UIColors.YellowLight, UIColors.YellowLighter,
                fixedForeColor: true));
        }

        private static Button CreateButton(string text, string tooltip, Size? size, bool isIcon, ButtonLook look)
        {
            Button button = new Button
            {
                Text = text ?? "",
                Size = size ?? DefaultButtonSize,
                FlatStyle = FlatStyle.Flat,
                BackColor = look.BackColor,
                ForeColor = look.ForeColor,
                Font = isIcon ? UIFonts.Icon : UIFonts.Normal,
                TabStop = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                Padding = isIcon ? new Padding(0) : new Padding(6, 0, 6, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            button.FlatAppearance.BorderSize = look.BorderSize;
            button.FlatAppearance.BorderColor = look.BorderColor;
            button.FlatAppearance.MouseOverBackColor = look.MouseOverBackColor;
            button.FlatAppearance.MouseDownBackColor = look.MouseDownBackColor;

            // Pressed before enabled: both listen to EnabledChanged, and the
            // pressed text color has to be undone before the enabled style
            // remembers the colors.
            SetStateForeColors(button, look);
            SetEnabledStyle(button, look);
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

        // FlatAppearance can only change the fill per mouse state, not the text
        // color. So the text color follows the state by hand: pressed gets
        // whichever color reads best on the pressed fill (dark or light,
        // depending on the accent), and a look from a UIColorSet also has its
        // own text color for hovered.
        //
        // What the text is "at rest" is read when the button leaves rest, not
        // at creation - callers may recolor a button after creating it - and
        // that is what comes back. The pressed fill is read at press time,
        // because TitleBarControl.RefreshTheme changes it later.
        private static void SetStateForeColors(Button button, ButtonLook look)
        {
            bool isHovered = false;
            bool isPressed = false;
            Color atRest = look.ForeColor;

            void Apply()
            {
                if (isPressed)
                {
                    button.ForeColor = look.FixedForeColor
                        ? atRest
                        : look.PressedForeColor ?? UIColors.GetContrastingForeColor(button.FlatAppearance.MouseDownBackColor);
                }
                else if (isHovered)
                {
                    button.ForeColor = look.HoverForeColor.Value;
                }
                else
                {
                    button.ForeColor = atRest;
                }
            }

            void LeaveRest()
            {
                if (!isHovered && !isPressed)
                    atRest = button.ForeColor;
            }

            void Press()
            {
                if (!button.Enabled || isPressed)
                    return;

                LeaveRest();
                isPressed = true;
                Apply();
            }

            void Release()
            {
                if (!isPressed)
                    return;

                isPressed = false;
                Apply();
            }

            void Enter()
            {
                if (!button.Enabled || isHovered || !look.HoverForeColor.HasValue)
                    return;

                LeaveRest();
                isHovered = true;
                Apply();
            }

            void Leave()
            {
                bool wasChanged = isHovered || isPressed;

                isHovered = false;
                isPressed = false;

                if (wasChanged)
                    Apply();
            }

            // Only the left button and the space bar press a button; the fill
            // changes for nothing else, so the text must not either.
            button.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Press();
            };

            button.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Space)
                    Press();
            };

            button.MouseUp += delegate { Release(); };
            button.KeyUp += (sender, e) =>
            {
                if (e.KeyCode == Keys.Space)
                    Release();
            };
            button.MouseEnter += delegate { Enter(); };
            button.MouseLeave += delegate { Leave(); };
            button.LostFocus += delegate { Release(); };

            // A Click handler that disables the button runs before MouseUp,
            // while the pressed color is still in place. SetEnabledStyle must
            // see the color at rest when it remembers one, so this has to be
            // hooked first (see CreateButton).
            button.EnabledChanged += delegate
            {
                if (!button.Enabled)
                    Leave();
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
        private static void SetEnabledStyle(Button button, ButtonLook look)
        {
            Color enabledBackColor = look.BackColor;
            Color enabledForeColor = look.ForeColor;
            bool fixedForeColor = look.FixedForeColor;

            bool lightFill = !fixedForeColor && UIColors.IsLight(enabledBackColor);
            Color disabledBackColor = look.DisabledBackColor
                ?? UIColors.Darken(enabledBackColor, lightFill ? LightDisabledDarkenAmount : DisabledDarkenAmount);

            // The text is the contrasting color blended 35% toward the disabled fill:
            // still readable, but visibly muted. One fixed gray did not work - on
            // Browse's yellow it came out at a contrast of about 1.0. Buttons with a
            // fixed text color (Browse) blend that color instead.
            Color disabledForeColor = look.DisabledForeColor
                ?? (fixedForeColor
                    ? UIColors.BlendToward(enabledForeColor, disabledBackColor, 0.35)
                    : UIColors.BlendToward(UIColors.GetContrastingForeColor(disabledBackColor), disabledBackColor, 0.35));

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
    }
}