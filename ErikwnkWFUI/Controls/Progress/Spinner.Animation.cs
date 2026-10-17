using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The turning of the arc: the clock, and when it runs.
    public partial class Spinner
    {
        private const int TickMs = 16;              // ~60 fps
        private const int HiddenPollMs = 250;       // how often a spinner on a hidden window looks whether it is shown again
        private const int AskEveryTicks = 32;       // Windows is asked again for its animation setting after this many ticks (~0.5 s)
        private const int LongestStepMs = 100;      // a step after a longer stall (a busy window) does not make the arc jump

        private readonly Timer _timer = new Timer { Interval = TickMs };
        private readonly Stopwatch _clock = new Stopwatch();
        private float _angle;
        private long _lastTickMs;
        private int _ticksSinceAsked;
        private bool _isSpinning = true;
        private SpinnerSpeed _speed = SpinnerSpeed.Medium;

        private void InitializeClock()
        {
            _timer.Tick += OnTick;
        }

        private void DisposeClock()
        {
            _timer.Stop();
            _clock.Stop();
            _timer.Tick -= OnTick;
            _timer.Dispose();
        }

        /// <summary>
        /// Whether the arc turns (true by default). False stands it still where it is, e.g. while nothing is loading; with
        /// <see cref="HideWhenStopped"/> the ring is gone then. It also stands still while the control is disabled or while Windows has the
        /// animations switched off (the ring stays visible then). A known load (<see cref="IsIndeterminate"/> off) never turns.
        /// </summary>
        [DefaultValue(true)]
        public bool IsSpinning
        {
            get { return _isSpinning; }
            set
            {
                if (value == _isSpinning)
                    return;

                _isSpinning = value;
                UpdateRunning();
                Invalidate();
            }
        }

        /// <summary>How fast the arc turns (a turn in about 1.2 seconds by default, the same on every computer). It has no effect on a known load.</summary>
        [DefaultValue(SpinnerSpeed.Medium)]
        public SpinnerSpeed Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        // The arc turns by the time that has passed, not by a fixed step per beat: the beat of a Windows timer is late and uneven.
        private void OnTick(object sender, EventArgs e)
        {
            long now = _clock.ElapsedMilliseconds;
            long passed = Math.Min(now - _lastTickMs, LongestStepMs);
            _lastTickMs = now;

            // A window that is hidden (or a spinner taken off its window) hides the spinner without telling it (no VisibleChanged,
            // and none when the window is shown again): the clock keeps watching at a slow pace and takes up its speed when it is
            // shown again.
            if (!Visible)
            {
                if (_timer.Interval != HiddenPollMs)
                    _timer.Interval = HiddenPollMs;

                return;
            }

            if (_timer.Interval != TickMs)
                _timer.Interval = TickMs;

            if (++_ticksSinceAsked >= AskEveryTicks)
            {
                _ticksSinceAsked = 0;

                if (!ThemedLabel.AnimationsEnabled())
                {
                    StopClock();

                    return;
                }
            }

            Advance(passed);
            Invalidate();
        }

        /// <summary>Turns the arc on by this many milliseconds (the clock does it on every beat).</summary>
        internal void Advance(double milliseconds)
        {
            float degreesPerSecond = _speed == SpinnerSpeed.Slow ? 180f : _speed == SpinnerSpeed.Fast ? 540f : 300f;

            _angle = (float)((_angle + degreesPerSecond * milliseconds / 1000.0) % 360.0);
        }

        private void StartClock()
        {
            _clock.Restart();
            _lastTickMs = 0;
            _timer.Interval = TickMs;
            _timer.Start();
        }

        private void StopClock()
        {
            _timer.Stop();
            _clock.Stop();
        }

        // Starts or stops the clock: it runs only while there is something to turn and somebody to see it.
        private void UpdateRunning()
        {
            bool run = Visible && IsHandleCreated && !DesignMode
                && _isSpinning && _isIndeterminate && Enabled && ThemedLabel.AnimationsEnabled();
            _ticksSinceAsked = 0;

            if (run && !_timer.Enabled)
                StartClock();
            else if (!run && _timer.Enabled)
                StopClock();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRunning();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateRunning();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRunning();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            StopClock();
            base.OnHandleDestroyed(e);
        }
    }
}
