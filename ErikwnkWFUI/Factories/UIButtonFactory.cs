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

        // 13 pt symbols on a 30 px button (UIFonts.Icon), scaled with the height.
        private const float SymbolPointsPerPixel = 13f / 30f;

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
                HoverForeColor = hoverForeColor;
                PressedForeColor = pressedForeColor;
                DisabledBackColor = disabledBackColor;
                DisabledForeColor = disabledForeColor;
            }
        }

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
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
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
                ? CreateColored(symbol, tooltip, buttonSize, true, colors, instance, 1)
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
            return CreateColored(text, tooltip, size, isIcon, colors, null, 1);
        }

        private static Button CreateColored(string text, string tooltip, Size? size, bool isIcon, UIColorSet colors, Button instance, int borderSize)
        {
            return CreateButton(text, tooltip, size, isIcon, new ButtonLook(
                colors.Basic, colors.BasicText, FixedBorderColor, borderSize,
                colors.Hover, colors.Pressed,
                hoverForeColor: colors.HoverText,
                pressedForeColor: colors.PressedText,
                disabledBackColor: colors.Disabled,
                disabledForeColor: colors.DisabledText), instance);
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
            return CreateColored(text, tooltip, size, isIcon, UIColors.PrimarySet, null, 0);
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateColored(text, tooltip, size, isIcon, UIStyles.Colors.Sets.Green, null, 1);
        }

        public static Button CreateRed(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateColored(text, tooltip, size, isIcon, UIStyles.Colors.Sets.Red, null, 1);
        }

        private static Button CreateButton(string text, string tooltip, Size? size, bool isIcon, ButtonLook look, Button instance = null)
        {
            Button button = instance ?? new Button();

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

            // Pressed before enabled: both listen to EnabledChanged, and the
            // pressed text color has to be undone before the enabled style
            // remembers the colors.
            SetStateForeColors(button, look);
            SetEnabledStyle(button, look);
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
                    button.ForeColor = look.PressedForeColor
                        ?? UIColors.GetContrastingForeColor(button.FlatAppearance.MouseDownBackColor);
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

        // Disabled means a darker fill and muted text. The colors in use right
        // before disabling are remembered and put back on enabling, so a
        // caller's own BackColor/ForeColor survives the round trip.
        private static void SetEnabledStyle(Button button, ButtonLook look)
        {
            Color enabledBackColor = look.BackColor;
            Color enabledForeColor = look.ForeColor;

            bool lightFill = UIColors.IsLight(enabledBackColor);
            Color disabledBackColor = look.DisabledBackColor
                ?? UIColors.Darken(enabledBackColor, lightFill ? LightDisabledDarkenAmount : DisabledDarkenAmount);

            // The text is the contrasting color blended 35% toward the disabled fill:
            // still readable, but visibly muted. One fixed gray did not work - on a
            // bright fill it came out at a contrast of about 1.0.
            Color disabledForeColor = look.DisabledForeColor
                ?? UIColors.BlendToward(UIColors.GetContrastingForeColor(disabledBackColor), disabledBackColor, 0.35);

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

        // A button for symbols. The native button centers the text by its
        // advance width and line height, which for symbol glyphs is not where
        // the glyph actually sits - they come out a pixel or several off, and
        // the more so the larger the button. So this one draws itself: fill,
        // outline and the symbol, moved so that the visible part of the glyph
        // is centered. Colors come from the same places as on any button
        // (BackColor, ForeColor, FlatAppearance), so the state handling of the
        // factory works unchanged.
        private sealed class SymbolButton : Button
        {
            private bool _isHot;
            private bool _isPressed;

            // Where the ink of the current text sits when the text is centered
            // by the framework - measured once per text, font and size.
            private Point _inkShift;
            private bool _isInkShiftKnown;

            public SymbolButton()
            {
                // Every paint covers the whole button with an opaque fill, so
                // nothing of a neighbor can show through.
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.Opaque |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _isHot = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _isHot = false;
                _isPressed = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                base.OnMouseDown(mevent);

                if (mevent.Button == MouseButtons.Left)
                    _isPressed = true;

                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                base.OnMouseUp(mevent);
                _isPressed = false;
                Invalidate();
            }

            protected override void OnKeyDown(KeyEventArgs kevent)
            {
                base.OnKeyDown(kevent);

                if (kevent.KeyCode == Keys.Space)
                {
                    _isPressed = true;
                    Invalidate();
                }
            }

            protected override void OnKeyUp(KeyEventArgs kevent)
            {
                base.OnKeyUp(kevent);

                if (kevent.KeyCode == Keys.Space)
                {
                    _isPressed = false;
                    Invalidate();
                }
            }

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);

                // A disabled control gets no MouseUp, so a press that was still
                // going (a click handler that disables the button) would stay.
                _isPressed = false;
                Invalidate();
            }

            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                _isPressed = false;
                Invalidate();
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                _isInkShiftKnown = false;
                Invalidate();
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                _isInkShiftKnown = false;
                Invalidate();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                _isInkShiftKnown = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                Color fill = BackColor;

                if (Enabled && _isPressed)
                    fill = FlatAppearance.MouseDownBackColor;
                else if (Enabled && _isHot)
                    fill = FlatAppearance.MouseOverBackColor;

                using (SolidBrush brush = new SolidBrush(fill))
                    g.FillRectangle(brush, ClientRectangle);

                int borderSize = Math.Min(FlatAppearance.BorderSize, Math.Min(Width, Height) / 2);

                if (borderSize > 0)
                {
                    // Four strips, not DrawRectangle: a pen drawing a rectangle
                    // leaves the pixel at its starting corner unpainted.
                    using (SolidBrush brush = new SolidBrush(FlatAppearance.BorderColor))
                    {
                        g.FillRectangle(brush, 0, 0, Width, borderSize);
                        g.FillRectangle(brush, 0, Height - borderSize, Width, borderSize);
                        g.FillRectangle(brush, 0, borderSize, borderSize, Height - 2 * borderSize);
                        g.FillRectangle(brush, Width - borderSize, borderSize, borderSize, Height - 2 * borderSize);
                    }
                }

                if (!string.IsNullOrEmpty(Text))
                {
                    Rectangle area = ClientRectangle;
                    Point shift = GetInkShift();
                    area.Offset(-shift.X, -shift.Y);

                    TextRenderer.DrawText(g, Text, Font, area, ForeColor, SymbolFlags);
                }

                if (Focused && ShowFocusCues)
                    ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -3, -3), ForeColor, fill);
            }

            // How bright a pixel of the white-on-black symbol has to be to count as ink.
            private const int InkThreshold = 60;

            private const TextFormatFlags SymbolFlags =
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

            // How far the visible part of the symbol lies from the middle of
            // the button when the framework centers the text. Found by drawing
            // the symbol white on black into a bitmap and looking at which
            // pixels it covers.
            private Point GetInkShift()
            {
                if (_isInkShiftKnown)
                    return _inkShift;

                _inkShift = Point.Empty;
                _isInkShiftKnown = true;

                if (Width <= 0 || Height <= 0)
                    return _inkShift;

                using (Bitmap bitmap = new Bitmap(Width, Height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Black);
                    TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), Color.White, SymbolFlags);

                    int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            Color pixel = bitmap.GetPixel(x, y);

                            // All three channels: ClearType colors the edges of a glyph
                            // differently per channel, one alone would tilt the result.
                            if (pixel.R + pixel.G + pixel.B <= InkThreshold * 3)
                                continue;

                            minX = System.Math.Min(minX, x);
                            maxX = System.Math.Max(maxX, x);
                            minY = System.Math.Min(minY, y);
                            maxY = System.Math.Max(maxY, y);
                        }
                    }

                    if (maxX >= 0)
                    {
                        // Twice the offset of the center, to keep the half pixel.
                        int shiftX = (minX + maxX + 1) - Width;
                        int shiftY = (minY + maxY + 1) - Height;

                        _inkShift = new Point(
                            (int)System.Math.Round(shiftX / 2.0, MidpointRounding.AwayFromZero),
                            (int)System.Math.Round(shiftY / 2.0, MidpointRounding.AwayFromZero));
                    }
                }

                return _inkShift;
            }
        }
    }
}