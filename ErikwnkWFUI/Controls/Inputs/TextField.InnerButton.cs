using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // A button in the box, at its right end, inside the frame (the pencil of a box that can be unlocked, the eye of a password box): a
    // margin of the text keeps the text away from it, it is painted over what the system painted, and the mouse messages over it are
    // taken before the box sees them (so a click on it does not set the caret).
    public partial class TextField
    {
        private const int EM_SETMARGINS = 0x00D3;
        private const int EC_LEFTMARGIN = 0x0001;
        private const int EC_RIGHTMARGIN = 0x0002;
        private const int EC_USEFONTINFO = 0xFFFF;
        private const int LeftMargin96 = 4;
        // (The frame is painted over the last column of the window, and the text runs up to the margin: five at the right leave as much room
        // between the text and the frame as four do at the left, where the frame is the first column and the letters have a little room of their own.)
        private const int RightMargin96 = 5;

        // The width of the edge at the right that no letter reaches (the frame is painted over its last column).
        private const int RightEdge96 = 6;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int InnerWidth96 = 24;
        private const float InnerFontSize96 = 10f;

        private string _innerGlyph;
        private string _innerToolTip = "";
        private bool _innerActive;
        private bool _innerHot;
        private bool _innerPressed;
        private Font _innerFont;
        private int _innerFontDpi;

        /// <summary>Raised when the button in the box has been clicked.</summary>
        internal event EventHandler InnerClick;

        /// <summary>The symbol of the button in the box; null (the default) is none.</summary>
        internal string InnerGlyph
        {
            get { return _innerGlyph; }
            set
            {
                _innerGlyph = value;
                ApplyInnerMargin();
                Invalidate();
            }
        }

        /// <summary>What the tooltip says while the mouse is over the button.</summary>
        internal string InnerToolTip
        {
            get { return _innerToolTip; }
            set
            {
                _innerToolTip = value ?? "";
                UpdateToolTip(_innerHot);
            }
        }

        /// <summary>Whether the button is switched on (the eye of a password that is shown): it is painted in the color of the frame.</summary>
        internal bool InnerActive
        {
            get { return _innerActive; }
            set
            {
                _innerActive = value;
                Invalidate();
            }
        }

        /// <summary>The width that the button takes, 0 without one.</summary>
        internal int InnerWidth
        {
            get { return _innerGlyph == null ? 0 : Dpi.Scale(this, InnerWidth96); }
        }

        // Where the button is, in the client area of the box (at its right end).
        internal Rectangle InnerBounds
        {
            get
            {
                Rectangle client = ClientRectangle;

                // (A box of several lines has its button at the top, as high as one line, not as high as the whole box.)
                int height = Multiline ? Math.Min(client.Height, Dpi.Scale(this, InnerWidth96)) : client.Height;

                return new Rectangle(client.Right - InnerWidth, client.Top, InnerWidth, height);
            }
        }

        // The text keeps away from the button: the right margin of the box (without a button a little room, so that the text does not touch
        // the frame at the right either). And a box with a frame has room between the frame and the text at the left (the system leaves one
        // or two pixels, which looked as if the text touched the frame); one without a frame keeps the narrow margin of the font.
        private void ApplyInnerMargin()
        {
            if (!IsHandleCreated)
                return;

            bool framed = BorderStyle != BorderStyle.None;
            int left = framed ? Dpi.Scale(this, LeftMargin96) : EC_USEFONTINFO;
            int right = InnerWidth > 0 ? InnerWidth : framed ? Dpi.Scale(this, RightMargin96) : EC_USEFONTINFO;

            SendMessage(Handle, EM_SETMARGINS, (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), (IntPtr)((right << 16) | left));
        }

        protected override void OnBorderStyleChanged(EventArgs e)
        {
            base.OnBorderStyleChanged(e);
            ApplyInnerMargin();
        }

        private static Point GetPoint(IntPtr lParam)
        {
            long value = (long)lParam;

            return new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
        }

        private bool IsOverInnerButton(Point point)
        {
            return _innerGlyph != null && InnerBounds.Contains(point);
        }

        // The mouse over the button: the press and the release are the button's, nothing else sees them.
        private bool HandleInnerButtonMessage(ref Message m)
        {
            if (_innerGlyph == null || !Enabled)
                return false;

            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
            {
                if (!IsOverInnerButton(GetPoint(m.LParam)))
                    return false;

                _innerPressed = true;
                Capture = true;
                Invalidate();

                return true;
            }

            if (m.Msg == WM_LBUTTONUP && _innerPressed)
            {
                bool inside = IsOverInnerButton(GetPoint(m.LParam));
                _innerPressed = false;
                Capture = false;
                Invalidate();

                if (inside)
                    OnInnerClick();

                return true;
            }

            if (m.Msg == WM_SETCURSOR && _innerHot)
            {
                Cursor.Current = Cursors.Hand;

                return true;
            }

            return false;
        }

        private void UpdateInnerHot(Point point)
        {
            bool hot = _innerGlyph != null && Enabled && IsOverInnerButton(point);

            if (hot == _innerHot)
                return;

            _innerHot = hot;
            Invalidate();
            UpdateToolTip(hot);
        }

        // A disabled box gets no MouseLeave, and a window that takes the mouse away no release: nothing stays hot or pressed
        // (see also OnEnabledChanged of the box).
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                ResetInnerState();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);

            if (_innerPressed && !Capture)
                ResetInnerState();
        }

        internal void ResetInnerState()
        {
            if (!_innerHot && !_innerPressed)
                return;

            _innerHot = false;
            _innerPressed = false;
            Invalidate();
            UpdateToolTip(false);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_innerHot || _innerPressed)
            {
                _innerHot = false;
                Invalidate();
                UpdateToolTip(false);
            }
        }

        protected virtual void OnInnerClick()
        {
            InnerClick?.Invoke(this, EventArgs.Empty);
        }

        // The symbol in the font of the symbols, at the size of the screen.
        private Font GetInnerFont()
        {
            int dpi = DeviceDpi;

            if (_innerFont == null || _innerFontDpi != dpi)
            {
                if (_innerFont != null)
                    _innerFont.Dispose();

                _innerFont = new Font("Segoe UI Symbol", InnerFontSize96 * dpi / 96f, FontStyle.Regular);
                _innerFontDpi = dpi;
            }

            return _innerFont;
        }

        private void PaintInnerButton(Graphics graphics)
        {
            if (_innerGlyph == null || !IsHandleCreated)
                return;

            Rectangle bounds = InnerBounds;
            Color back = BackColor;
            Color fore = Enabled ? ForeColor : UIColors.GetDisabledText(back);

            // The state: a soft field behind the symbol while the mouse is over it or it is pressed.
            if (Enabled && (_innerHot || _innerPressed))
            {
                Rectangle field = Rectangle.Inflate(bounds, -Dpi.Scale(this, 2), -Dpi.Scale(this, 3));

                using (SolidBrush brush = new SolidBrush(UIColors.BlendToward(back, fore, _innerPressed ? 0.22 : 0.12)))
                    graphics.FillRectangle(brush, field);
            }

            // The symbol: quiet (muted), the text color while the mouse is over it, the color of the frame while it is switched on.
            // (Disabled: fainter than at rest, 2:1 and a little more.)
            // (A box with a color set: the symbol has the color of the set whichever symbol it is - the pencil as well as the check mark.)
            Color glyph = !Enabled ? UIColors.BlendToward(UIColors.GetBestTextColor(back), back, 0.65)
                : _innerActive || Colors != null ? InputField.GetFrameColor(this, Colors, true)
                : _innerHot || _innerPressed ? fore
                : UIColors.GetMutedText(back, 0.45, 3.5);

            TextRenderer.DrawText(graphics, _innerGlyph, GetInnerFont(), bounds, glyph,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }
}
