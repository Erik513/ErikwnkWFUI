using System;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Watches the whole application for a mouse button going down and calls
    /// back when <c>isOutside</c> says it landed somewhere that counts as
    /// "elsewhere" for the owner. Never swallows the click. Start it when
    /// the owner needs it, stop it (or dispose it) when done.
    /// </summary>
    public sealed class OutsideClickFilter : IMessageFilter, IDisposable
    {
        private readonly Func<IntPtr, bool> _isOutside;
        private readonly Action _onOutsideClick;
        private readonly bool _leftButtonOnly;
        private bool _started;

        /// <summary>Makes a filter; <see cref="Start"/> begins the watching.</summary>
        /// <param name="isOutside">Given the window the click went to: is that outside?</param>
        /// <param name="onOutsideClick">What to do about it.</param>
        /// <param name="leftButtonOnly">Watch only the left button (otherwise every button).</param>
        /// <exception cref="ArgumentNullException"><paramref name="isOutside"/> or <paramref name="onOutsideClick"/> is null.</exception>
        public OutsideClickFilter(Func<IntPtr, bool> isOutside, Action onOutsideClick, bool leftButtonOnly = false)
        {
            _isOutside = isOutside ?? throw new ArgumentNullException(nameof(isOutside));
            _onOutsideClick = onOutsideClick ?? throw new ArgumentNullException(nameof(onOutsideClick));
            _leftButtonOnly = leftButtonOnly;
        }

        /// <summary>Begins to watch the clicks of the application (on the thread that calls it, which needs a message loop). Does nothing when it already does.</summary>
        public void Start()
        {
            if (_started)
                return;

            _started = true;
            Application.AddMessageFilter(this);
        }

        /// <summary>Stops watching. Does nothing when it does not.</summary>
        public void Stop()
        {
            if (!_started)
                return;

            _started = false;
            Application.RemoveMessageFilter(this);
        }

        /// <summary>Stops watching (the same as <see cref="Stop"/>).</summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>Called by the message loop for every message: calls back for a button going down outside, and never swallows the message.</summary>
        public bool PreFilterMessage(ref Message m)
        {
            if (IsWatchedButtonDown(m.Msg) && _isOutside(m.HWnd))
            {
                _onOutsideClick();
            }

            return false;
        }

        private bool IsWatchedButtonDown(int message)
        {
            if (_leftButtonOnly)
                return message == WM_LBUTTONDOWN || message == WM_NCLBUTTONDOWN;

            return message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN || message == WM_MBUTTONDOWN ||
                   message == WM_XBUTTONDOWN || message == WM_NCLBUTTONDOWN || message == WM_NCRBUTTONDOWN;
        }
    }
}
