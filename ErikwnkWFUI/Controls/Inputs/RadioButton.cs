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
    /// A themed <see cref="System.Windows.Forms.RadioButton"/>, drawn entirely by the library: a round box with a dot when it is
    /// selected, and the label. The selected box takes its colors from a <see cref="UIColorSet"/> - neutral gray by default, the
    /// accent for the primary one - and, like a button, the box has a look for hovered, pressed, focused (shown once the keyboard
    /// is used) and disabled. Get one through <see cref="UIStyles.RadioButtons"/>.
    /// </summary>
    /// <remarks>
    /// It keeps everything that is not looks from the standard control: <c>Checked</c>, <c>AutoCheck</c>, the events, the keyboard
    /// (arrow keys move within a group), the mnemonic and the grouping - radio buttons with the same parent form one group, so
    /// selecting one clears the others; put them in a <see cref="Panel"/> or <see cref="GroupBox"/> for separate groups, or use a
    /// <see cref="RadioGroup"/> for a choice that is not tied to one parent.
    /// <para>
    /// The box is as large as that of <see cref="CheckBox"/>, so both line up in one column. <c>CheckAlign</c> puts the box on the
    /// right with one of the right-hand alignments (<c>TopRight</c>, <c>MiddleRight</c>, <c>BottomRight</c>), as on the check box;
    /// the box is always centered vertically. The factory sets the size for the text and font it is given and leaves
    /// <c>AutoSize</c> off; with <c>AutoSize</c> on, the size follows <c>Text</c> and <c>Font</c>.
    /// </para>
    /// <para>
    /// Because the library draws the box itself, these properties are ignored: <c>TextAlign</c>, <c>RightToLeft</c>,
    /// <c>Appearance</c>, <c>Image</c> with <c>ImageAlign</c> and <c>TextImageRelation</c>, <c>FlatStyle</c> with
    /// <c>FlatAppearance</c>, <c>AutoEllipsis</c>, <c>Padding</c> and <c>UseVisualStyleBackColor</c>.
    /// </para>
    /// </remarks>
    public class RadioButton : System.Windows.Forms.RadioButton
    {
        public RadioButton()
        {
            // As the CheckBox: no shared double buffer (OnPaint draws into a private bitmap), no native drawing.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);

            AutoSize = false;

            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
        }

        private bool _isHot;
        private bool _isPressed;
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        /// <summary>
        /// The colors of the selected box: its fill at rest, hovered and pressed, and the dot on each. Left null, the box is filled in
        /// the neutral gray (<c>UIStyles.Colors.Sets.Gray</c>); <c>UIStyles.RadioButtons.CreatePrimary</c> makes one in the accent color.
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

        // The size for a text and font: the same as the check box.
        internal static Size ComputePreferredSize(string text, Font font, int dpi = 96)
        {
            return CheckBox.ComputePreferredSize(text, font, dpi);
        }

        // With AutoSize on, the size follows the text and the font (measured as the check box is: the native measuring knows only
        // the system's round glyph).
        public override Size GetPreferredSize(Size proposedSize)
        {
            return ComputePreferredSize(Text, Font, DeviceDpi);
        }

        protected override void OnCheckedChanged(System.EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
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

        // The mouse can be taken away without a MouseUp (another window, a dialog opened while pressing).
        protected override void OnMouseCaptureChanged(System.EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            _isPressed = false;
            Invalidate();
        }

        // Hidden, a button gets no MouseLeave: shown again it must not still look hovered.
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

            // A disabled control gets neither MouseUp nor MouseLeave (see CheckBox): what is hovered is looked at again when it is enabled.
            _isPressed = false;
            _isHot = Enabled && IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }

        // Nothing is painted here: OnPaint erases to the parent's color itself (a transparent BackColor made stale text of neighbors show).
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
                return;

            // The round box goes into a private bitmap first, then onto the control; the text is drawn last, straight onto the
            // control, because ClearType through an intermediate bitmap comes out soft (see CheckBox).
            Color parentBackColor = GetParentBackColor();
            Bitmap buffer = GetBuffer();

            using (Graphics g = Graphics.FromImage(buffer))
                PaintShapes(g, parentBackColor);

            e.Graphics.DrawImageUnscaled(buffer, 0, 0);

            PaintText(e.Graphics, parentBackColor);
            PaintFocus(e.Graphics, parentBackColor);

            // No base.OnPaint: the native drawing must not run.
        }

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

        private Color GetParentBackColor()
        {
            for (Control parent = Parent; parent != null; parent = parent.Parent)
            {
                if (parent.BackColor.A == 255)
                    return parent.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        // The outline of the round box at rest: as the check box (3:1 against the page in both themes).
        internal Color OutlineColor
        {
            get { return UIColors.GetOutlineColor(GetParentBackColor()); }
        }

        // The border in the current state: the outline at rest, the fixed gray when disabled, and for an empty box that is hovered or
        // pressed the color of the set, brought to 4.5:1 (hovered) or 6:1 (pressed) against the page.
        internal Color BorderColor
        {
            get { return GetBorderColor(GetParentBackColor()); }
        }

        private Color GetBorderColor(Color parentBackColor)
        {
            if (!Enabled)
                return UIColors.DisabledGray;

            bool isActive = _isHot || _isPressed;

            if (!isActive || Checked)
                return UIColors.GetOutlineColor(parentBackColor);

            UIColorSet colors = GetColors();

            return UIColors.EnsureContrast(
                _isPressed ? colors.Pressed : colors.Hover,
                parentBackColor,
                _isPressed ? 6.0 : 4.5);
        }

        // The fill of a selected box and the dot on it for the current state: as the check box, so both look alike side by side.
        internal void GetFill(Color parentBackColor, out Color fill, out Color dot)
        {
            if (!Enabled)
            {
                fill = UIColors.DisabledGray;
                dot = UIColors.GetContrastingForeColor(UIColors.DisabledGray);
                return;
            }

            UIColorSet colors = GetColors();
            bool brighter = UIColors.IsLight(parentBackColor)
                && UIColors.GetContrastRatio(colors.HoverText, colors.Hover) >= 3.0;

            if (_isPressed)
            {
                fill = brighter ? colors.Basic : colors.Pressed;
                dot = brighter ? colors.BasicText : colors.PressedText;
            }
            else if (_isHot)
            {
                fill = brighter ? colors.Pressed : colors.Hover;
                dot = brighter ? ReadableDot(colors.PressedText, colors.Pressed) : colors.HoverText;
            }
            else
            {
                fill = brighter ? colors.Hover : colors.Basic;
                dot = brighter ? colors.HoverText : colors.BasicText;
            }
        }

        // The dot of the set when it is readable on the fill (3:1), else the dark or the light one that has the higher contrast on it.
        private static Color ReadableDot(Color setDot, Color fill)
        {
            if (UIColors.GetContrastRatio(setDot, fill) >= 3.0)
                return setDot;

            Color dark = Color.FromArgb(20, 20, 20);
            Color light = Color.FromArgb(240, 240, 240);

            return UIColors.GetContrastRatio(dark, fill) >= UIColors.GetContrastRatio(light, fill) ? dark : light;
        }

        private void PaintShapes(Graphics g, Color parentBackColor)
        {
            using (SolidBrush eraseBrush = new SolidBrush(parentBackColor))
                g.FillRectangle(eraseBrush, new Rectangle(0, 0, Width, Height));

            g.SmoothingMode = SmoothingMode.AntiAlias;

            CheckBox.Metrics metrics = new CheckBox.Metrics(DeviceDpi);
            bool hasText = !string.IsNullOrEmpty(Text);

            int boxX = !hasText
                ? (Width - metrics.BoxSize) / 2
                : CheckBox.IsBoxOnRight(CheckAlign) ? Width - metrics.BoxLeftMargin - metrics.BoxSize : metrics.BoxLeftMargin;
            int boxY = (Height - metrics.BoxSize) / 2;
            Rectangle boxRect = new Rectangle(boxX, boxY, metrics.BoxSize, metrics.BoxSize);

            bool isActive = Enabled && (_isHot || _isPressed);
            Color borderColor = GetBorderColor(parentBackColor);

            // The pen is drawn inside the box, so the outline is as wide as that of the check box.
            RectangleF ring = new RectangleF(boxRect.X + 0.5f, boxRect.Y + 0.5f, boxRect.Width - 1, boxRect.Height - 1);

            if (Checked)
            {
                GetFill(parentBackColor, out Color fill, out Color dot);

                using (SolidBrush fillBrush = new SolidBrush(fill))
                    g.FillEllipse(fillBrush, ring);

                using (Pen borderPen = new Pen(borderColor, metrics.Scale(15) / 10f))
                    g.DrawEllipse(borderPen, ring);

                float side = boxRect.Width * 0.4f;
                using (SolidBrush dotBrush = new SolidBrush(dot))
                    g.FillEllipse(dotBrush, boxRect.X + (boxRect.Width - side) / 2, boxRect.Y + (boxRect.Height - side) / 2, side, side);
            }
            else
            {
                if (isActive)
                {
                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(_isPressed ? 60 : 30, GetColors().Basic)))
                        g.FillEllipse(tintBrush, ring);
                }

                using (Pen borderPen = new Pen(borderColor, metrics.Scale(isActive ? 20 : 15) / 10f))
                    g.DrawEllipse(borderPen, ring);
            }
        }

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

            CheckBox.Metrics metrics = new CheckBox.Metrics(DeviceDpi);
            int glyphColumn = metrics.GlyphColumnWidth;
            Rectangle textArea = CheckBox.IsBoxOnRight(CheckAlign)
                ? new Rectangle(metrics.BoxLeftMargin, 0, Width - glyphColumn, Height)
                : new Rectangle(glyphColumn, 0, Width - glyphColumn, Height);

            Color textColor = Enabled
                ? UIColors.TextPrimary
                : UIColors.BlendToward(UIColors.GetContrastingForeColor(parentBackColor), parentBackColor, 0.35);

            TextRenderer.DrawText(g, Text, Font, textArea, textColor, GetTextFlags());
        }

        // The dotted frame of the keyboard focus around the label and the box (the box alone without a label) - only once the keyboard was used.
        private void PaintFocus(Graphics g, Color parentBackColor)
        {
            if (!Focused || !ShowFocusCues)
                return;

            CheckBox.Metrics metrics = new CheckBox.Metrics(DeviceDpi);
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
                int width = CheckBox.IsBoxOnRight(CheckAlign)
                    ? Width - 2
                    : Math.Min(Width - 2, metrics.GlyphColumnWidth + textWidth + 2);
                area = new Rectangle(1, 1, width, Height - 2);
            }

            area.Intersect(new Rectangle(0, 0, Width, Height));
            ControlPaint.DrawFocusRectangle(g, area, UIColors.TextPrimary, parentBackColor);
        }
    }
}
