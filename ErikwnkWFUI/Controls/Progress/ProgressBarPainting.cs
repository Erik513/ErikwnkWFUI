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
        public static Rectangle PaintFill(Graphics graphics, Rectangle area, ProgressBarStyle style, double fraction, Color fill, int marqueeOffset)
        {
            if (area.Width <= 0 || area.Height <= 0)
                return Rectangle.Empty;

            using (SolidBrush brush = new SolidBrush(fill))
            {
                if (style == ProgressBarStyle.Marquee)
                {
                    int blockWidth = GetMarqueeBlockWidth(area.Width);
                    int position = marqueeOffset % (area.Width + blockWidth);
                    Rectangle block = Rectangle.Intersect(area, new Rectangle(area.X - blockWidth + position, area.Y, blockWidth, area.Height));

                    if (!block.IsEmpty)
                        graphics.FillRectangle(brush, block);

                    return block;
                }

                int fillWidth = (int)Math.Round(area.Width * fraction);

                if (fillWidth <= 0)
                    return Rectangle.Empty;

                if (style != ProgressBarStyle.Blocks)
                {
                    graphics.FillRectangle(brush, new Rectangle(area.X, area.Y, fillWidth, area.Height));

                    return new Rectangle(area.X, area.Y, fillWidth, area.Height);
                }

                int step = Math.Max(2, (int)(area.Height * 0.75)) + BlockGap;
                int blockSize = step - BlockGap;

                for (int x = 0; x < fillWidth; x += step)
                    graphics.FillRectangle(brush, new Rectangle(area.X + x, area.Y, Math.Min(blockSize, fillWidth - x), area.Height));

                return new Rectangle(area.X, area.Y, fillWidth, area.Height);
            }
        }

        /// <summary>The value as a percentage of the range, as text ("42 %"); a range without width is 0 %.</summary>
        public static string GetPercentText(int minimum, int maximum, int value)
        {
            return (int)Math.Round(GetFraction(minimum, maximum, value) * 100) + " %";
        }

        /// <summary>The text and the percentage in one: "Upload - 42 %", or only one of them, or nothing.</summary>
        public static string Compose(string text, bool showPercentage, string percent)
        {
            bool hasText = !string.IsNullOrEmpty(text);

            if (!showPercentage)
                return hasText ? text : "";

            return hasText ? text + " \u2013 " + percent : percent;
        }

        private const TextFormatFlags TextFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
            | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;

        /// <summary>
        /// Draws a text into <paramref name="area"/> so that it can be read on the track and on the fill: in <paramref name="onTrack"/>
        /// everywhere, and again in <paramref name="onFill"/> where the fill lies (<paramref name="fillBounds"/>).
        /// </summary>
        public static void PaintText(
            Graphics graphics,
            Rectangle area,
            string text,
            Font font,
            Color onTrack,
            Color onFill,
            Rectangle fillBounds,
            TextFormatFlags alignment)
        {
            if (string.IsNullOrEmpty(text) || area.Width <= 0 || area.Height <= 0)
                return;

            TextRenderer.DrawText(graphics, text, font, area, onTrack, TextFlags | alignment);

            Rectangle covered = Rectangle.Intersect(area, fillBounds);

            if (covered.IsEmpty)
                return;

            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();

            try
            {
                // (TextRenderer only keeps to the clip of the Graphics with PreserveGraphicsClipping.)
                graphics.SetClip(covered, System.Drawing.Drawing2D.CombineMode.Intersect);
                TextRenderer.DrawText(graphics, text, font, area, onFill, TextFlags | alignment | TextFormatFlags.PreserveGraphicsClipping);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // The gap between the text and the percentage when they are drawn apart.
        private const int PartsGap = 8;

        private static TextFormatFlags ToFlags(HorizontalAlignment alignment)
        {
            return alignment == HorizontalAlignment.Left ? TextFormatFlags.Left
                : alignment == HorizontalAlignment.Right ? TextFormatFlags.Right
                : TextFormatFlags.HorizontalCenter;
        }

        /// <summary>
        /// Draws the text and the percentage (<paramref name="percent"/> is null when it is not shown) into <paramref name="area"/>.
        /// Alone, each stands where its alignment says. Together at the same place they are one text ("Upload - 42 %"); at different
        /// places they are drawn apart, the percentage keeping the room of its widest form (<paramref name="widestPercent"/>) and the
        /// text taking what is left next to it (and being centered, aligned or cut there). Both are readable on the track and on
        /// the fill, see <see cref="PaintText"/>.
        /// </summary>
        public static void PaintTexts(
            Graphics graphics,
            Rectangle area,
            string text,
            string percent,
            string widestPercent,
            HorizontalAlignment textAlign,
            HorizontalAlignment percentAlign,
            Font font,
            Color onTrack,
            Color onFill,
            Rectangle fillBounds)
        {
            bool hasText = !string.IsNullOrEmpty(text);
            bool hasPercent = percent != null;

            if (!hasText && !hasPercent)
                return;

            if (hasText != hasPercent || textAlign == percentAlign)
            {
                HorizontalAlignment alignment = hasText && !hasPercent ? textAlign : percentAlign;
                PaintText(graphics, area, Compose(text, hasPercent, percent), font, onTrack, onFill, fillBounds, ToFlags(alignment));

                return;
            }

            int percentWidth = Math.Min(area.Width, MeasureText(widestPercent, font).Width);
            Rectangle percentArea = new Rectangle(area.X, area.Y, percentWidth, area.Height);

            if (percentAlign == HorizontalAlignment.Right)
                percentArea.X = area.Right - percentWidth;
            else if (percentAlign == HorizontalAlignment.Center)
                percentArea.X = area.X + (area.Width - percentWidth) / 2;

            // The text takes the room next to the percentage, on the side where the text itself is.
            Rectangle textArea = area;
            // (With the percentage in the middle, the text goes to the side its own alignment names.)
            bool textOnTheLeft = percentAlign == HorizontalAlignment.Right
                || (percentAlign == HorizontalAlignment.Center && textAlign == HorizontalAlignment.Left);

            if (textOnTheLeft)
                textArea.Width = Math.Max(0, percentArea.X - PartsGap - area.X);
            else
            {
                textArea.X = percentArea.Right + PartsGap;
                textArea.Width = Math.Max(0, area.Right - textArea.X);
            }

            PaintText(graphics, textArea, text, font, onTrack, onFill, fillBounds, ToFlags(textAlign));
            PaintText(graphics, percentArea, percent, font, onTrack, onFill, fillBounds, TextFormatFlags.HorizontalCenter);
        }

        /// <summary>The size of a text as it is drawn by <see cref="PaintText"/>.</summary>
        public static Size MeasureText(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFlags | TextFormatFlags.NoClipping);
        }
    }
}
