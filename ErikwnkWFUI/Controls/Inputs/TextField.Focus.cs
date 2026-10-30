using System.ComponentModel;

namespace ErikwnkWFUI.Controls
{
    // A click with the left button anywhere else takes the focus out of the box (see FocusReleaseWatcher).
    public partial class TextField
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
    }
}