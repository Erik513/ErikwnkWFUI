using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Factories
{
    // A button for symbols. The native button centers the text by its
    // advance width and line height, which for symbol glyphs is not where
    // the glyph actually sits - they come out a pixel or several off, and
    // the more so the larger the button. So this one draws itself: fill,
    // outline and the symbol, moved so that the visible part of the glyph
    // is centered. Colors come from the same places as on any button
    // (BackColor, ForeColor, FlatAppearance), so the state handling of the
    // factory works unchanged.
    internal sealed class SymbolButton : Button
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
            SetStyle(ControlStyleSets.OwnerDrawn | ControlStyles.Opaque, true);
        }

        // A button like on a tool bar: a click does not take the focus (and the tab key skips it). For buttons that
        // work on something else, so that the focus stays where the user was - and does not jump on when the button
        // is disabled by its own click.
        internal void TakeNoFocus()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
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

            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Black);
                    TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), Color.White, SymbolFlags);
                }

                // The pixels are read in one piece (GetPixel one by one is slow, and this runs again whenever the
                // text, the font or the size changes).
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[data.Stride * Height];

                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }

                int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

                for (int y = 0; y < Height; y++)
                {
                    int row = y * data.Stride;

                    for (int x = 0; x < Width; x++)
                    {
                        int at = row + x * 4; // blue, green, red, alpha

                        // All three channels: ClearType colors the edges of a glyph
                        // differently per channel, one alone would tilt the result.
                        if (pixels[at] + pixels[at + 1] + pixels[at + 2] <= InkThreshold * 3)
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
