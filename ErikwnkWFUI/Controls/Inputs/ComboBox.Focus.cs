using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // A click with the left button anywhere else takes the focus out of the box (see FocusReleaseWatcher). The open list is a window of the
    // box itself: choosing an item in it is not a click somewhere else.
    public partial class ComboBox
    {
        private FocusReleaseWatcher _clickWatcher;

        /// <summary>Whether a click with the left mouse button on something else in the window takes the focus out of the box. On by default.</summary>
        [DefaultValue(true)]
        public bool ReleaseFocusOnOutsideClick { get; set; } = true;

        private void StartWatchingClicks()
        {
            if (_clickWatcher == null)
                _clickWatcher = new FocusReleaseWatcher(this, () => ReleaseFocusOnOutsideClick);

            _clickWatcher.Start();
        }

        private void StopWatchingClicks()
        {
            if (_clickWatcher != null)
                _clickWatcher.Stop();
        }

        private void DisposeClickWatcher()
        {
            if (_clickWatcher == null)
                return;

            _clickWatcher.Dispose();
            _clickWatcher = null;
        }
    }
}