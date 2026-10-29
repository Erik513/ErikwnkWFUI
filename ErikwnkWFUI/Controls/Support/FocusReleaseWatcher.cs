using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // A click with the left button anywhere else takes the focus out of a box that has it (the standard box keeps it when the click lands on
    // something that cannot take the focus itself - a label, a panel - so the frame stayed lit and the caret kept blinking). One watcher for
    // every box that has it: TextField, ComboBox, NumericUpDown. It watches the whole application, so it runs only while the box has a window.
    internal sealed class FocusReleaseWatcher : IDisposable
    {
        private readonly Control _owner;
        private readonly Func<bool> _isEnabled;
        private readonly OutsideClickFilter _filter;

        public FocusReleaseWatcher(Control owner, Func<bool> isEnabled)
        {
            _owner = owner;
            _isEnabled = isEnabled;
            _filter = new OutsideClickFilter(IsOutsideClick, ReleaseFocus, leftButtonOnly: true);
        }

        public void Start()
        {
            _filter.Start();
        }

        public void Stop()
        {
            _filter.Stop();
        }

        public void Dispose()
        {
            _filter.Dispose();
        }

        // A control of the application other than the box. Not a menu or a tooltip (windows that are no controls: the context menu of the
        // box belongs to it - choosing "Paste" must still find the focus where it was; nor the drop-down list of a combo box, a window of
        // the box itself), and not a drop-down menu of the library.
        internal bool IsOutsideClick(IntPtr window)
        {
            // (Not _owner.Handle: that would make a window for a box that has none, and throws for a box that is gone.)
            if (_owner.IsDisposed || !_owner.IsHandleCreated || !_isEnabled() || window == _owner.Handle)
                return false;

            Control control = Control.FromHandle(window);

            // (A box that is made of controls - the number box has a text box and the arrows - or a class of its own that derives from it
            // might have windows inside it.)
            return control != null && !(control is ToolStripDropDown) && !_owner.Contains(control);
        }

        // The focus goes to the window that holds the box (and the box has none then), as it does when one clicks on the empty background
        // of a window in other programs.
        internal void ReleaseFocus()
        {
            if (_owner.IsDisposed || !_owner.ContainsFocus)
                return;

            Form form = _owner.FindForm();

            if (form == null)
                return;

            form.ActiveControl = null;

            if (_owner.ContainsFocus)
                form.Focus();
        }
    }
}