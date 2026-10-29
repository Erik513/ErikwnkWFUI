using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>The outline of a rectangle with rounded corners, for the controls that draw their own boxes and switches.</summary>
    internal static class RoundedRectangle
    {
        /// <summary>
        /// The path of <paramref name="rect"/> with corners of the given radius. The caller disposes it. A radius of half the
        /// height makes a pill; a radius of 0 or less gives a plain rectangle, and a radius larger than half of the shorter side is
        /// taken as half of it (the corners would run into each other otherwise).
        /// </summary>
        public static GraphicsPath CreatePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);

            // (An arc of no size is not a corner: GDI+ does not take it.)
            if (radius <= 0)
            {
                path.AddRectangle(rect);

                return path;
            }

            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>The same with a rectangle and a radius in fractions of a pixel (for a line that runs along the middle of a pixel).</summary>
        public static GraphicsPath CreatePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);

            if (radius <= 0)
            {
                path.AddRectangle(rect);

                return path;
            }

            float diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
