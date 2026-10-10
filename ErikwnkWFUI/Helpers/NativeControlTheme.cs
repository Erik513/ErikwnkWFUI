using System;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
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

        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_FRAMECHANGED = 0x0020;

        /// <summary>Scroll bars of a window (or of a scroll bar control).</summary>
        public static void ApplyToScrollBars(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                SetWindowTheme(handle, IsDark ? "DarkMode_Explorer" : "Explorer", null);
        }

        /// <summary>
        /// The header of a list view: the style of the system's own item lists, which also darkens the
        /// strip behind the last column title.
        /// </summary>
        public static void ApplyToItemsView(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                SetWindowTheme(handle, IsDark ? "DarkMode_ItemsView" : "ItemsView", null);
        }
    }
}
