using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The moving text of ThemedLabel: the settings, the measure, the steps of the movement on the beat of
    // LabelAnimationClock, the picture of the text and how it is painted. The colors, the disabled text and
    // the dispatch of the painting are in ThemedLabel.cs.
    public sealed partial class ThemedLabel
    {
        // ---- the constants ----

        // The moving text goes a little further than its end, so that the last sign is clear of the edge.
        private const int EndRoom = 2;

        // A picture wider than this is not made: the text of such a label ends in an ellipsis.
        private const int MaxPictureWidth = 16000;

        // The message that asks the label for its next picture, and the one that tells it that a setting of
        // Windows has changed.
        private const int WM_NEXT_PICTURE = WM_APP + 0x4C;
        private const int WM_SETTING_CHANGED = WM_APP + 0x4D;

        // ---- the state ----

        // The settings (see the properties below).
        private LabelScrollMode _scrollMode = LabelScrollMode.None;
        private LabelScrollSpeed _scrollSpeed = LabelScrollSpeed.Medium;
        private LabelScrollPause _scrollPause = LabelScrollPause.Medium;
        private LabelScrollDirection _scrollDirection = LabelScrollDirection.Left;
        private LabelScrollSeparator _scrollSeparator = LabelScrollSeparator.Dot;
        private bool _scrollPauseOnHover;

        // Whether the mouse is on the label now (for the pause on hover).
        private bool _hovered;

        // The movement. How wide the text is; back and forth: how far the text can go to show its end; endless: how long
        // one round is (the text, a gap, the separator, a gap). Of the last two, at most one is more than 0,
        // and both are 0 while the text does not move.
        private int _textWidth;
        private int _maxOffset;
        private int _cycle;
        private int _separatorWidth;

        // Several texts that move one behind the other (the Scroll that was set says so), the text they were joined
        // to for the label, and how wide each one is. Null while the text of the label moves alone.
        private string[] _texts;
        private string _textsJoined;
        private int[] _pieceWidths;

        // How far the text has moved to the left (to the right when it is endless and goes that way), the
        // offset of the last picture, and - back and forth only - which way it goes now (1 to its end, -1
        // back), until when it rests and when it was last moved.
        private float _offset;
        private float _paintedOffset;
        private int _direction = 1;
        private long _restUntil;
        private long _lastFrame;

        // The moving text as a picture of its own (grayscale smoothing on a clear background), made when
        // the text starts to move: the picture can be put at any place between two pixels, which the
        // text of the screen font cannot - and the text then moves without jerks.
        private Bitmap _picture;
        private Color _pictureColor;

        // What an ampersand was in the last measure and in the last picture (see GetHotkeyPrefix).
        private HotkeyPrefix _measuredPrefix;
        private HotkeyPrefix _pictureHotkeyPrefix;

        // Whether the label is on the beat of LabelAnimationClock, whether it has been asked for a picture
        // that it has not made yet (so that a slow window is asked once, not many times), and the handle the
        // clock posts to (kept here: the clock thread must not ask the control for its Handle).
        private bool _animating;
        private int _framePending;
        private volatile IntPtr _postHandle;

        // Whether the label waits for LabelSettingsWatcher (it moves, or could), and how much room the screen
        // font puts before the text (measured with the text, see Measure).
        private bool _watching;
        private int _textInset;

        // ---- the seams of the tests ----

        // Whether Windows lets windows animate (Settings, Accessibility, "Animation effects"). Replaceable so a test
        // can switch it.
        internal static Func<bool> AnimationsEnabled = ReadAnimationSetting;

        private static bool ReadAnimationSetting()
        {
            int on = 1;

            // Where Windows cannot say, the animation is allowed.
            return !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) || on != 0;
        }

        // The time of the moving text. Replaceable so a test can let time pass.
        internal static Func<long> Milliseconds = () => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

        // ---- the settings of the movement ----

        /// <summary>
        /// Whether and how a text that is too long for the label moves instead of ending in an ellipsis:
        /// not at all (the default), back and forth with a rest at both ends, or endlessly in one
        /// direction. A text that fits stands still. It applies to a text of one line in a label that is
        /// enabled; a disabled label shows the start of the text with an ellipsis. A label with
        /// <see cref="Label.AutoSize"/> is as big as its text, so back and forth does not move it; endless does, and
        /// the size of the label stays what AutoSize makes it (a label with a maximum width wraps its text and does not move).
        /// With several texts (<see cref="LabelScroll.Texts"/>) AutoSize is not of much use: the label is as wide as all
        /// the texts together, so there is little left to move. While the text moves it starts at the left whatever <see cref="Label.TextAlign"/>
        /// says (the vertical place is kept), and right-to-left is not supported (as everywhere in the library).
        /// An image of the label is not shown while the text moves, only while it stands still.
        /// </summary>
        public LabelScrollMode ScrollMode
        {
            get { return _scrollMode; }
            set
            {
                _scrollMode = value;
                Restart();
            }
        }

        /// <summary>How fast the moving text moves: slowly, at a medium speed (the default) or fast.</summary>
        public LabelScrollSpeed ScrollSpeed
        {
            get { return _scrollSpeed; }
            set { _scrollSpeed = value; }
        }

        /// <summary>How long the text rests at the start and at the end when it moves back and forth: a short time, a medium one (the default) or a long one.</summary>
        public LabelScrollPause ScrollPause
        {
            get { return _scrollPause; }
            set { _scrollPause = value; }
        }

        /// <summary>Whether the moving text stands still while the mouse is on the label, so that it can be read. Off by default.</summary>
        public bool ScrollPauseOnHover
        {
            get { return _scrollPauseOnHover; }
            set { _scrollPauseOnHover = value; }
        }

        /// <summary>Which way the text moves in the endless mode: to the left (the default) or to the right. Back and forth always starts to the left.</summary>
        public LabelScrollDirection ScrollDirection
        {
            get { return _scrollDirection; }
            set
            {
                _scrollDirection = value;
                Restart();
            }
        }

        /// <summary>The sign between the end of the text and its next appearance in the endless mode: a dot (the default), a bar, a diamond or none.</summary>
        public LabelScrollSeparator ScrollSeparator
        {
            get { return _scrollSeparator; }
            set
            {
                _scrollSeparator = value;

                // (The text the label shows where it does not move names the separator too.)
                if (_texts != null)
                    ShowJoinedTexts();

                Restart();
            }
        }

        /// <summary>
        /// All of the above in one value: set to take over every setting at once (the movement starts over
        /// once), read to get the settings the label has now. Null is no movement.
        /// </summary>
        public LabelScroll Scroll
        {
            get
            {
                switch (_scrollMode)
                {
                    case LabelScrollMode.BackAndForth:
                        return LabelScroll.BackAndForth(_scrollSpeed, _scrollPause, _scrollPauseOnHover);
                    case LabelScrollMode.Endless:
                        return _texts != null
                            ? LabelScroll.Endless(_texts, _scrollDirection, _scrollSeparator, _scrollSpeed, _scrollPauseOnHover)
                            : LabelScroll.Endless(_scrollDirection, _scrollSeparator, _scrollSpeed, _scrollPauseOnHover);
                    default:
                        return LabelScroll.None;
                }
            }
            set
            {
                LabelScroll scroll = value ?? LabelScroll.None;

                _scrollSpeed = scroll.Speed;
                _scrollPause = scroll.Pause;
                _scrollDirection = scroll.Direction;
                _scrollSeparator = scroll.Separator;
                _scrollPauseOnHover = scroll.PauseOnHover;
                _scrollMode = scroll.Mode;
                _texts = scroll.Texts?.ToArray();
                _textsJoined = null;

                if (_texts != null)
                    ShowJoinedTexts();

                Restart();
            }
        }

        // The label shows the texts one after the other, the separator between them, where it does not move.
        private void ShowJoinedTexts()
        {
            string sign = _scrollSeparator.ToText();
            _textsJoined = string.Join(sign.Length > 0 ? " " + sign + " " : " ", _texts);
            Text = _textsJoined;
        }

        // ---- starting and measuring ----

        // Back to the start, and to the question whether the text moves at all. Called whenever
        // something changes that the answer depends on.
        private void Restart()
        {
            _offset = 0f;
            _paintedOffset = 0f;
            DisposePicture();
            _direction = 1;
            Measure();

            // Back and forth rests at the start first (the endless text goes on at once and ignores the rest).
            _restUntil = Milliseconds() + _scrollPause.ToMilliseconds();
            _lastFrame = Milliseconds();

            bool wantsSettings = _scrollMode != LabelScrollMode.None && IsHandleCreated;

            if (wantsSettings && !_watching)
            {
                _watching = true;
                LabelSettingsWatcher.Add(this);
            }
            else if (!wantsSettings && _watching)
            {
                _watching = false;
                LabelSettingsWatcher.Remove(this);
            }

            bool moves = (_maxOffset > 0 || _cycle > 0) && Visible && IsHandleCreated;

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

        // How the text has to move: back and forth needs to know how far to go to show its end, the endless
        // mode how long one round is. Neither when the text does not move at all (off, disabled, back and forth with a
        // text that fits, an empty text, more than one line).
        private void Measure()
        {
            _textWidth = 0;
            _maxOffset = 0;
            _cycle = 0;
            _separatorWidth = 0;
            _pieceWidths = null;

            if (_scrollMode == LabelScrollMode.None || !Enabled || !AnimationsEnabled() || (AutoSize && !MovesWhenAutoSized()) || string.IsNullOrWhiteSpace(Text) || Text.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                return;

            int inner = ClientSize.Width - Padding.Horizontal;
            int inset = _textInset = GetTextInset();
            int separatorWidth = 0;

            _measuredPrefix = GetHotkeyPrefix();

            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (StringFormat format = CreateMovingTextFormat())
            {
                if (_scrollMode == LabelScrollMode.Endless && _texts != null)
                {
                    _pieceWidths = _texts.Select(t => (int)Math.Ceiling(graphics.MeasureString(t, Font, PointF.Empty, format).Width)).ToArray();
                    _textWidth = _pieceWidths.Sum();
                }
                else
                {
                    _textWidth = (int)Math.Ceiling(graphics.MeasureString(Text, Font, PointF.Empty, format).Width);
                }

                string separator = _scrollSeparator.ToText();

                if (separator.Length > 0)
                    separatorWidth = _separatorWidth = (int)Math.Ceiling(graphics.MeasureString(separator, Font, PointF.Empty, format).Width);
            }

            if (_scrollMode == LabelScrollMode.Endless)
            {
                int gap = GetGap();

                // One round is the text, a gap, the separator and a gap - and at least as wide as the label, so
                // that a short text keeps the room the label has for it (a word of 10 pixels in a label of 50
                // shows once, with 40 pixels of room behind it, and comes back after them). The endless text
                // moves whether it fits or not.
                int pieces = _pieceWidths?.Length ?? 1;
                int cycle = Math.Max(_textWidth + pieces * (gap + separatorWidth + gap), inner);

                if (cycle <= MaxPictureWidth)
                    _cycle = cycle;

                return;
            }

            // One that fits (even just) stands still; only one that does not fit has a way to go.
            if (_textWidth + 2 * inset > inner && _textWidth <= MaxPictureWidth)
                _maxOffset = _textWidth + 2 * inset - inner + EndRoom;
        }

        // A label with AutoSize is exactly as big as its text, so back and forth has nothing to show; the endless
        // text goes round all the same (the size of the label is not touched: the round is the text, a gap, the
        // separator and a gap, seen through a window of the text's width). One that wraps (a maximum width) is
        // not one line.
        private bool MovesWhenAutoSized()
        {
            return _scrollMode == LabelScrollMode.Endless && MaximumSize.Width <= 0;
        }

        // The gap before and behind the separator of the endless mode: about the height of a letter.
        private int GetGap()
        {
            return Math.Max(4, (int)Math.Round(Font.GetHeight()));
        }

        // ---- the beat of the clock and the steps ----

        private void StopAnimating()
        {
            _animating = false;
            LabelAnimationClock.Remove(this);
        }

        // Called from the thread of the system events: tell the window that a setting of Windows has changed.
        internal void NotifySettingChanged()
        {
            IntPtr handle = _postHandle;

            if (handle != IntPtr.Zero)
                PostMessage(handle, WM_SETTING_CHANGED, IntPtr.Zero, IntPtr.Zero);
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

            // The user may have switched the animations of Windows on or off: the text moves, or stops, at once.
            if (m.Msg == WM_SETTING_CHANGED)
            {
                Restart();

                return;
            }

            base.WndProc(ref m);
        }

        private void MakeFrame()
        {
            Interlocked.Exchange(ref _framePending, 0);

            if (!_animating || IsDisposed)
                return;

            // A window that is hidden does not tell its labels (they get no VisibleChanged): the clock
            // finds out here, and the label is on it again when it is shown (VisibleChanged then comes).
            if (!Visible)
            {
                StopAnimating();

                return;
            }

            if (Step() && IsHandleCreated)
            {
                // At once, not when the queue has nothing better to do: the picture belongs to this beat.
                Update();
            }
        }

        // One beat without the painting: what the tests call to let the clock beat when they want to look at
        // the movement only.
        private void OnFrame(object sender, EventArgs e)
        {
            Step();
        }

        // One step of the movement, back and forth: it counts the time that has passed since the last step,
        // so the speed does not depend on how often the clock manages to beat. Whether the text has moved
        // (then it is invalidated and a picture is due).
        private bool Step()
        {
            long now = Milliseconds();

            // While the mouse is on the label the text stands still, and what is left of its rest stays: the end of
            // the rest moves on as much as the time does (one that was over stays over).
            if (_scrollPauseOnHover && _hovered)
            {
                _restUntil += now - _lastFrame;
                _lastFrame = now;

                return false;
            }

            if (_cycle > 0)
                return StepEndless(now);

            long from = Math.Max(_lastFrame, _restUntil);
            _lastFrame = now;

            if (now <= _restUntil)
                return false;

            _offset += _direction * _scrollSpeed.ToPixelsPerSecond() * (now - from) / 1000f;

            if (_direction > 0 && _offset >= _maxOffset)
            {
                _offset = _maxOffset;
                _direction = -1;
                _restUntil = now + _scrollPause.ToMilliseconds();
            }
            else if (_direction < 0 && _offset <= 0f)
            {
                _offset = 0f;
                _direction = 1;
                _restUntil = now + _scrollPause.ToMilliseconds();
            }

            // A picture only when the text has moved at all (not while it rests).
            if (_offset == _paintedOffset)
                return false;

            _paintedOffset = _offset;
            Invalidate();

            return true;
        }

        // The endless text goes on at its speed, round after round, without a rest.
        private bool StepEndless(long now)
        {
            long passed = now - _lastFrame;
            _lastFrame = now;

            _offset = (_offset + _scrollSpeed.ToPixelsPerSecond() * passed / 1000f) % _cycle;

            if (_offset == _paintedOffset)
                return false;

            _paintedOffset = _offset;
            Invalidate();

            return true;
        }

        // ---- what the label is told ----

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            // Another text than the joined one: the label moves that text alone from now on.
            if (_texts != null && Text != _textsJoined)
                _texts = null;

            Restart();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Restart();
        }

        // A size is changed again and again while a window is resized: the text must not jump back to its
        // start each time. The measure is made again, the place in the movement stays (as far as it still exists).
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            float offset = _offset;
            int direction = _direction;
            long restUntil = _restUntil;
            bool wasMoving = _animating;

            Restart();

            if (wasMoving && _cycle > 0)
            {
                _offset = offset % _cycle;
            }
            else if (wasMoving && _maxOffset > 0)
            {
                _offset = Math.Min(offset, _maxOffset);
                _direction = direction;
                _restUntil = restUntil;
            }
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            Restart();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            // (A label that is disabled gets no MouseLeave: the mouse is not "on" it any more.)
            _hovered = false;
            Restart();
        }

        // A label hidden by itself, or a window shown again, comes here; a window that is hidden does not tell
        // its labels (see MakeFrame).
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            // (A label that is hidden gets no MouseLeave either.)
            _hovered = false;
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
            _hovered = false;

            if (_animating)
                StopAnimating();

            if (_watching)
            {
                _watching = false;
                LabelSettingsWatcher.Remove(this);
            }

            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_animating)
                    StopAnimating();

                if (_watching)
                {
                    _watching = false;
                    LabelSettingsWatcher.Remove(this);
                }

                DisposePicture();
            }

            base.Dispose(disposing);
        }

        // ---- painting the moving text ----

        // The picture of the text from the place it has moved to, shown only inside the label (the padding
        // stays free). The place may lie between two pixels; the picture is then mixed between them. It is
        // also where a changed UseMnemonic is noticed, because the label is not told about it.
        private void PaintMovingText(Graphics graphics)
        {
            Rectangle inner = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, ClientSize.Width - Padding.Horizontal),
                Math.Max(0, ClientSize.Height - Padding.Vertical));

            // UseMnemonic cannot tell the label when it changes: it is seen here. With or without the
            // ampersand the text has another width (measure again), only the underline: another picture.
            HotkeyPrefix prefix = GetHotkeyPrefix();

            if ((prefix == HotkeyPrefix.None) != (_measuredPrefix == HotkeyPrefix.None))
                Restart();
            else if (prefix != _pictureHotkeyPrefix)
                DisposePicture();

            if (_maxOffset <= 0 && _cycle <= 0)
            {
                // The new measure says the text fits: nothing to move.
                base.OnPaint(new PaintEventArgs(graphics, ClientRectangle));

                return;
            }

            Bitmap picture = GetPicture();
            int y = GetPictureTop(inner, picture.Height);

            // Back and forth: the picture moves to the left as far as it has gone. Endless: round after
            // round, to the left or to the right, so the picture is put again and again, one round apart,
            // as often as the label needs to be covered.
            float x = inner.Left + _textInset - _offset;

            if (_cycle > 0 && _scrollDirection == LabelScrollDirection.Right)
                x = inner.Left + _textInset + _offset - _cycle;

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

                if (_cycle > 0)
                {
                    for (float next = x + _cycle; next < inner.Right; next += _cycle)
                    {
                        graphics.DrawImage(
                            picture,
                            new RectangleF(next, y, picture.Width, picture.Height),
                            new RectangleF(0, 0, picture.Width, picture.Height),
                            GraphicsUnit.Pixel);
                    }
                }
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

        // The top of the picture: at the top, at the bottom or in the middle of the room, as TextAlign says.
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

            // The endless picture is one round: the text, a gap, the separator and a gap.
            int width = _cycle > 0 ? _cycle : _textWidth + EndRoom;
            Bitmap picture = new Bitmap(Math.Max(1, width), Math.Max(1, (int)Math.Ceiling(Font.GetHeight())) + 2, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(picture))
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = CreateMovingTextFormat())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.Transparent);

                if (_cycle <= 0)
                {
                    graphics.DrawString(Text, Font, brush, new PointF(0f, 1f), format);
                }
                else
                {
                    // Each text is followed by the same room, the separator in the middle of it (a gap on both
                    // sides when the round is as short as it can be, more when the label is wide).
                    string[] texts = _pieceWidths != null ? _texts : new[] { Text };
                    int[] widths = _pieceWidths ?? new[] { _textWidth };
                    float room = (_cycle - _textWidth) / (float)texts.Length;
                    float left = 0f;

                    for (int i = 0; i < texts.Length; i++)
                    {
                        graphics.DrawString(texts[i], Font, brush, new PointF(left, 1f), format);

                        if (_scrollSeparator != LabelScrollSeparator.None)
                            graphics.DrawString(_scrollSeparator.ToText(), Font, brush, new PointF(left + widths[i] + (room - _separatorWidth) / 2f, 1f), format);

                        left += widths[i] + room;
                    }
                }
            }

            _picture = picture;
            _pictureColor = color;
            _pictureHotkeyPrefix = GetHotkeyPrefix();

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
            format.HotkeyPrefix = GetHotkeyPrefix();

            return format;
        }

        // An ampersand is a sign of the text, or - with UseMnemonic - the mark of a key (underlined only
        // while the keyboard cues are shown).
        private HotkeyPrefix GetHotkeyPrefix()
        {
            return !UseMnemonic ? HotkeyPrefix.None : ShowKeyboardCues ? HotkeyPrefix.Show : HotkeyPrefix.Hide;
        }
    }
}
