using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.CheckBox"/>, drawn entirely by the
    /// library: a rounded box filled with a color set (neutral gray, or the accent for
    /// the primary one) when checked (a dash
    /// when undetermined), a checkmark and the label. Like a button it has a
    /// look for hovered, pressed, focused (shown when the keyboard is used) and
    /// disabled. Get one through <see cref="UIStyles.CheckBoxes"/>.
    /// </summary>
    // Fully owner-drawn (box, checkmark, and label) instead of trying to
    // recolor pieces of the native glyph - four earlier attempts each
    // fixed one problem and caused another: ForeColor drives the native
    // checkmark's color too, so tuning it for label readability made the
    // checkmark invisible against its own white box (and vice versa);
    // the native glyph doesn't dim on Enabled at all; erasing only a
    // guessed-width text region left a sliver of the native glyph/text
    // peeking out at the boundary; and even a full-rectangle erase in a
    // plain CheckBox's Paint EVENT still let native ButtonBase text
    // render underneath/after it in specific layouts (confirmed via a
    // PropertyTable row: a disabled or otherwise-repainted
    // checkbox showed its label doubled, faintly offset - ButtonBase's
    // own OnPaint drawing its default glyph/text using its own layout,
    // with our Paint-event handler's drawing on top not fully hiding it
    // depending on repaint timing). A real subclass that overrides
    // OnPaint and never calls base.OnPaint prevents ButtonBase's native
    // rendering from running at all, which is the only approach that
    // eliminated the doubling in every case tested.
    public class CheckBox : System.Windows.Forms.CheckBox
    {
        private const int BoxSize = 16;
        private const int BoxLeftMargin = 2;
        private const int TextLeftPadding = 6;
        private const int GlyphColumnWidth = BoxLeftMargin + BoxSize + TextLeftPadding;

        public CheckBox()
        {
            // Deliberately NOT using ControlStyles.OptimizedDoubleBuffer/
            // DoubleBuffered=true - both route through WinForms'
            // process-wide BufferedGraphicsManager, which reuses ONE
            // shared backing bitmap across every double-buffered control
            // for efficiency. With several of these checkboxes (and
            // other double-buffered controls, e.g. ToggleSwitch) all
            // painting during the same layout/paint burst, that shared
            // buffer got reused before being fully overwritten, and a
            // neighboring control's leftover pixels (its text, or in one
            // observed case a ToggleSwitch's blue knob) showed up baked
            // into a checkbox's painted area on real screen captures
            // (CopyFromScreen and PrintWindow both showed it - not a
            // capture-tooling artifact). OnPaint below manages its own
            // private per-call Bitmap instead, so no buffer is ever
            // shared with any other control.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
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
            get { return _colors; }
            set
            {
                _colors = value;
                Invalidate();
            }
        }

        private UIColorSet GetColors()
        {
            return _colors ?? UIStyles.Colors.Sets.Gray;
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

        protected override void OnEnabledChanged(System.EventArgs e)
        {
            base.OnEnabledChanged(e);

            // A disabled control gets no MouseUp: a press that was still going
            // (a handler that disables the box) must not outlive it.
            _isPressed = false;
            Invalidate();
        }

        // Native CheckBox.AutoSize defaults to true and measures a
        // preferred size for the SYSTEM checkbox glyph, which knows
        // nothing about our custom 16px box/padding layout. Left
        // enabled, this caused the real (already laid-out, resized,
        // relaid-out) PropertyTable instance to settle on
        // different bounds than what a single fresh construction+paint
        // produces - live screen captures (CopyFromScreen AND
        // PrintWindow, i.e. not a capture-tooling artifact) showed
        // leftover text fragments from neighboring rows/cells baked
        // into the checkbox's painted area, while a throwaway
        // freshly-constructed-then-immediately-DrawToBitmap probe never
        // reproduced it - consistent with a layout-history-dependent
        // bug, not a per-paint drawing bug. The factory now disables
        // AutoSize and assigns an explicitly computed Size instead, so
        // there is exactly one deterministic set of bounds regardless
        // of how many real layout passes the control goes through.
        internal static Size ComputePreferredSize(string text, Font font)
        {
            bool hasText = !string.IsNullOrEmpty(text);

            int width = hasText
                ? GlyphColumnWidth + TextRenderer.MeasureText(text, font).Width + 4
                : BoxSize + BoxLeftMargin * 2;

            int height = System.Math.Max(BoxSize + 8, font.Height + 8);

            return new Size(width, height);
        }

        // BackColor=Transparent (the previous approach) makes WinForms
        // route background painting through its "ask the parent to
        // render what's behind me" fake-transparency path - inside a
        // nested TableLayoutPanel (PropertyTable's editor cells)
        // that path picked up stale/sibling content (a neighboring
        // checkbox's or the row label's text), showing through as
        // ghosted text on real screen paints. DrawToBitmap didn't
        // reproduce it (WM_PRINT paints each control directly, bypassing
        // that compositing path), which is why it looked fine there.
        // Overriding this as a no-op guarantees our own OnPaint (which
        // already does a full manual erase to the parent's BackColor)
        // is the only thing that ever paints this control's background.
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
                return;

            // The box/border/checkmark are painted onto a private
            // per-call Bitmap first (see the constructor comment for
            // why - avoids WinForms' shared BufferedGraphicsManager
            // bleeding in a neighboring control's pixels), then blitted
            // onto e.Graphics.
            //
            // The label TEXT is different: TextRenderer.DrawText uses
            // GDI ClearType, which bakes in RGB sub-pixel color hinting
            // calibrated to the REAL display's physical pixel grid.
            // Rendering it onto an intermediate Bitmap and blitting that
            // bitmap to the screen moves those sub-pixel-colored pixels
            // to a target they were never calibrated for, showing up as
            // soft/fringed, faintly-doubled-looking text - confirmed by
            // the user directly on the live app, on EVERY checkbox's
            // text (not just disabled ones, since low-contrast colors
            // just make the artifact more visible, not more present).
            // So text is drawn as a separate, final step straight onto
            // e.Graphics - the actual on-screen surface - never through
            // the intermediate bitmap.
            using (Bitmap buffer = new Bitmap(Width, Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(buffer))
                    PaintShapes(g);

                e.Graphics.DrawImageUnscaled(buffer, 0, 0);
            }

            PaintText(e.Graphics);
            PaintFocus(e.Graphics);

            // Deliberately no base.OnPaint(e) call - see the class
            // comment above.
        }

        private Color GetParentBackColor()
        {
            return Parent != null
                ? Parent.BackColor
                : UIColors.BackgroundMedium;
        }

        private void PaintShapes(Graphics g)
        {
            using (SolidBrush eraseBrush = new SolidBrush(GetParentBackColor()))
                g.FillRectangle(eraseBrush, new Rectangle(0, 0, Width, Height));

            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool hasText = !string.IsNullOrEmpty(Text);

            int boxX = hasText
                ? BoxLeftMargin
                : (Width - BoxSize) / 2;
            int boxY = (Height - BoxSize) / 2;
            Rectangle boxRect = new Rectangle(boxX, boxY, BoxSize, BoxSize);

            // Filled when checked (or undetermined), outline only when not.
            // Like a button: the fill and the mark on it come from a color set
            // for rest, hovered and pressed; an unchecked box shows the state with
            // its border (and a faint tint while pressed). DisabledGray
            // substitutes for border and fill when disabled - a fixed mid-gray
            // (unlike TextDisabled, which is a theme role that differs between
            // Dark/Light), so a disabled checkbox looks identical in both themes,
            // the same way a disabled ToggleSwitch/button does.
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

            Color borderColor = !Enabled
                ? UIColors.DisabledGray
                : isActive && !isFilled
                    ? (_isPressed ? colors.Pressed : colors.Hover)
                    : UIColors.BorderMedium;

            using (GraphicsPath boxPath = CreateRoundedRectanglePath(boxRect, 3))
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

                using (Pen borderPen = new Pen(borderColor, 1.5f))
                    g.DrawPath(borderPen, boxPath);
            }

            if (CheckState == CheckState.Checked)
                DrawCheckmark(g, boxRect, markColor);
            else if (CheckState == CheckState.Indeterminate)
                DrawDash(g, boxRect, markColor);
        }

        private void PaintText(Graphics g)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            Rectangle textArea = new Rectangle(
                GlyphColumnWidth, 0, Width - GlyphColumnWidth, Height);

            // DisabledGray (120,120,120) is a fixed constant chosen
            // for the box/border/fill, which stay legible against it
            // regardless of theme since they're solid shapes. Fine
            // text is different: against Light theme's near-white
            // BackgroundLight, that same gray falls below a readable
            // contrast ratio (confirmed illegible in a live
            // screenshot, not just a theoretical contrast
            // calculation). Blend toward the ACTUAL background
            // instead - same technique buttons already use for
            // their disabled text - so contrast stays adequate in
            // both themes even though the literal color now differs
            // between them.
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
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        // The dotted frame of the keyboard focus, around the label and the box (or the
        // box alone when there is no label) - only once the keyboard has been used.
        private void PaintFocus(Graphics g)
        {
            if (!Focused || !ShowFocusCues)
                return;

            Rectangle area;

            if (string.IsNullOrEmpty(Text))
            {
                area = new Rectangle((Width - BoxSize) / 2 - 3, (Height - BoxSize) / 2 - 3, BoxSize + 6, BoxSize + 6);
            }
            else
            {
                int textWidth = TextRenderer.MeasureText(Text, Font).Width;
                int width = System.Math.Min(Width - 2, GlyphColumnWidth + textWidth + 2);
                area = new Rectangle(1, 1, width, Height - 2);
            }

            area.Intersect(new Rectangle(0, 0, Width, Height));
            ControlPaint.DrawFocusRectangle(g, area, UIColors.TextPrimary, GetParentBackColor());
        }

        private static void DrawDash(Graphics g, Rectangle box, Color color)
        {
            int y = box.Y + box.Height / 2;
            int left = box.X + (int)(box.Width * 0.26);
            int right = box.X + (int)(box.Width * 0.74);

            using (Pen pen = new Pen(color, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, left, y, right, y);
            }
        }

        private static void DrawCheckmark(Graphics g, Rectangle box, Color color)
        {
            Point p1 = new Point(box.X + (int)(box.Width * 0.22), box.Y + (int)(box.Height * 0.52));
            Point p2 = new Point(box.X + (int)(box.Width * 0.42), box.Y + (int)(box.Height * 0.74));
            Point p3 = new Point(box.X + (int)(box.Width * 0.80), box.Y + (int)(box.Height * 0.26));

            using (Pen pen = new Pen(color, 2f))
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
