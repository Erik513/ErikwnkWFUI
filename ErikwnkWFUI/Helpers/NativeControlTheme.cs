using System;
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
