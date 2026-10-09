using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The label of the library, made by <c>UIStyles.Labels</c>: the standard <see cref="Label"/> in the
    /// colors of the library, with a text that can move back and forth when it is too long
    /// (<see cref="ScrollWhenTooLong"/>).
    /// </summary>
    /// <remarks>
    /// The standard label does the painting - text, alignment, padding, ellipsis, mnemonic, border,
    /// image, transparent background - with the <see cref="ForeColor"/> this class answers. Only one
    /// thing it does not let us decide: a disabled label ignores its ForeColor and takes a fixed system
    /// gray (set ForeColor to pure red, the disabled text is still that gray). So a disabled label alone
    /// is painted here, in <c>UIColors.DisabledGray</c>, the one gray every other control uses for
    /// "disabled", so that it reads the same in every control and both themes. The moving text is painted
    /// here too: it needs a place of its own for every frame.
    /// </remarks>
    public sealed class ThemedLabel : Label
    {
        // The least contrast the text of a label with a color set has against what is behind it.
        private const double TextContrast = 4.5;

        // The text moves a little further than its end, so that the last sign is clear of the edge.
        private const int EndRoom = 2;

        private readonly Color _enabledForeColor;
        private readonly UIColorSet _colors;
        private bool _hasOwnForeColor;

        // Whether the label is on the beat of LabelAnimationClock, and whether it has been asked for a
        // picture that it has not made yet (so that a slow window is asked once, not many times).
        private bool _animating;
        private int _framePending;

        // The message that asks the label for its next picture, and the handle the clock posts it to
        // (kept here: the clock thread must not ask the control for its Handle).
        private const int WM_NEXT_PICTURE = WM_APP + 0x4C;
        private volatile IntPtr _postHandle;
        private bool _scrollWhenTooLong;
        private LabelScrollSpeed _scrollSpeed = LabelScrollSpeed.Medium;
        private LabelScrollPause _edgePause = LabelScrollPause.Medium;

        // The moving text: how far it is moved to the left, which way it goes, how far it can go,
        // how wide it is, until when it rests and when it was last moved.
        private float _offset;
        private float _paintedOffset;
        private int _direction = 1;
        private int _maxOffset;
        private int _textWidth;

        // The moving text as a picture of its own (grayscale smoothing on a clear background), made when
        // the text starts to move: the picture can be put at any place between two pixels, which the
        // text of the screen font cannot - and the text then moves without jerks.
        private Bitmap _picture;
        private Color _pictureColor;

        // A picture wider than this is not made: the text of such a label ends in an ellipsis.
        private const int MaxPictureWidth = 16000;
        private long _restUntil;
        private long _lastFrame;

        internal ThemedLabel(Color enabledForeColor, UIColorSet colors)
        {
            _enabledForeColor = enabledForeColor;
            _colors = colors;
        }

        // The time of the moving text. Replaceable so a test can let time pass.
        internal static Func<long> Milliseconds = () => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

        // ---- the moving text ----

        /// <summary>
        /// Whether a text that is too long for the label moves back and forth instead of ending in an
        /// ellipsis: it rests at the start, moves to its end, rests there, moves back, and so on.
        /// A text that fits stands still. Off by default. It applies to a text of one line in a label
        /// that is enabled; a disabled label shows the start of the text with an ellipsis.
        /// </summary>
        public bool ScrollWhenTooLong
        {
            get { return _scrollWhenTooLong; }
            set
            {
                _scrollWhenTooLong = value;
                Restart();
            }
        }

        /// <summary>How fast the moving text moves: slowly, at a medium speed (the default) or fast.</summary>
        public LabelScrollSpeed ScrollSpeed
        {
            get { return _scrollSpeed; }
            set { _scrollSpeed = value; }
        }

        /// <summary>How long the moving text rests at the start and at the end: a short time, a medium one (the default) or a long one.</summary>
        public LabelScrollPause EdgePause
        {
            get { return _edgePause; }
            set { _edgePause = value; }
        }

        // Back to the start, and to the question whether the text moves at all. Called whenever
        // something changes that the answer depends on.
        private void Restart()
        {
            _offset = 0f;
            _paintedOffset = 0f;
            DisposePicture();
            _direction = 1;
            _maxOffset = MeasureMaxOffset();
            _restUntil = Milliseconds() + _edgePause.ToMilliseconds();
            _lastFrame = Milliseconds();

            bool moves = _maxOffset > 0 && Visible && IsHandleCreated;

            if (moves && !_animating)
            {
                _animating = true;
                LabelAnimationClock.Add(this);
            }
            else if (!moves && _animating)
            {
                StopAnimating();
            }

            Invalidate();
        }

        // How far the text has to move to show its end: 0 when it fits, or when it does not move at all
        // (off, disabled, more than one line).
        private int MeasureMaxOffset()
        {
            _textWidth = 0;

            if (!_scrollWhenTooLong || !Enabled || AutoSize || string.IsNullOrEmpty(Text) || Text.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                return 0;

            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (StringFormat format = CreateMovingTextFormat())
            {
                _textWidth = (int)Math.Ceiling(graphics.MeasureString(Text, Font, PointF.Empty, format).Width);
            }

            if (_textWidth > MaxPictureWidth)
                return 0;

            return Math.Max(0, _textWidth + 2 * GetTextInset() - (ClientSize.Width - Padding.Horizontal) + EndRoom);
        }

        private void StopAnimating()
        {
            _animating = false;
            LabelAnimationClock.Remove(this);
        }

        // Called from the thread of the clock: ask the window for the next picture, once.
        internal void RequestFrame()
        {
            IntPtr handle = _postHandle;

            if (handle == IntPtr.Zero || Interlocked.Exchange(ref _framePending, 1) == 1)
                return;

            if (!PostMessage(handle, WM_NEXT_PICTURE, IntPtr.Zero, IntPtr.Zero))
                Interlocked.Exchange(ref _framePending, 0);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NEXT_PICTURE)
            {
                MakeFrame();

                return;
            }

            base.WndProc(ref m);
        }

        private void MakeFrame()
        {
            Interlocked.Exchange(ref _framePending, 0);

            if (!_animating || IsDisposed)
                return;

            if (Step() && IsHandleCreated)
            {
                // At once, not when the queue has nothing better to do: the picture belongs to this beat.
                Update();
            }
        }

        // One step of the movement; the time that has passed decides how far, so the speed does not
        // depend on how often the timer manages to tick.
        private void OnFrame(object sender, EventArgs e)
        {
            Step();
        }

        // One step of the movement; whether the text has moved (then it is invalidated).
        private bool Step()
        {
            long now = Milliseconds();
            long from = Math.Max(_lastFrame, _restUntil);
            _lastFrame = now;

            if (now <= _restUntil)
                return false;

            _offset += _direction * _scrollSpeed.ToPixelsPerSecond() * (now - from) / 1000f;

            if (_direction > 0 && _offset >= _maxOffset)
            {
                _offset = _maxOffset;
                _direction = -1;
                _restUntil = now + _edgePause.ToMilliseconds();
            }
            else if (_direction < 0 && _offset <= 0f)
            {
                _offset = 0f;
                _direction = 1;
                _restUntil = now + _edgePause.ToMilliseconds();
            }

            // A picture only when the text has moved at all (not while it rests).
            if (_offset == _paintedOffset)
                return false;

            _paintedOffset = _offset;
            Invalidate();

            return true;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Restart();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Restart();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Restart();
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            Restart();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Restart();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Restart();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _postHandle = Handle;
            Restart();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _postHandle = IntPtr.Zero;

            if (_animating)
                StopAnimating();

            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_animating)
                    StopAnimating();

                DisposePicture();
            }

            base.Dispose(disposing);
        }

        // ---- painting ----

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Enabled)
            {
                // The background is already painted (the label's own, or the page's behind a
                // transparent one); what is left is the text, as the standard label would lay it out.
                Rectangle textArea = new Rectangle(
                    Padding.Left,
                    Padding.Top,
                    Width - Padding.Horizontal,
                    Height - Padding.Vertical);

                TextRenderer.DrawText(e.Graphics, Text, Font, textArea, GetTextColor(), GetTextFormatFlags());

                return;
            }

            if (_maxOffset > 0)
            {
                PaintMovingText(e.Graphics);

                return;
            }

            base.OnPaint(e);
        }

        // The picture of the text from the place it has moved to, shown only inside the label (the padding
        // stays free). The place may lie between two pixels; the picture is then mixed between them.
        private void PaintMovingText(Graphics graphics)
        {
            Rectangle inner = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, ClientSize.Width - Padding.Horizontal),
                Math.Max(0, ClientSize.Height - Padding.Vertical));

            Bitmap picture = GetPicture();
            float x = inner.Left + GetTextInset() - _offset;
            int y = GetPictureTop(inner, picture.Height);

            GraphicsState state = graphics.Save();

            try
            {
                graphics.IntersectClip(inner);
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(
                    picture,
                    new RectangleF(x, y, picture.Width, picture.Height),
                    new RectangleF(0, 0, picture.Width, picture.Height),
                    GraphicsUnit.Pixel);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // The room the screen font puts before the text (a few pixels, as much as the text of a label that
        // does not move has), so that the text does not jump when the label starts to move.
        private int GetTextInset()
        {
            Size big = new Size(int.MaxValue, int.MaxValue);
            int padded = TextRenderer.MeasureText("A", Font, big, TextFormatFlags.Default).Width;
            int bare = TextRenderer.MeasureText("A", Font, big, TextFormatFlags.NoPadding).Width;

            return Math.Max(0, (padded - bare) / 2);
        }

        private int GetPictureTop(Rectangle inner, int height)
        {
            switch (GetVerticalFlag(TextAlign))
            {
                case TextFormatFlags.Top:
                    return inner.Top;
                case TextFormatFlags.Bottom:
                    return inner.Bottom - height;
                default:
                    return inner.Top + (inner.Height - height) / 2;
            }
        }

        // The picture of the text in the color it has now.
        private Bitmap GetPicture()
        {
            Color color = ForeColor;

            if (_picture != null && _pictureColor == color)
                return _picture;

            DisposePicture();

            Bitmap picture = new Bitmap(Math.Max(1, _textWidth + EndRoom), Math.Max(1, (int)Math.Ceiling(Font.GetHeight())) + 2, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(picture))
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = CreateMovingTextFormat())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.Transparent);
                graphics.DrawString(Text, Font, brush, new PointF(0f, 1f), format);
            }

            _picture = picture;
            _pictureColor = color;

            return _picture;
        }

        private void DisposePicture()
        {
            if (_picture != null)
            {
                _picture.Dispose();
                _picture = null;
            }
        }

        // One line from the left, as it is, without the room GDI+ would add around it; what an ampersand is
        // follows UseMnemonic.
        private StringFormat CreateMovingTextFormat()
        {
            StringFormat format = new StringFormat(StringFormat.GenericTypographic);
            format.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
            format.HotkeyPrefix = !UseMnemonic ? HotkeyPrefix.None : ShowKeyboardCues ? HotkeyPrefix.Show : HotkeyPrefix.Hide;

            return format;
        }

        // ---- colors ----

        // Like the standard label: a ForeColor that was set is the text color; set back to
        // empty, the label has its own color again. Read without one, it says the color the
        // text has when the label is enabled.
        public override Color ForeColor
        {
            get { return GetEnabledTextColor(); }
            set
            {
                _hasOwnForeColor = !value.IsEmpty;
                base.ForeColor = value;
                Invalidate();
            }
        }

        // Disabled, always the one gray of the library, whatever the ForeColor.
        internal Color GetTextColor()
        {
            return Enabled ? GetEnabledTextColor() : UIColors.DisabledGray;
        }

        // A ForeColor that was set wins. With a color set the text takes the main color of
        // the set (the one its controls show hovered), lightened or darkened where the page
        // behind it needs that to read; the page is looked at when the label is painted, so
        // a theme change is followed.
        private Color GetEnabledTextColor()
        {
            // (Before the constructor has run, the base class may already ask.)
            if (_hasOwnForeColor || _enabledForeColor.IsEmpty)
                return base.ForeColor;

            if (_colors == null)
                return _enabledForeColor;

            return UIColors.EnsureContrast(_colors.Hover, GetEffectiveBackColor(), TextContrast);
        }

        private Color GetEffectiveBackColor()
        {
            if (BackColor.A > 0)
                return BackColor;

            for (Control ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor.BackColor.A > 0)
                    return ancestor.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        // ---- how text is laid out ----

        // What an ampersand is: a literal one, or - with UseMnemonic - the sign for the key that
        // reaches the next control (underlined only while the keyboard cues are shown).
        private TextFormatFlags GetPrefixFlags()
        {
            if (!UseMnemonic)
                return TextFormatFlags.NoPrefix;

            return ShowKeyboardCues ? TextFormatFlags.Default : TextFormatFlags.HidePrefix;
        }

        // How the disabled text is laid out, as the standard label does it: the alignment, what
        // the text does at the end of a line, and what an ampersand is.
        internal TextFormatFlags GetTextFormatFlags()
        {
            TextFormatFlags flags = GetHorizontalFlag(TextAlign) | GetVerticalFlag(TextAlign) | GetPrefixFlags();

            // Like the standard label: a fixed-size label either cuts
            // its text with an ellipsis or wraps it. An auto-sized one
            // is exactly as big as its text - nothing to cut - and only
            // wraps when it has a maximum width to wrap at.
            if (AutoSize)
            {
                if (MaximumSize.Width > 0)
                    flags |= TextFormatFlags.WordBreak;
            }
            else
            {
                flags |= AutoEllipsis ? TextFormatFlags.EndEllipsis : TextFormatFlags.WordBreak;
            }

            return flags;
        }

        private static TextFormatFlags GetHorizontalFlag(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    return TextFormatFlags.Left;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    return TextFormatFlags.HorizontalCenter;
                default:
                    return TextFormatFlags.Right;
            }
        }

        private static TextFormatFlags GetVerticalFlag(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    return TextFormatFlags.Top;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    return TextFormatFlags.Bottom;
                default:
                    return TextFormatFlags.VerticalCenter;
            }
        }
    }
}
