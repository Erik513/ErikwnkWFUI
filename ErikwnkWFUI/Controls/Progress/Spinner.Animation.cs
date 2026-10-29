using System;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // The turning of the arc: the clock, and when it runs.
    public partial class Spinner
    {
        private FrameTicker _ticker;
        private float _angle;
        private ControlAnimationListener _settings;   // the animations of Windows were switched: the clock starts or stops
        private bool _isSpinning = true;
        private SpinnerSpeed _speed = SpinnerSpeed.Medium;

        private void InitializeClock()
        {
            _ticker = new FrameTicker(this, Step);
            _settings = new ControlAnimationListener(this, UpdateRunning);
        }

        private void DisposeClock()
        {
            AnimationSettings.Remove(_settings);
            _ticker.Dispose();
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

        // On each beat of the clock: the arc turns by the time that has passed, not by a fixed step per beat.
        private void Step(double milliseconds)
        {
            Advance(milliseconds);
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
            _ticker.Start();
        }

        private void StopClock()
        {
            _ticker.Stop();
        }

        // Starts or stops the clock: it runs only while there is something to turn and somebody to see it.
        private void UpdateRunning()
        {
            bool run = Visible && IsHandleCreated && !DesignMode
                && _isSpinning && _isIndeterminate && Enabled && AnimationSettings.Enabled();

            if (run)
                StartClock();
            else
                StopClock();
        }

        /// <inheritdoc/>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRunning();
        }

        /// <inheritdoc/>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateRunning();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AnimationSettings.Add(_settings);
            UpdateRunning();
        }

        /// <inheritdoc/>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            AnimationSettings.Remove(_settings);
            StopClock();
            base.OnHandleDestroyed(e);
        }
    }
}

