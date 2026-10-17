using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The moving text of ThemedLabel, in three files: this one (the settings, the state and the measure), ThemedLabel.Animation.cs
    // (the steps on the beat of LabelAnimationClock and what the label is told) and ThemedLabel.Picture.cs (the picture of the
    // text and how it is painted). The colors, the disabled text and the dispatch of the painting are in ThemedLabel.cs.
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

        private static readonly char[] LineBreaks = { '\r', '\n' };

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

        // The tooltip with the whole text, while the text moves because it does not fit (see ShowFullTextToolTip).
        private readonly FullTextToolTip _fullTextToolTip;

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

        // Whether the label waits for AnimationSettings (it moves, or could), and how much room the screen
        // font puts before the text (measured with the text, see Measure).
        private bool _watching;
        private int _textInset;

        // ---- the seams of the tests ----

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

        /// <summary>
        /// Whether a tooltip shows the whole text while the mouse is on the label and the text moves because it is too long for
        /// the label (on by default). A text that fits has none, and neither has one that does not move (a standard label with
        /// <see cref="Label.AutoEllipsis"/> has a tooltip of its own).
        /// </summary>
        public bool ShowFullTextToolTip
        {
            get { return _fullTextToolTip.Enabled; }
            set
            {
                _fullTextToolTip.Enabled = value;
                UpdateFullTextToolTip();
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
                AnimationSettings.Add(this);
            }
            else if (!wantsSettings)
            {
                StopWatching();
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

            UpdateFullTextToolTip();
            Invalidate();
        }

        // The tooltip has the whole text while the text moves because it does not fit (an endless text that fits moves too, but
        // is shown whole at some time in each round: it has none).
        private void UpdateFullTextToolTip()
        {
            bool moves = (_maxOffset > 0 || _cycle > 0) && Visible && IsHandleCreated;
            bool exceeds = _maxOffset > 0 || (_cycle > 0 && _textWidth + 2 * _textInset > ClientSize.Width - Padding.Horizontal);

            _fullTextToolTip.Update(Text, moves && exceeds);
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

            if (!CanMove())
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

        // Whether the text can move at all: it is asked for, the label is enabled, Windows lets windows animate, the label
        // is not as big as its text (see MovesWhenAutoSized), and the text is one line that is not empty.
        private bool CanMove()
        {
            return _scrollMode != LabelScrollMode.None
                && Enabled
                && AnimationSettings.Enabled()
                && (!AutoSize || MovesWhenAutoSized())
                && !string.IsNullOrWhiteSpace(Text)
                && Text.IndexOfAny(LineBreaks) < 0;
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
    }
}
