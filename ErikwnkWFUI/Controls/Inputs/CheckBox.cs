using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
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
    /// ignored: <c>CheckAlign</c> (the box is always on the left), <c>TextAlign</c>,
    /// <c>RightToLeft</c>, <c>Appearance</c>, <c>Image</c> with <c>ImageAlign</c> and
    /// <c>TextImageRelation</c>, <c>FlatStyle</c> with <c>FlatAppearance</c>, <c>AutoEllipsis</c>,
    /// <c>Padding</c> and <c>UseVisualStyleBackColor</c>. A double click is two clicks, as on the
    /// standard box. The size is not automatic: the factory sets
    /// it for the text and font it is given, so after changing <c>Text</c> or <c>Font</c> set
    /// the <c>Size</c> too.
    /// </remarks>
    // Why not recolor the native control: ForeColor drives the native checkmark as
    // well as the label, the native glyph does not dim when disabled, and drawing over it
    // from a Paint event still let ButtonBase draw its own glyph and text underneath (a
    // label showed up doubled in a PropertyTable cell). A subclass that draws in OnPaint
    // and never calls base.OnPaint stops the native drawing completely.
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
                BoxSize = ScaleBy(Dpi, BoxSize96);
                BoxLeftMargin = ScaleBy(Dpi, BoxLeftMargin96);
                GlyphColumnWidth = BoxLeftMargin + BoxSize + ScaleBy(Dpi, TextLeftPadding96);
            }

            private static int ScaleBy(int dpi, int value96)
            {
                return (int)System.Math.Round(value96 * dpi / 96.0);
            }

            public int Scale(int value96)
            {
                return ScaleBy(Dpi, value96);
            }
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

            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
        }

        private bool _isHot;
        private bool _isPressed;
        private UIColorSet _colors;

        /// <summary>
        /// The colors of the filled box: its fill at rest, hovered and pressed, and the
        /// mark on each. Left null, the box is filled in the neutral gray
        /// (<c>UIStyles.Colors.Sets.Gray</c>); <c>UIStyles.CheckBoxes.CreatePrimary</c>
        /// makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;
                Invalidate();
            }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        private Func<UIColorSet> _colorsSource;

        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
            Invalidate();
        }

        private UIColorSet GetColors()
        {
            return _colorsSource?.Invoke() ?? _colors ?? UIStyles.Colors.Sets.Gray;
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
            using (Bitmap buffer = new Bitmap(Width, Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(buffer))
                    PaintShapes(g);

                e.Graphics.DrawImageUnscaled(buffer, 0, 0);
            }

            PaintText(e.Graphics);
            PaintFocus(e.Graphics);

            // No base.OnPaint: the native drawing must not run (see the class comment).
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
            get
            {
                if (!Enabled)
                    return UIColors.DisabledGray;

                bool isActive = _isHot || _isPressed;

                if (!isActive || CheckState != CheckState.Unchecked)
                    return OutlineColor;

                UIColorSet colors = GetColors();

                return UIColors.EnsureContrast(
                    _isPressed ? colors.Pressed : colors.Hover,
                    GetParentBackColor(),
                    _isPressed ? 6.0 : 4.5);
            }
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

        private void PaintShapes(Graphics g)
        {
            using (SolidBrush eraseBrush = new SolidBrush(GetParentBackColor()))
                g.FillRectangle(eraseBrush, new Rectangle(0, 0, Width, Height));

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Metrics metrics = GetMetrics();
            bool hasText = !string.IsNullOrEmpty(Text);

            int boxX = hasText
                ? metrics.BoxLeftMargin
                : (Width - metrics.BoxSize) / 2;
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
            else if (_isPressed)
            {
                fillColor = colors.Pressed;
                markColor = colors.PressedText;
            }
            else if (_isHot)
            {
                fillColor = colors.Hover;
                markColor = colors.HoverText;
            }
            else
            {
                fillColor = colors.Basic;
                markColor = colors.BasicText;
            }

            Color borderColor = BorderColor;

            using (GraphicsPath boxPath = CreateRoundedRectanglePath(boxRect, metrics.Scale(3)))
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
                DrawCheckmark(g, boxRect, markColor, metrics.Scale(2));
            else if (CheckState == CheckState.Indeterminate)
                DrawDash(g, boxRect, markColor, metrics.Scale(2));
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

        private void PaintText(Graphics g)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            int glyphColumn = GetMetrics().GlyphColumnWidth;
            Rectangle textArea = new Rectangle(glyphColumn, 0, Width - glyphColumn, Height);

            // Disabled text is the contrasting color blended toward the background, as on
            // buttons: the fixed DisabledGray of the box is too faint for text on the
            // light theme.
            Color parentBackColor = GetParentBackColor();
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
        private void PaintFocus(Graphics g)
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
                int width = System.Math.Min(Width - 2, metrics.GlyphColumnWidth + textWidth + 2);
                area = new Rectangle(1, 1, width, Height - 2);
            }

            area.Intersect(new Rectangle(0, 0, Width, Height));
            ControlPaint.DrawFocusRectangle(g, area, UIColors.TextPrimary, GetParentBackColor());
        }

        /// <summary>
        /// Draws just the box of a check box - outline, fill when checked or undetermined, and the
        /// mark - for other controls that show check boxes (the list view). The colors are the
        /// caller's; the measures follow the screen's scaling.
        /// </summary>
        internal static void DrawGlyph(Graphics g, Rectangle box, CheckState state, Color outline, Color fill, Color mark, int dpi)
        {
            Metrics metrics = new Metrics(dpi);
            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = CreateRoundedRectanglePath(box, metrics.Scale(3)))
            {
                if (state != CheckState.Unchecked)
                {
                    using (SolidBrush brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(outline, metrics.Scale(15) / 10f))
                    g.DrawPath(pen, path);
            }

            if (state == CheckState.Checked)
                DrawCheckmark(g, box, mark, metrics.Scale(2));
            else if (state == CheckState.Indeterminate)
                DrawDash(g, box, mark, metrics.Scale(2));

            g.SmoothingMode = previous;
        }

        private static void DrawDash(Graphics g, Rectangle box, Color color, float width)
        {
            int y = box.Y + box.Height / 2;
            int left = box.X + (int)(box.Width * 0.26);
            int right = box.X + (int)(box.Width * 0.74);

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, left, y, right, y);
            }
        }

        private static void DrawCheckmark(Graphics g, Rectangle box, Color color, float width)
        {
            Point p1 = new Point(box.X + (int)(box.Width * 0.22), box.Y + (int)(box.Height * 0.52));
            Point p2 = new Point(box.X + (int)(box.Width * 0.42), box.Y + (int)(box.Height * 0.74));
            Point p3 = new Point(box.X + (int)(box.Width * 0.80), box.Y + (int)(box.Height * 0.26));

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[] { p1, p2, p3 });
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
