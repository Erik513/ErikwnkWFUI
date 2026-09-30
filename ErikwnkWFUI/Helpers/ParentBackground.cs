using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Lets a control with a transparent background show its parent's own
    /// background - drawn by the parent itself, so images and gradients come
    /// along.
    /// </summary>
    internal static class ParentBackground
    {
        /// <summary>
        /// Runs <paramref name="paintParent"/> with arguments that cover the
        /// parent, shifted so it lands behind <paramref name="child"/>. The
        /// callback is where the control calls its own (protected)
        /// <c>InvokePaintBackground</c> / <c>InvokePaint</c> with the parent.
        /// </summary>
        public static void Paint(Control child, Graphics graphics, Action<PaintEventArgs> paintParent)
        {
            Control parent = child.Parent;

            if (parent == null)
                return;

            GraphicsState state = graphics.Save();

            try
            {
                graphics.TranslateTransform(-child.Left, -child.Top);
                paintParent(new PaintEventArgs(graphics, parent.ClientRectangle));
            }
            finally
            {
                graphics.Restore(state);
            }
        }
    }
}
