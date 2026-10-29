using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.CheckBox"/>, drawn entirely by the library:
    /// a rounded box, a checkmark (a dash when undetermined) and the label. The filled box
    /// takes its colors from a <see cref="UIColorSet"/> - neutral gray by default, the accent
    /// for the primary one - and, like a button, the box has a look for hovered, pressed,
    /// focused (shown once the keyboard is used) and disabled. Get one through
    /// <see cref="UIStyles.CheckBoxes"/>.
    /// </summary>
    /// <remarks>
    /// It keeps everything that is not looks from the standard control: <c>Checked</c>,
    /// <c>CheckState</c>, <c>ThreeState</c>, <c>AutoCheck</c>, the events, the keyboard and
    /// the mnemonic. Because the library draws the box itself, these properties are
    /// ignored: <c>TextAlign</c>,
    /// <c>RightToLeft</c>, <c>Appearance</c>, <c>Image</c> with <c>ImageAlign</c> and
    /// <c>TextImageRelation</c>, <c>FlatStyle</c> with <c>FlatAppearance</c>, <c>AutoEllipsis</c>,
    /// <c>Padding</c> and <c>UseVisualStyleBackColor</c>. A double click is two clicks, as on the
    /// standard box. <c>CheckAlign</c> puts the box on the right when it is one of the right-hand
    /// alignments (<c>TopRight</c>, <c>MiddleRight</c>, <c>BottomRight</c>) - the label then stays on the
    /// left and the box sits at the right edge, the way settings lists are often laid out; the box is
    /// always centered vertically. The factory sets the size for the text and font it is given and leaves
    /// <c>AutoSize</c> off (a table cell that is laid out several times is steadier with a fixed
    /// size); with <c>AutoSize</c> on, the size follows <c>Text</c> and <c>Font</c> by itself.
    /// <para>
    /// Why the library draws the box itself instead of recoloring the native one: <c>ForeColor</c> drives the native
    /// checkmark as well as the label, the native glyph does not dim when disabled, and drawing over it from a Paint
    /// event still let <c>ButtonBase</c> draw its own glyph and text underneath (a label showed up doubled in a
    /// PropertyTable cell). A subclass that draws in <c>OnPaint</c> and never calls <c>base.OnPaint</c> stops the native
    /// drawing completely.
    /// </para>
    /// </remarks>
    public class CheckBox : System.Windows.Forms.CheckBox
    {
        // The measures of the box and its label at 96 dpi; they grow with the screen's
        // scaling (see Metrics), so the box keeps pace with the text.
        private const int BoxSize96 = 16;
        private const int BoxLeftMargin96 = 2;
        private const int TextLeftPadding96 = 6;

        internal readonly struct Metrics
        {
            public readonly int Dpi;
            public readonly int BoxSize;
            public readonly int BoxLeftMargin;
            public readonly int GlyphColumnWidth;

            public Metrics(int dpi)
            {
                Dpi = dpi < 96 ? 96 : dpi;
                BoxSize = Helpers.Dpi.Scale(BoxSize96, Dpi);
                BoxLeftMargin = Helpers.Dpi.Scale(BoxLeftMargin96, Dpi);
                GlyphColumnWidth = BoxLeftMargin + BoxSize + Helpers.Dpi.Scale(TextLeftPadding96, Dpi);
            }

            public int Scale(int value96)
            {
                return Helpers.Dpi.Scale(value96, Dpi);
            }
        }

        // The box is on the right with one of the right-hand CheckAlign values (shared with the radio button).
        internal static bool IsBoxOnRight(ContentAlignment checkAlign)
        {
            return checkAlign == ContentAlignment.TopRight
                || checkAlign == ContentAlignment.MiddleRight
                || checkAlign == ContentAlignment.BottomRight;
        }

        private Metrics GetMetrics()
        {
            return new Metrics(DeviceDpi);
        }

        public CheckBox()
        {
            // No OptimizedDoubleBuffer / DoubleBuffered: both use WinForms' one shared buffer
            // bitmap, and with several of these boxes (and other double-buffered controls
            // such as ToggleSwitch) painting in one burst, pixels of a neighbor - its text,
            // once a switch's knob - showed up inside a box. OnPaint draws into a private
            // bitmap of its own instead.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);

            // The size is the caller's (the factory sets it for the text and font, see ComputePreferredSize): the native
            // measuring knows only the system glyph, so the standard control's automatic size would be wrong here.
            AutoSize = false;

            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
        }

        private bool _isHot;
        private bool _isPressed;
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        /// <summary>
        /// The colors of the filled box: its fill at rest, hovered and pressed, and the
        /// mark on each. Left null, the box is filled in the neutral gray
        /// (<c>UIStyles.Colors.Sets.Gray</c>); <c>UIStyles.CheckBoxes.CreatePrimary</c>
        /// makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                Invalidate();
            }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            Invalidate();
        }

        private UIColorSet GetColors()
        {
            return _colorSet.Value ?? UIStyles.Colors.Sets.Gray;
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHot = true;
            Invalidate();
        }

        protected override void OnMouseLeave(System.EventArgs e)
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.Space)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnGotFocus(System.EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(System.EventArgs e)
        {
            base.OnLostFocus(e);
            _isPressed = false;
            Invalidate();
        }

        // The mouse can be taken away without a MouseUp (another window, a dialog opened
        // while pressing): then nothing is pressed any more.
        protected override void OnMouseCaptureChanged(System.EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            _isPressed = false;
            Invalidate();
        }

        // Hidden, a box gets no MouseLeave: shown again it must not still look hovered.
        protected override void OnVisibleChanged(System.EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
            {
                _isHot = false;
                _isPressed = false;
            }
        }

        protected override void OnEnabledChanged(System.EventArgs e)
        {
            base.OnEnabledChanged(e);

            // A disabled control gets no MouseUp: a press that was still going
            // (a handler that disables the box) must not outlive it. Nor does it get a
            // MouseLeave: whether the mouse is over the box is looked at again when it is
            // enabled, so it does not look hovered with the mouse somewhere else.
            _isPressed = false;
            _isHot = Enabled && IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }

        // The size for a text and font: the 16 px box, the gap and the measured label. The
        // factory sets it and turns AutoSize off, because the native measuring knows only
        // the system glyph and, in a table cell that is laid out several times, settled
        // on different bounds than a fresh control (leftovers of neighboring cells showed).
        internal static Size ComputePreferredSize(string text, Font font, int dpi = 96)
        {
            Metrics metrics = new Metrics(dpi);
            bool hasText = !string.IsNullOrEmpty(text);

            int width = hasText
                ? metrics.GlyphColumnWidth + TextRenderer.MeasureText(text, font).Width + 4
                : metrics.BoxSize + metrics.BoxLeftMargin * 2;

            int height = System.Math.Max(metrics.BoxSize + 8, font.Height + 8);

            return new Size(width, height);
        }

        // With AutoSize on, the size follows the text and the font: the native measuring knows only the system glyph, so the
        // box is measured here, with the same measures that it is drawn with.
        public override Size GetPreferredSize(Size proposedSize)
        {
            return ComputePreferredSize(Text, Font, DeviceDpi);
        }

        // Nothing is painted here: OnPaint erases to the parent's color itself. A
        // transparent BackColor would have WinForms ask the parent to draw what is behind
        // the box, and in a nested table layout that showed stale text of neighboring cells.
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
                return;

            // Box and checkmark go into a private bitmap first (see the constructor), then
            // onto the control. The text does not: ClearType colors its pixels for the real
            // screen, and through an intermediate bitmap it came out soft and fringed. So
            // the label is drawn last, straight onto the control's own surface.
            // The color of the page behind the box is looked for once, not for every part.
            Color parentBackColor = GetParentBackColor();
            Bitmap buffer = GetBuffer();

            using (Graphics g = Graphics.FromImage(buffer))
                PaintShapes(g, parentBackColor);

            e.Graphics.DrawImageUnscaled(buffer, 0, 0);

            PaintText(e.Graphics, parentBackColor);
            PaintFocus(e.Graphics, parentBackColor);

            // No base.OnPaint: the native drawing must not run (see the class comment).
        }

        // The private bitmap of OnPaint: kept from one paint to the next (a new one for every paint is a lot of garbage
        // where many boxes sit in a table), made again when the size changes. PaintShapes erases all of it first.
        private Bitmap _buffer;

        private Bitmap GetBuffer()
        {
            if (_buffer == null || _buffer.Width != Width || _buffer.Height != Height)
            {
                _buffer?.Dispose();
                _buffer = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            }

            return _buffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer?.Dispose();
                _buffer = null;
            }

            base.Dispose(disposing);
        }

        // The outline of the box at rest: the best text color for the parent, moved toward
        // it just far enough to keep 3:1 against it. The box so stands out in both themes
        // (the theme's own border color is only 1.6:1 there) and on any parent color, and a
        // filled box is delineated even when its fill is close to the page.
        internal Color OutlineColor
        {
            get { return UIColors.GetOutlineColor(GetParentBackColor()); }
        }

        // The border of the box in the current state: the outline at rest, the fixed gray
        // when disabled, and for an empty box that is hovered or pressed the color of the
        // set - brought to 4.5:1 (hovered) or 6:1 (pressed) against the page, so the state
        // is seen as a stronger outline in both themes (the gray set alone would be fainter
        // than the outline in the dark theme).
        internal Color BorderColor
        {
            get { return GetBorderColor(GetParentBackColor()); }
        }

        private Color GetBorderColor(Color parentBackColor)
        {
            if (!Enabled)
                return UIColors.DisabledGray;

            bool isActive = _isHot || _isPressed;

            if (!isActive || CheckState != CheckState.Unchecked)
                return UIColors.GetOutlineColor(parentBackColor);

            UIColorSet colors = GetColors();

            return UIColors.EnsureContrast(
                _isPressed ? colors.Pressed : colors.Hover,
                parentBackColor,
                _isPressed ? 6.0 : 4.5);
        }

        // The mark of the set when it is readable on the fill (3:1), else the dark or the light mark that has the higher contrast on it.
        private static Color ReadableMark(Color setMark, Color fill)
        {
            if (UIColors.GetContrastRatio(setMark, fill) >= 3.0)
                return setMark;

            Color dark = Color.FromArgb(20, 20, 20);
            Color light = Color.FromArgb(240, 240, 240);

            return UIColors.GetContrastRatio(dark, fill) >= UIColors.GetContrastRatio(light, fill) ? dark : light;
        }

        private Color GetParentBackColor()
        {
            for (Control parent = Parent; parent != null; parent = parent.Parent)
            {
                if (parent.BackColor.A == 255)
                    return parent.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        private void PaintShapes(Graphics g, Color parentBackColor)
        {
            using (SolidBrush eraseBrush = new SolidBrush(parentBackColor))
                g.FillRectangle(eraseBrush, new Rectangle(0, 0, Width, Height));

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Metrics metrics = GetMetrics();
            bool hasText = !string.IsNullOrEmpty(Text);

            int boxX = !hasText
                ? (Width - metrics.BoxSize) / 2
                : IsBoxOnRight(CheckAlign) ? Width - metrics.BoxLeftMargin - metrics.BoxSize : metrics.BoxLeftMargin;
            int boxY = (Height - metrics.BoxSize) / 2;
            Rectangle boxRect = new Rectangle(boxX, boxY, metrics.BoxSize, metrics.BoxSize);

            // Filled when checked or undetermined, outline only when not. The fill and the
            // mark on it come from the color set for rest, hovered and pressed. An empty box
            // shows hover and press by its border and, pressed, a faint tint. Disabled is a
            // fixed gray (DisabledGray, not the theme's TextDisabled) so a disabled box looks
            // the same in both themes, like a disabled button or switch.
            UIColorSet colors = GetColors();
            bool isFilled = CheckState != CheckState.Unchecked;
            bool isActive = Enabled && (_isHot || _isPressed);

            Color fillColor;
            Color markColor;

            if (!Enabled)
            {
                fillColor = UIColors.DisabledGray;
                markColor = UIColors.GetContrastingForeColor(UIColors.DisabledGray);
            }
            else
            {
                // On a light page the set's own fill (dark, made for the dark theme) hardly looks colored - a blue box looks almost gray - so the box
                // takes the next brighter shade of the set: rest = the set's hover shade, hovered = its pressed shade, pressed = its basic shade.
                // Only where the mark of the set stays readable on the brighter rest shade. A shade the set has no readable mark for gets the
                // dark or light mark that reads best.
                bool brighter = UIColors.IsLight(parentBackColor)
                    && UIColors.GetContrastRatio(colors.HoverText, colors.Hover) >= 3.0;

                if (_isPressed)
                {
                    fillColor = brighter ? colors.Basic : colors.Pressed;
                    markColor = brighter ? colors.BasicText : colors.PressedText;
                }
                else if (_isHot)
                {
                    fillColor = brighter ? colors.Pressed : colors.Hover;
                    markColor = brighter ? ReadableMark(colors.PressedText, colors.Pressed) : colors.HoverText;
                }
                else
                {
                    fillColor = brighter ? colors.Hover : colors.Basic;
                    markColor = brighter ? colors.HoverText : colors.BasicText;
                }            }

            Color borderColor = GetBorderColor(parentBackColor);

            using (GraphicsPath boxPath = RoundedRectangle.CreatePath(boxRect, metrics.Scale(3)))
            {
                if (isFilled)
                {
                    using (SolidBrush fillBrush = new SolidBrush(fillColor))
                        g.FillPath(fillBrush, boxPath);
                }
                else if (isActive)
                {
                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(_isPressed ? 60 : 30, colors.Basic)))
                        g.FillPath(tintBrush, boxPath);
                }

                using (Pen borderPen = new Pen(borderColor, metrics.Scale(isActive && !isFilled ? 20 : 15) / 10f))
                    g.DrawPath(borderPen, boxPath);
            }

            if (CheckState == CheckState.Checked)
                CheckGlyph.DrawCheckmark(g, boxRect, markColor, metrics.Scale(2));
            else if (CheckState == CheckState.Indeterminate)
                CheckGlyph.DrawDash(g, boxRect, markColor, metrics.Scale(2));
        }

        // A mnemonic ("&Save") shows its underline only once the Alt key was used, as on any
        // checkbox; without mnemonics the "&" is just a character.
        private TextFormatFlags GetTextFlags()
        {
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

            if (!UseMnemonic)
                flags |= TextFormatFlags.NoPrefix;
            else if (!ShowKeyboardCues)
                flags |= TextFormatFlags.HidePrefix;

            return flags;
        }

        private void PaintText(Graphics g, Color parentBackColor)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            Metrics metrics = GetMetrics();
            int glyphColumn = metrics.GlyphColumnWidth;
            Rectangle textArea = IsBoxOnRight(CheckAlign)
                ? new Rectangle(metrics.BoxLeftMargin, 0, Width - glyphColumn, Height)
                : new Rectangle(glyphColumn, 0, Width - glyphColumn, Height);

            // Disabled text is the contrasting color blended toward the background, as on
            // buttons: the fixed DisabledGray of the box is too faint for text on the
            // light theme.
            Color textColor = Enabled
                ? UIColors.TextPrimary
                : UIColors.BlendToward(UIColors.GetContrastingForeColor(parentBackColor), parentBackColor, 0.35);

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textArea,
                textColor,
                GetTextFlags());
        }

        // The dotted frame of the keyboard focus, around the label and the box (or the
        // box alone when there is no label) - only once the keyboard has been used.
        private void PaintFocus(Graphics g, Color parentBackColor)
        {
            if (!Focused || !ShowFocusCues)
                return;

            Metrics metrics = GetMetrics();
            Rectangle area;

            if (string.IsNullOrEmpty(Text))
            {
                int around = metrics.Scale(3);
                area = new Rectangle(
                    (Width - metrics.BoxSize) / 2 - around,
                    (Height - metrics.BoxSize) / 2 - around,
                    metrics.BoxSize + around * 2,
                    metrics.BoxSize + around * 2);
            }
            else
            {
                int textWidth = TextRenderer.MeasureText(Text, Font).Width;
                int width = IsBoxOnRight(CheckAlign)
                    ? Width - 2
                    : System.Math.Min(Width - 2, metrics.GlyphColumnWidth + textWidth + 2);
                area = new Rectangle(1, 1, width, Height - 2);
            }

            area.Intersect(new Rectangle(0, 0, Width, Height));
            ControlPaint.DrawFocusRectangle(g, area, UIColors.TextPrimary, parentBackColor);
        }
    }
}
