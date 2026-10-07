using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// What the popups that hang on a window (the info popup, the toast) share: they are shown
    /// only while that window is in front, and they move with it.
    /// </summary>
    internal static class PopupSupport
    {
        // Whether the window the popup belongs to is the one in front. Replaceable so a test
        // does not depend on what is in front on the machine it runs on.
        internal static Func<Form, bool> OwnerIsActive = owner => IsActiveFor(Form.ActiveForm, owner);

        // The window in front is the owner itself, or a window owned by it (a dialog opened
        // from it). With another window of the application in front, or another program, it is
        // not. A control without a form has no window to compare: any active window will do.
        internal static bool IsActiveFor(Form active, Form owner)
        {
            if (active == null)
                return false;

            if (owner == null)
                return true;

            for (Form form = active; form != null; form = form.Owner)
            {
                if (form == owner)
                    return true;
            }

            return false;
        }

        // A popup is owned by its window, and showing an owned window raises that window: with
        // another window or program in front the popup would pull its window up over it. So
        // nothing is shown unless that window is in front, and not over a minimized one.
        internal static bool CanShowOver(Form ownerForm)
        {
            if (ownerForm != null && (ownerForm.IsDisposed || ownerForm.WindowState == FormWindowState.Minimized))
                return false;

            return OwnerIsActive(ownerForm);
        }
    }

    /// <summary>
    /// Keeps a popup at the same place relative to its owner window: when the window is moved
    /// the popup goes with it.
    /// </summary>
    internal sealed class OwnerFollower
    {
        private Form _popup;
        private Form _owner;
        private Point _offset;

        // Starts following <paramref name="owner"/> from where the popup is now; called again
        // after the popup was put somewhere else.
        internal void Attach(Form popup, Form owner)
        {
            if (owner == null)
            {
                Detach();

                return;
            }

            if (_owner != owner)
            {
                Detach();
                _owner = owner;
                _owner.LocationChanged += OnOwnerLocationChanged;
            }

            _popup = popup;
            _offset = new Point(popup.Left - owner.Left, popup.Top - owner.Top);
        }

        internal void Detach()
        {
            if (_owner != null)
                _owner.LocationChanged -= OnOwnerLocationChanged;

            _owner = null;
            _popup = null;
        }

        private void OnOwnerLocationChanged(object sender, EventArgs e)
        {
            if (_popup == null || _popup.IsDisposed || _owner == null || _owner.IsDisposed)
                return;

            _popup.Location = new Point(_owner.Left + _offset.X, _owner.Top + _offset.Y);
        }
    }
}
