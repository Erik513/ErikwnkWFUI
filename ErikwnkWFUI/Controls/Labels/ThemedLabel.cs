using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

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

        // How often the moving text is drawn anew (about 60 times a second).
        private const int FrameMilliseconds = 16;

        // The text moves a little further than its end, so that the last sign is clear of the edge.
        private const int EndRoom = 2;

        private readonly Color _enabledForeColor;
        private readonly UIColorSet _colors;
        private bool _hasOwnForeColor;

        private readonly Timer _timer = new Timer { Interval = FrameMilliseconds };
        private bool _scrollWhenTooLong;
        private LabelScrollSpeed _scrollSpeed = LabelScrollSpeed.Medium;
        private LabelScrollPause _edgePause = LabelScrollPause.Medium;

        // The moving text: how far it is moved to the left, which way it goes, how far it can go,
        // how wide it is, until when it rests and when it was last moved.
        private float _offset;
        private int _direction = 1;
        private int _maxOffset;
        private int _textWidth;
        private long _restUntil;
        private long _lastFrame;

        internal ThemedLabel(Color enabledForeColor, UIColorSet colors)
        {
            _enabledForeColor = enabledForeColor;
            _colors = colors;

            _timer.Tick += OnFrame;
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
            _direction = 1;
            _maxOffset = MeasureMaxOffset();
            _restUntil = Milliseconds() + _edgePause.ToMilliseconds();
            _lastFrame = Milliseconds();

            bool moves = _maxOffset > 0 && Visible && IsHandleCreated;

            if (moves && !_timer.Enabled)
                _timer.Start();
            else if (!moves && _timer.Enabled)
                _timer.Stop();

            Invalidate();
        }

        // How far the text has to move to show its end: 0 when it fits, or when it does not move at all
        // (off, disabled, more than one line).
        private int MeasureMaxOffset()
        {
            _textWidth = 0;

            if (!_scrollWhenTooLong || !Enabled || AutoSize || string.IsNullOrEmpty(Text) || Text.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                return 0;

            _textWidth = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), GetMovingTextFlags()).Width;

            return Math.Max(0, _textWidth - (ClientSize.Width - Padding.Horizontal) + EndRoom);
        }

        // One step of the movement; the time that has passed decides how far, so the speed does not
        // depend on how often the timer manages to tick.
        private void OnFrame(object sender, EventArgs e)
        {
            long now = Milliseconds();
            long from = Math.Max(_lastFrame, _restUntil);
            _lastFrame = now;

            if (now <= _restUntil)
                return;

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

            Invalidate();
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
            Restart();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Tick -= OnFrame;
                _timer.Dispose();
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

        // The text from the place it has moved to, shown only inside the label (the padding stays free).
        private void PaintMovingText(Graphics graphics)
        {
            Rectangle inner = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, ClientSize.Width - Padding.Horizontal),
                Math.Max(0, ClientSize.Height - Padding.Vertical));

            Rectangle textArea = new Rectangle(
                inner.Left - (int)Math.Round(_offset),
                inner.Top,
                _textWidth + EndRoom,
                inner.Height);

            GraphicsState state = graphics.Save();

            try
            {
                graphics.IntersectClip(inner);
                // (Without PreserveGraphicsClipping the text would ignore the clip and show in the padding.)
                TextRenderer.DrawText(graphics, Text, Font, textArea, ForeColor,
                    GetMovingTextFlags() | GetVerticalFlag(TextAlign) | TextFormatFlags.PreserveGraphicsClipping);
            }
            finally
            {
                graphics.Restore(state);
            }
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

        // The moving text: one line, from the left, no ellipsis.
        private TextFormatFlags GetMovingTextFlags()
        {
            return TextFormatFlags.Left | GetPrefixFlags();
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
