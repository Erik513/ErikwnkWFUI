using System;
using System.Windows.Forms;

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
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;

        private readonly Func<IntPtr, bool> _isOutside;
        private readonly Action _onOutsideClick;
        private readonly bool _leftButtonOnly;
        private bool _started;

        /// <param name="isOutside">Given the window the click went to: is that outside?</param>
        /// <param name="onOutsideClick">What to do about it.</param>
        /// <param name="leftButtonOnly">Watch only the left button (otherwise every button).</param>
        public OutsideClickFilter(Func<IntPtr, bool> isOutside, Action onOutsideClick, bool leftButtonOnly = false)
        {
            _isOutside = isOutside;
            _onOutsideClick = onOutsideClick;
            _leftButtonOnly = leftButtonOnly;
        }

        public void Start()
        {
            if (_started)
                return;

            _started = true;
            Application.AddMessageFilter(this);
        }

        public void Stop()
        {
            if (!_started)
                return;

            _started = false;
            Application.RemoveMessageFilter(this);
        }

        public void Dispose()
        {
            Stop();
        }

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
