using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// What <see cref="InfoPopupForm"/> and <see cref="ToastForm"/> have in common: a borderless
    /// window that is never in the taskbar, never takes the activation, has rounded corners
    /// (Windows 11), is see-through by a <see cref="PopupTransparency"/> and moves with the
    /// window it belongs to.
    /// </summary>
    public abstract class PopupFormBase : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private PopupTransparency _transparency = PopupTransparency.Medium;

        internal PopupFormBase()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Opacity = _transparency.ToOpacity();
        }

        /// <summary>Moves the popup with the window it is shown for.</summary>
        internal OwnerFollower Follower { get; } = new OwnerFollower();

        /// <summary>How see-through the popup is: not at all, slightly, clearly (the default) or strongly.</summary>
        public PopupTransparency Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value;

                if (IsDisposed)
                    return;

                Opacity = value.ToOpacity();
                OnTransparencyChanged();
            }
        }

        /// <summary>Called when the opacity has changed: the colors that depend on it are to be set again.</summary>
        protected abstract void OnTransparencyChanged();

        /// <summary>The corner of the window as DWM knows it: <c>DWMWCP_ROUND</c> or <c>DWMWCP_ROUNDSMALL</c>.</summary>
        protected virtual int CornerPreference
        {
            get { return DWMWCP_ROUND; }
        }

        // A popup must never take the activation away from the window it is shown for (the window
        // of a slider that is being dragged, say).
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // The same for a click on the popup: it does not activate either.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_NOACTIVATE;

                return parameters;
            }
        }

        // Win11 rounds window corners itself via DWM (anti-aliased, all four identical): just
        // ask for the radius. A window Region fights this and leaves one corner square; before
        // Windows 11 the call does nothing and the popup is a plain rectangle.
        protected void ApplyRoundedCorners()
        {
            if (!IsHandleCreated)
                return;

            int preference = CornerPreference;

            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
                // before Windows 11: no rounded corners
            }
            catch (DllNotFoundException)
            {
                // no desktop window manager: no rounded corners
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyRoundedCorners();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Follower.Detach();

            base.Dispose(disposing);
        }
    }
}
