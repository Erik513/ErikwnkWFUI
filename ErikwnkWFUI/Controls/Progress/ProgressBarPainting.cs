using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// What the two progress bars (<see cref="ProgressBar"/> and <see cref="SlimProgressBar"/>) have in common: how much of the
    /// bar is filled, and how a fill in each <see cref="ProgressBarStyle"/> looks. One place, so the two cannot drift apart.
    /// </summary>
    internal static class ProgressBarPainting
    {
        // How far the block of the marquee style moves per step, and how often (MarqueeAnimationSpeed milliseconds).
        public const int MarqueeStepPixels = 4;

        // The classic (blocks) style: blocks about three quarters as wide as the bar is high, with a gap of 2 px.
        private const int BlockGap = 2;

        /// <summary>The part of the bar that is filled, from 0 to 1.</summary>
        public static double GetFraction(int minimum, int maximum, int value)
        {
            double range = maximum - minimum;

            return range > 0 ? Math.Max(0, Math.Min(1, (value - minimum) / range)) : 0;
        }

        /// <summary>The width of the moving block of the marquee style on a bar of the given width.</summary>
        public static int GetMarqueeBlockWidth(int barWidth)
        {
            return Math.Min(barWidth, Math.Max(8, barWidth / 5));
        }

        /// <summary>
        /// Paints the fill into <paramref name="area"/>: a block that runs through it (<see cref="ProgressBarStyle.Marquee"/>, at
        /// <paramref name="marqueeOffset"/>), or the filled part (<paramref name="fraction"/>) as one piece or as blocks.
        /// </summary>
        public static void PaintFill(Graphics graphics, Rectangle area, ProgressBarStyle style, double fraction, Color fill, int marqueeOffset)
        {
            if (area.Width <= 0 || area.Height <= 0)
                return;

            using (SolidBrush brush = new SolidBrush(fill))
            {
                if (style == ProgressBarStyle.Marquee)
                {
                    int blockWidth = GetMarqueeBlockWidth(area.Width);
                    int position = marqueeOffset % (area.Width + blockWidth);
                    Rectangle block = Rectangle.Intersect(area, new Rectangle(area.X - blockWidth + position, area.Y, blockWidth, area.Height));

                    if (!block.IsEmpty)
                        graphics.FillRectangle(brush, block);

                    return;
                }

                int fillWidth = (int)Math.Round(area.Width * fraction);

                if (fillWidth <= 0)
                    return;

                if (style != ProgressBarStyle.Blocks)
                {
                    graphics.FillRectangle(brush, new Rectangle(area.X, area.Y, fillWidth, area.Height));

                    return;
                }

                int step = Math.Max(2, (int)(area.Height * 0.75)) + BlockGap;
                int blockSize = step - BlockGap;

                for (int x = 0; x < fillWidth; x += step)
                    graphics.FillRectangle(brush, new Rectangle(area.X + x, area.Y, Math.Min(blockSize, fillWidth - x), area.Height));
            }
        }
    }
}
