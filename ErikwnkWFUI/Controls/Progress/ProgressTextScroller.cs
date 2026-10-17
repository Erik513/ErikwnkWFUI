using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The movement of a text that is too long for its place on a progress bar (<c>TextScroll</c>): the settings (a
    /// <see cref="LabelScroll"/>, as for a label), how far the text has moved, and the clock that moves it. The bar tells it, each
    /// time it paints, how wide the text and its room are (<see cref="Configure"/>); the clock runs only while the text does not fit,
    /// the bar is shown and enabled, and Windows lets windows animate.
    /// </summary>
    internal sealed class ProgressTextScroller : IDisposable
    {
        // The moving text goes a little further than its end, so that the last sign is clear of the edge.
        private const int EndRoom = 2;

        // Whether Windows lets windows animate is asked again after this long (not on every picture of the moving text).
        private const int AnimationSettingMilliseconds = 500;

        private readonly Control _owner;
        private readonly Timer _timer = new Timer { Interval = 15 };
        private readonly Stopwatch _clock = new Stopwatch();

        private LabelScroll _scroll = LabelScroll.None;
        private bool _active;
        private bool _hovered;

        // What the movement was configured for: how far back and forth goes, how long one round of the endless text is, and how wide
        // the pieces of that round are.
        private int _distance;
        private int _cycle;
        private int _textWidth;
        private int _separatorWidth;
        private int _gap;

        // How long the text has been moving (not counting the time it was paused by the mouse), and when it was counted last.
        private double _elapsed;
        private long _lastTick;
        private int _offset;

        // The last answer about the animations of Windows, and when it was asked.
        private bool _animationsAllowed = true;
        private long _animationsAsked = long.MinValue;
        private readonly ControlAnimationListener _settings;

        public ProgressTextScroller(Control owner)
        {
            _owner = owner;
            _timer.Tick += (sender, e) => Tick();
            _settings = new ControlAnimationListener(owner, OnSettingChanged);
        }

        // Windows switched its animations on or off: the answer is asked again at the next picture, and that picture is made now (a
        // text that stands still because they were off makes none by itself).
        private void OnSettingChanged()
        {
            _animationsAsked = long.MinValue;
            _owner.Invalidate();
        }

        /// <summary>How the text moves; <see cref="LabelScroll.None"/> (it does not) until it is set.</summary>
        public LabelScroll Scroll
        {
            get { return _scroll; }
            set
            {
                _scroll = value ?? LabelScroll.None;

                // It hears of the animations of Windows only while it has a movement to make.
                if (_scroll.Mode != LabelScrollMode.None)
                    AnimationSettings.Add(_settings);
                else
                    AnimationSettings.Remove(_settings);

                Restart();
                _owner.Invalidate();
            }
        }

        /// <summary>Whether the text moves now.</summary>
        public bool IsMoving
        {
            get { return _active; }
        }

        /// <summary>The width of the text the movement was configured for.</summary>
        public int TextWidth
        {
            get { return _textWidth; }
        }

        /// <summary>The gap before and behind the separator of the endless text.</summary>
        public int Gap
        {
            get { return _gap; }
        }

        /// <summary>The width of the separator of the endless text.</summary>
        public int SeparatorWidth
        {
            get { return _separatorWidth; }
        }

        /// <summary>How long one round of the endless text is, in pixels.</summary>
        public int Cycle
        {
            get { return _cycle; }
        }

        /// <summary>
        /// How far the text is to the left of the start of its room now (to the right of it, when it is endless and goes that
        /// way, as a number of that round).
        /// </summary>
        public int Offset
        {
            get { return _offset; }
        }

        /// <summary>The mouse is on the bar (the text stands still then, when the scroll asks for it).</summary>
        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
        }

        /// <summary>
        /// Tells it how wide the text and its room are now, and whether the text may move. Starts the movement over where
        /// the measures changed, and starts or stops the clock.
        /// </summary>
        public void Configure(int textWidth, int roomWidth, Font font)
        {
            bool active = _scroll.Mode != LabelScrollMode.None
                && textWidth > roomWidth
                && roomWidth > 0
                && AnimationsAllowed()
                && _owner.Enabled
                && _owner.Visible
                && _owner.IsHandleCreated;

            int distance = 0;
            int cycle = 0;
            int separatorWidth = 0;
            int gap = 0;

            if (active && _scroll.Mode == LabelScrollMode.BackAndForth)
            {
                distance = textWidth - roomWidth + EndRoom;
            }
            else if (active)
            {
                gap = Math.Max(4, (int)Math.Round(font.GetHeight()));
                string separator = _scroll.Separator.ToText();
                separatorWidth = separator.Length > 0 ? ProgressBarPainting.MeasureText(separator, font).Width : 0;
                cycle = Math.Max(textWidth + gap + separatorWidth + gap, roomWidth);
            }

            // Another text, or the movement starting or stopping, begins it again; a room that grows or shrinks (a window that is
            // resized) only changes how far it has to go: the text stays where it is in the movement.
            bool begins = active != _active || textWidth != _textWidth;
            bool measuresChanged = distance != _distance || cycle != _cycle;

            _distance = distance;
            _cycle = cycle;
            _separatorWidth = separatorWidth;
            _gap = gap;
            _textWidth = textWidth;

            if (begins)
            {
                Restart(active);
            }
            else if (active)
            {
                if (measuresChanged)
                    _offset = ComputeOffset(_elapsed);

                if (!_timer.Enabled)
                    StartClock();
            }
        }

        private bool AnimationsAllowed()
        {
            long now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

            if (_animationsAsked == long.MinValue || now - _animationsAsked >= AnimationSettingMilliseconds)
            {
                _animationsAllowed = AnimationSettings.Enabled();
                _animationsAsked = now;
            }

            return _animationsAllowed;
        }

        // Back to the start, with the answer of the last Configure kept or reset.
        private void Restart(bool? active = null)
        {
            _active = active ?? false;
            _elapsed = 0;
            _offset = 0;

            if (_active)
                StartClock();
            else
                StopClock();
        }

        private void StartClock()
        {
            _clock.Restart();
            _lastTick = 0;
            _timer.Start();
        }

        private void StopClock()
        {
            _timer.Stop();
            _clock.Stop();
        }

        private void Tick()
        {
            if (!_active || _owner.IsDisposed || !_owner.Visible || !_owner.Enabled)
            {
                Restart();
                _owner.Invalidate();

                return;
            }

            long now = _clock.ElapsedMilliseconds;
            long passed = now - _lastTick;
            _lastTick = now;

            Advance(passed);
        }

        /// <summary>Lets the text move on by this many milliseconds (not while the mouse holds it, if the scroll asks for that); the clock does it on every beat.</summary>
        internal void Advance(double milliseconds)
        {
            if (!(_hovered && _scroll.PauseOnHover))
                _elapsed += milliseconds;

            int offset = ComputeOffset(_elapsed);

            if (offset == _offset)
                return;

            _offset = offset;
            _owner.Invalidate();
        }

        // How far the text is to the left of its start after it has moved for this long.
        private int ComputeOffset(double elapsed)
        {
            double speed = _scroll.Speed.ToPixelsPerSecond();

            if (_scroll.Mode == LabelScrollMode.Endless)
            {
                if (_cycle <= 0)
                    return 0;

                double moved = speed * elapsed / 1000.0 % _cycle;

                return (int)Math.Round(_scroll.Direction == LabelScrollDirection.Left ? moved : _cycle - moved) % _cycle;
            }

            // Back and forth: rest at the start, move to the end, rest, move back.
            double rest = _scroll.Pause.ToMilliseconds();
            double travel = _distance / speed * 1000.0;
            double time = elapsed % (2 * (rest + travel));

            if (time < rest)
                return 0;

            if (time < rest + travel)
                return (int)Math.Round(speed * (time - rest) / 1000.0);

            if (time < 2 * rest + travel)
                return _distance;

            return (int)Math.Round(_distance - speed * (time - 2 * rest - travel) / 1000.0);
        }

        public void Dispose()
        {
            AnimationSettings.Remove(_settings);
            _timer.Dispose();
            _clock.Stop();
        }
    }
}
