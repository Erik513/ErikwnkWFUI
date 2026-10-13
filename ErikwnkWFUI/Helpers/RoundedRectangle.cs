using System.Drawing;
using System.Drawing.Drawing2D;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>The outline of a rectangle with rounded corners, for the controls that draw their own boxes and switches.</summary>
    internal static class RoundedRectangle
    {
        /// <summary>
        /// The path of <paramref name="rect"/> with corners of the given radius. The caller disposes it. A radius of half the
        /// height makes a pill.
        /// </summary>
        public static GraphicsPath CreatePath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
