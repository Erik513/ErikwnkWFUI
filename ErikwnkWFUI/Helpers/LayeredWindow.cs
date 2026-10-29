using System;
using System.Drawing;
using ErikwnkWFUI.Native;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>Gives a layered window its picture: a bitmap with an alpha value for every pixel (<c>UpdateLayeredWindow</c>).</summary>
    internal static class LayeredWindow
    {
        /// <param name="window">The window (it has the layered style).</param>
        /// <param name="picture">A 32-bit bitmap with premultiplied alpha, as large as the window.</param>
        /// <param name="alpha">How opaque the whole window is on top of that, 255 for not at all see-through.</param>
        public static void Update(IntPtr window, Bitmap picture, byte alpha)
        {
            IntPtr screen = GetDC(IntPtr.Zero);
            IntPtr memory = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr previous = IntPtr.Zero;

            // (Everything is made inside the try: when the bitmap cannot be made, the two device contexts are still given back.)
            try
            {
                memory = CreateCompatibleDC(screen);
                bitmap = picture.GetHbitmap(Color.FromArgb(0));
                previous = SelectObject(memory, bitmap);
                NativeSize size = new NativeSize { Width = picture.Width, Height = picture.Height };
                NativePoint source = new NativePoint();
                NativeBlendFunction blend = new NativeBlendFunction
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = alpha,
                    AlphaFormat = AC_SRC_ALPHA
                };

                // (No destination point: the window stays where it is.)
                UpdateLayeredWindow(window, screen, IntPtr.Zero, ref size, memory, ref source, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (memory != IntPtr.Zero && previous != IntPtr.Zero)
                    SelectObject(memory, previous);

                if (bitmap != IntPtr.Zero)
                    DeleteObject(bitmap);

                if (memory != IntPtr.Zero)
                    DeleteDC(memory);

                ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
