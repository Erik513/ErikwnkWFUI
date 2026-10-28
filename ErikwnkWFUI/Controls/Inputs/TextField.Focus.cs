using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // A click with the left button anywhere else takes the focus out of the box (the standard box keeps it when the click lands on something
    // that cannot take the focus itself - a label, a panel - so the frame stayed lit and the caret kept blinking).
    public partial class TextField
    {
        private OutsideClickFilter _clickWatcher;

        private void StartWatchingClicks()
        {
            if (_clickWatcher == null)
                _clickWatcher = new OutsideClickFilter(IsOutsideClick, ReleaseFocus, leftButtonOnly: true);

            _clickWatcher.Start();
        }

        private void StopWatchingClicks()
        {
            if (_clickWatcher != null)
                _clickWatcher.Stop();
        }

        // A control of the application other than this box. Not a menu or a tooltip (windows that are no controls: the context menu of the
        // box belongs to it - choosing "Paste" must still find the focus where it was), and not a drop-down menu of the library.
        private bool IsOutsideClick(IntPtr window)
        {
            if (window == Handle)
                return false;

            Control control = Control.FromHandle(window);

            return control != null && !(control is ToolStripDropDown) && !IsAncestorOf(control);
        }

        // (A box has no windows inside it, but a class of its own that derives from it might.)
        private bool IsAncestorOf(Control control)
        {
            return Contains(control);
        }

        // The focus goes to the window that holds the box (and the box has none then), as it does when one clicks on the empty background
        // of a window in other programs.
        private void ReleaseFocus()
        {
            if (!Focused || IsDisposed)
                return;

            Form form = FindForm();

            if (form == null)
                return;

            form.ActiveControl = null;

            if (Focused)
                form.Focus();
        }
    }
}