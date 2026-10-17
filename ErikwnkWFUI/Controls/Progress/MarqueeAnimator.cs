using System;
using System.Diagnostics;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The clock of the marquee style of a progress bar: while the bar wants it, the offset of the moving block grows by one step every
    /// <c>MarqueeAnimationSpeed</c> milliseconds and the bar is repainted. Nothing runs while the bar is not a marquee, hidden
    /// or its speed is 0.
    /// </summary>
    internal sealed class MarqueeAnimator : IDisposable
    {
        private readonly Control _owner;
        private readonly Func<bool> _wanted;
        private readonly Func<int> _speed;
        private readonly FrameTicker _ticker;
        private readonly Stopwatch _clock = new Stopwatch();

        public MarqueeAnimator(Control owner, Func<bool> wanted, Func<int> speed)
        {
            _owner = owner;
            _wanted = wanted;
            _speed = speed;

            _ticker = new FrameTicker(owner, milliseconds => Tick());
        }

        /// <summary>How far the block has moved, in pixels (the bar wraps it).</summary>
        public int Offset { get; private set; }

        /// <summary>Starts or stops the clock, according to what the bar wants now. Call it when that may have changed.</summary>
        public void Update()
        {
            bool run = _wanted() && _owner.IsHandleCreated && _owner.Visible;

            if (run && !_ticker.IsRunning)
            {
                _clock.Restart();
                _ticker.Start();
            }
            else if (!run && _ticker.IsRunning)
            {
                _ticker.Stop();
                _clock.Stop();
            }
        }

        private void Tick()
        {
            if (!_wanted())
            {
                Update();

                return;
            }

            int speed = _speed();

            if (speed <= 0)
                return;

            // (Wrapped long before an int could overflow.)
            int offset = (int)(_clock.ElapsedMilliseconds / speed % 1000000) * ProgressBarPainting.MarqueeStepPixels;

            if (offset == Offset)
                return;

            Offset = offset;
            _owner.Invalidate();
        }

        public void Dispose()
        {
            _ticker.Dispose();
        }
    }
}
