using System;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The parts of a native control that the library does not draw itself - the scroll bars of a
    /// list or a grid, the header strip behind the column titles - take the visual style the system
    /// uses for its own dark windows when the theme is dark, so they are not a light block on dark
    /// content. (The style name is not part of the documented API, but the system's own dark
    /// windows use it and it is widely relied on; where it does not exist the control just keeps its
    /// normal look.)
    /// </summary>
    internal static class NativeControlTheme
    {
        public static bool IsDark
        {
            get { return !UIColors.IsLight(UIColors.BackgroundMedium); }
        }

        /// <summary>
        /// Keeps the scroll bars of a control in the style of the current theme: set whenever its window is
        /// created and again - with the frame painted anew - whenever the theme changes. Called once in the
        /// constructor of a control; there is no other place that decides about the style of a scroll bar.
        /// </summary>
        public static void Track(Control control)
        {
            Track(control, () => control.Handle, ApplyToScrollBars);
        }

        /// <summary>
        /// The same for another native window that belongs to the control (the header of a list view, the drop-down
        /// list of a combo box): <paramref name="getHandle"/> finds it (it may be zero), <paramref name="apply"/>
        /// sets the style.
        /// </summary>
        public static void Track(Control control, Func<IntPtr> getHandle, Action<IntPtr> apply)
        {
            Action<bool> refresh = redraw =>
            {
                if (control.IsDisposed || !control.IsHandleCreated)
                    return;

                IntPtr handle = getHandle();

                if (handle == IntPtr.Zero)
                    return;

                apply(handle);

                // The frame (where the scroll bars are) is measured and painted again.
                if (redraw)
                    SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            };

            control.HandleCreated += (sender, e) => refresh(false);
            ThemeWatcher.Attach(control, () => refresh(true));

            if (control.IsHandleCreated)
                refresh(false);
        }

        // The color of the track of the dark scroll bars (the system's own).
        private static readonly System.Drawing.Color DarkScrollBarTrack = System.Drawing.Color.FromArgb(23, 23, 23);

        /// <summary>
        /// Where both scroll bars of a window meet and the system leaves a light square even with dark bars (in window
        /// coordinates, as the non-client area is painted); false when the theme is light or the window does not show both bars.
        /// </summary>
        public static bool TryGetScrollBarCorner(Control control, out System.Drawing.Rectangle corner)
        {
            corner = System.Drawing.Rectangle.Empty;

            if (!IsDark || !control.IsHandleCreated)
                return false;

            int style = GetWindowLong(control.Handle, GWL_STYLE);

            if ((style & WS_HSCROLL) == 0 || (style & WS_VSCROLL) == 0)
                return false;

            NativeRect window;
            NativeRect client;

            if (!GetWindowRect(control.Handle, out window) || !GetClientRect(control.Handle, out client))
                return false;

            int barWidth = SystemInformation.VerticalScrollBarWidth * control.DeviceDpi / 96;
            int barHeight = SystemInformation.HorizontalScrollBarHeight * control.DeviceDpi / 96;
            int border = Math.Max(0, (window.Right - window.Left - client.Right - barWidth) / 2);
            corner = new System.Drawing.Rectangle(border + client.Right, border + client.Bottom, barWidth, barHeight);

            return true;
        }

        /// <summary>Paints that square in the color of the track of the bars. Called after the system has painted the frame (WM_NCPAINT).</summary>
        public static void PaintScrollBarCorner(Control control)
        {
            System.Drawing.Rectangle corner;

            if (!TryGetScrollBarCorner(control, out corner))
                return;

            IntPtr hdc = GetWindowDC(control.Handle);

            if (hdc == IntPtr.Zero)
                return;

            try
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHdc(hdc))
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(DarkScrollBarTrack))
                    g.FillRectangle(brush, corner);
            }
            finally
            {
                ReleaseDC(control.Handle, hdc);
            }
        }

        // What sets the style of a window (a seam for the tests, which have no visual styles to look at).
        internal static Action<IntPtr, string> SetStyle = (handle, name) => SetWindowTheme(handle, name, null);

        /// <summary>Scroll bars of a window (or of a scroll bar control).</summary>
        public static void ApplyToScrollBars(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                SetStyle(handle, IsDark ? "DarkMode_Explorer" : "Explorer");
        }

        /// <summary>
        /// The header of a list view: the style of the system's own item lists, which also darkens the
        /// strip behind the last column title.
        /// </summary>
        public static void ApplyToItemsView(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                SetStyle(handle, IsDark ? "DarkMode_ItemsView" : "ItemsView");
        }
    }
}
