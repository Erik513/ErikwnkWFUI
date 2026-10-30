using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// What a control that moves (a spinner, the marquee of a progress bar, a moving text on a bar) uses instead of its own timer: it takes
    /// part in the <see cref="AnimationClock"/> and, on each beat, calls <c>step</c> on the thread of the control with the milliseconds since
    /// the last step (not more than <see cref="LongestStepMilliseconds"/>, so that a stall of the window does not make what moves jump).
    /// A control on a window that is hidden is not told so (no VisibleChanged, and none when the window is shown again): the ticker leaves the
    /// clock then and only looks four times a second whether the control is shown again.
    /// </summary>
    internal sealed class FrameTicker : IAnimationClient, IDisposable
    {
        /// <summary>A step after a longer stall is this long.</summary>
        internal const int LongestStepMilliseconds = 100;

        private const int HiddenPollMilliseconds = 250;

        // A step that was asked for and has not come after this long is taken as lost (its window was destroyed and made again on the way,
        // which throws away the posted message): the next beat asks again, otherwise the ticker would wait for it for ever.
        private const int LostStepMilliseconds = 500;

        private readonly Control _owner;
        private readonly Action<double> _step;
        private readonly Timer _poll = new Timer { Interval = HiddenPollMilliseconds };

        private volatile bool _running;
        private volatile bool _disposed;
        private bool _onClock;
        private int _framePending;
        private long _framePendingSince;
        private long _lastStep;

        public FrameTicker(Control owner, Action<double> step)
        {
            _owner = owner;
            _step = step;
            _poll.Tick += (sender, e) => Poll();
        }

        /// <summary>Whether it is meant to run (also while it only watches because the control is hidden).</summary>
        public bool IsRunning
        {
            get { return _running; }
        }

        /// <summary>Whether it is on the beat of the clock now (not while the control is hidden).</summary>
        public bool IsOnClock
        {
            get { return _onClock; }
        }

        /// <summary>Starts it; the first step is the one after the first beat.</summary>
        public void Start()
        {
            // (A ticker that was disposed does not start again: the control that owns it can still be asked about its visibility while it is
            // being disposed, and would put the ticker back on the clock, where nothing would ever take it off.)
            if (_running || _disposed)
                return;

            _running = true;
            _lastStep = Now();

            if (_owner.Visible)
                JoinClock();
            else
                _poll.Start();
        }

        public void Stop()
        {
            _running = false;
            LeaveClock();
            _poll.Stop();
        }

        // Called from the thread of the clock: ask the window for the next step, once.
        void IAnimationClient.RequestFrame()
        {
            if (!_running || !_owner.IsHandleCreated)
                return;

            long now = Now();

            if (Interlocked.Exchange(ref _framePending, 1) == 1 && now - Interlocked.Read(ref _framePendingSince) < LostStepMilliseconds)
                return;

            Interlocked.Exchange(ref _framePendingSince, now);

            try
            {
                _owner.BeginInvoke(new Action(MakeStep));
            }
            catch (Exception)
            {
                // the window has gone since: nothing is waiting for the step
                Interlocked.Exchange(ref _framePending, 0);
            }
        }

        private void MakeStep()
        {
            Interlocked.Exchange(ref _framePending, 0);

            if (!_running || _owner.IsDisposed)
                return;

            if (!_owner.Visible)
            {
                LeaveClock();
                _poll.Start();

                return;
            }

            long now = Now();
            double passed = Math.Min(now - _lastStep, LongestStepMilliseconds);
            _lastStep = now;

            _step(passed);
        }

        private void Poll()
        {
            if (!_running)
            {
                _poll.Stop();

                return;
            }

            if (!_owner.Visible)
                return;

            _poll.Stop();
            _lastStep = Now();
            JoinClock();
        }

        private void JoinClock()
        {
            if (_onClock)
                return;

            _onClock = true;
            AnimationClock.Add(this);
        }

        private void LeaveClock()
        {
            if (!_onClock)
                return;

            _onClock = false;
            AnimationClock.Remove(this);
        }

        private static long Now()
        {
            return Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();
            _poll.Dispose();
        }
    }
}
