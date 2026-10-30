using System;
using System.ComponentModel;

namespace ErikwnkWFUI.Controls
{
    // A click with the left button anywhere else takes the focus out of the box (see FocusReleaseWatcher). The text field and the arrows
    // are controls inside the box: they are not somewhere else.
    public partial class NumericUpDown
    {
        private FocusReleaseWatcher _clickWatcher;

        /// <summary>Whether a click with the left mouse button on something else in the window takes the focus out of the box. On by default.</summary>
        [DefaultValue(true)]
        public bool ReleaseFocusOnOutsideClick { get; set; } = true;

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_clickWatcher == null)
                _clickWatcher = new FocusReleaseWatcher(this, () => ReleaseFocusOnOutsideClick);

            _clickWatcher.Start();
        }

        /// <inheritdoc/>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_clickWatcher != null)
                _clickWatcher.Stop();

            base.OnHandleDestroyed(e);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _clickWatcher != null)
            {
                _clickWatcher.Dispose();
                _clickWatcher = null;
            }

            base.Dispose(disposing);
        }
    }
}