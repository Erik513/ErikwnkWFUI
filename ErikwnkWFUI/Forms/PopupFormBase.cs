using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// What <see cref="InfoPopupForm"/> and <see cref="ToastForm"/> have in common: a borderless
    /// window that is never in the taskbar, never takes the activation, has rounded corners
    /// (cut with a region, so without the shadow Windows 11 gives a rounded window), is see-through by a <see cref="PopupTransparency"/> and moves with the
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

        /// <summary>The radius of the rounded corners at 96 dpi (scaled with the screen).</summary>
        protected virtual int CornerRadius96
        {
            get { return 12; }
        }

        /// <summary>The thin line around the popup: the text color of its background, faded most of the way into it.</summary>
        protected virtual Color BorderColor
        {
            get { return UIColors.BlendToward(UIColors.GetBestTextColor(BackColor), BackColor, BorderFade); }
        }

        // How far the line around the popup is faded into the background (it is a quiet outline, not a frame).
        private const double BorderFade = 0.7;

        private int GetCornerRadius()
        {
            return Math.Min(Dpi.Scale(this, CornerRadius96), Math.Min(Width, Height) / 2);
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

        // The corners are cut with a window region, the same on every Windows. Windows 11 would round them itself
        // (anti-aliased), but gives a rounded window a shadow, which follows a popup that moves with the mouse late and at
        // the wrong place - so its own rounding is switched off.
        protected void ApplyRoundedCorners()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0)
                return;

            SwitchOffSystemRounding();

            using (GraphicsPath path = RoundedRectangle.CreatePath(new Rectangle(0, 0, Width, Height), GetCornerRadius()))
            {
                Region previous = Region;
                Region = new Region(path);

                if (previous != null)
                    previous.Dispose();
            }
        }

        private void SwitchOffSystemRounding()
        {
            int preference = DWMWCP_DONOTROUND;

            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
                // before Windows 11 there is nothing to switch off
            }
            catch (DllNotFoundException)
            {
                // no desktop window manager
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
            Invalidate();
        }

        // The line around the popup - drawn smoothed along the corners, which also softens the edge of the region there
        // (the edge of a window region has no smoothing of its own).
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int thickness = Math.Max(1, Dpi.Scale(this, 1));
            SmoothingMode previous = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = RoundedRectangle.CreatePath(new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), GetCornerRadius()))
            using (Pen pen = new Pen(BorderColor, thickness))
            {
                pen.Alignment = PenAlignment.Inset;
                e.Graphics.DrawPath(pen, path);
            }

            e.Graphics.SmoothingMode = previous;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Follower.Detach();

            base.Dispose(disposing);
        }
    }
}
