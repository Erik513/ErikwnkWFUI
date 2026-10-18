using System;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// What the two progress bars (<see cref="ProgressBar"/> and <see cref="SlimProgressBar"/>) have in common: how much of the
    /// bar is filled, how a fill in each <see cref="ProgressBarStyle"/> looks, and how the text and the percentage are laid out and
    /// painted (readable on the track and on the fill, cut, or moving). One place, so the two cannot drift apart.
    /// </summary>
    internal static class ProgressBarPainting
    {
        // How far the block of the marquee style moves per step, and how often (MarqueeAnimationSpeed milliseconds).
        public const int MarqueeStepPixels = 4;

        // The classic (blocks) style: blocks about three quarters as wide as the bar is high, with a gap of 2 px.
        private const int BlockGap = 2;

        // What the colors of a bar have to reach (WCAG): the fill against the track is the state the bar shows (3:1 for the parts of a
        // control), the text against what it stands on is text (4.5:1).
        public const double FillContrast = 3.0;
        public const double TextContrast = 4.5;

        /// <summary>
        /// The text color for text on <paramref name="background"/>: the best of the dark and the light text color of the library, and
        /// - where that is not enough on a mid-tone (gray, teal, pink) - pure black or white, whichever reads better.
        /// </summary>
        public static Color GetTextColorOn(Color background)
        {
            Color best = UIColors.GetBestTextColor(background);

            if (UIColors.GetContrastRatio(best, background) >= TextContrast)
                return best;

            return UIColors.GetContrastRatio(Color.White, background) >= UIColors.GetContrastRatio(Color.Black, background) ? Color.White : Color.Black;
        }

        /// <summary>
        /// What the status colors (red, orange, yellow, green) have to reach against the track: 2:1, less than the other fills. A
        /// stronger demand turned the red of a status on a dark track into a pale pink (and the green and the orange into lighter ones), only to
        /// stand out from the lighter track; the hue tells the fill from the track as well.
        /// </summary>
        public const double StatusFillContrast = 2.0;

        /// <summary>The fill of a status bar at a value (red to green), moved just far enough to stand out from the track (<see cref="StatusFillContrast"/>).</summary>
        public static Color GetStatusFill(int value, int minimum, int maximum, Color track)
        {
            return UIColors.EnsureContrast(UIColors.GetStatusColor(value, minimum, maximum), track, StatusFillContrast);
        }

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

        /// <summary>
        /// The value as a percentage of the range, as text, written the way of the language of the library: "42%" in English, "42 %"
        /// in German. A range without width is 0 %.
        /// </summary>
        public static string GetPercentText(int minimum, int maximum, int value)
        {
            return (Math.Round(GetFraction(minimum, maximum, value) * 100) / 100.0).ToString("P0", UIStrings.NumberCulture);
        }

        /// <summary>The text and the percentage in one: "Upload - 42%", or only one of them, or nothing.</summary>
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

        // Where the percentage stands in the area, for its alignment and its width.
        private static Rectangle PlacePercentage(Rectangle area, HorizontalAlignment alignment, int width)
        {
            Rectangle place = new Rectangle(area.X, area.Y, width, area.Height);

            if (alignment == HorizontalAlignment.Right)
                place.X = area.Right - width;
            else if (alignment == HorizontalAlignment.Center)
                place.X = area.X + (area.Width - width) / 2;

            return place;
        }

        // The room the text takes next to the percentage: on the side where the text itself is.
        private static Rectangle RoomNextTo(Rectangle area, Rectangle percentArea, HorizontalAlignment textAlign, HorizontalAlignment percentAlign)
        {
            Rectangle room = area;

            // (With the percentage in the middle, the text goes to the side its own alignment names.)
            bool onTheLeft = percentAlign == HorizontalAlignment.Right
                || (percentAlign == HorizontalAlignment.Center && textAlign == HorizontalAlignment.Left);

            if (onTheLeft)
                room.Width = Math.Max(0, percentArea.X - PartsGap - area.X);
            else
            {
                room.X = percentArea.Right + PartsGap;
                room.Width = Math.Max(0, area.Right - room.X);
            }

            return room;
        }

        /// <summary>
        /// Draws the text and the percentage (<paramref name="percent"/> is null when it is not shown) into <paramref name="area"/>.
        /// Alone, each stands where its alignment says. Together at the same place they are one text ("Upload - 42%"); at different
        /// places they are drawn apart, the percentage keeping the room of its widest form (<paramref name="widestPercent"/>) and the
        /// text taking what is left next to it (and being centered, aligned or cut there). Both are readable on the track and on
        /// the fill, see <see cref="PaintText"/>. A text that is too wide for its room moves there when the
        /// <paramref name="scroller"/> has a scroll for it (then the percentage stays where it is, and one text that was drawn
        /// together with it is drawn apart from it, the percentage at the right end). Returns whether the text could not be shown
        /// whole (it moves or is cut), for the tooltip with the full text.; otherwise it is cut with an ellipsis.
        /// </summary>
        public static bool PaintTexts(
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
            Rectangle fillBounds,
            ProgressTextScroller scroller = null)
        {
            bool hasText = !string.IsNullOrEmpty(text);
            bool hasPercent = percent != null;

            if (!hasText && !hasPercent)
            {
                scroller?.Configure(0, 0, font);

                return false;
            }

            Rectangle textArea = area;
            Rectangle percentArea = Rectangle.Empty;
            string textPart;
            string percentPart = null;
            HorizontalAlignment textAlignment;
            bool together = hasText != hasPercent || textAlign == percentAlign;

            if (together)
            {
                textPart = Compose(text, hasPercent, percent);
                textAlignment = hasText && !hasPercent ? textAlign : percentAlign;
            }
            else
            {
                int percentWidth = Math.Min(area.Width, MeasureText(widestPercent, font).Width);
                percentArea = PlacePercentage(area, percentAlign, percentWidth);
                textArea = RoomNextTo(area, percentArea, textAlign, percentAlign);
                textPart = text;
                percentPart = percent;
                textAlignment = textAlign;
            }

            bool moves = false;

            if (scroller != null && hasText && scroller.Scroll.Mode != LabelScrollMode.None)
            {
                int width = MeasureText(textPart, font).Width;

                // One text with the percentage that is too wide: the percentage goes to the right end, and the text moves next to it.
                if (together && hasPercent && width > textArea.Width)
                {
                    HorizontalAlignment side = HorizontalAlignment.Right;
                    int percentWidth = Math.Min(area.Width, MeasureText(widestPercent, font).Width);

                    percentArea = PlacePercentage(area, side, percentWidth);
                    textArea = RoomNextTo(area, percentArea, textAlign, side);
                    textPart = text;
                    percentPart = percent;
                    textAlignment = textAlign;
                    percentAlign = side;
                    width = MeasureText(textPart, font).Width;
                }

                scroller.Configure(width, textArea.Width, font);
                moves = scroller.IsMoving;
            }
            else
            {
                scroller?.Configure(0, 0, font);
            }

            if (moves)
                PaintMovingText(graphics, textArea, textPart, font, onTrack, onFill, fillBounds, scroller);
            else
                PaintText(graphics, textArea, textPart, font, onTrack, onFill, fillBounds, ToFlags(textAlignment));

            if (percentPart != null)
                PaintText(graphics, percentArea, percentPart, font, onTrack, onFill, fillBounds, TextFormatFlags.HorizontalCenter);

            // Whether the text could not be shown whole: it moves, or it is cut with an ellipsis.
            return hasText && (moves || MeasureText(textPart, font).Width > textArea.Width);
        }

        // The text that moves through its room: back and forth from its start to its end, or round and round with a separator.
        private static void PaintMovingText(
            Graphics graphics,
            Rectangle room,
            string text,
            Font font,
            Color onTrack,
            Color onFill,
            Rectangle fillBounds,
            ProgressTextScroller scroller)
        {
            if (room.Width <= 0 || room.Height <= 0)
                return;

            TextFormatFlags flags = (TextFlags & ~TextFormatFlags.EndEllipsis) | TextFormatFlags.Left | TextFormatFlags.PreserveGraphicsClipping;
            LabelScroll scroll = scroller.Scroll;
            string separator = scroll.Separator.ToText();

            Action<Color, Rectangle> pass = (color, clip) =>
            {
                System.Drawing.Drawing2D.GraphicsState state = graphics.Save();

                try
                {
                    graphics.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Intersect);

                    if (scroll.Mode == LabelScrollMode.BackAndForth)
                    {
                        TextRenderer.DrawText(graphics, text, font, new Rectangle(room.X - scroller.Offset, room.Y, scroller.TextWidth + 8, room.Height), color, flags);

                        return;
                    }

                    // The round: the text, a gap, the separator, a gap - and the next round behind it (and one before it, while it is on its way in).
                    for (int round = -1; round <= 1; round++)
                    {
                        int left = room.X - scroller.Offset + round * scroller.Cycle;

                        if (left > room.Right || left + scroller.Cycle < room.X)
                            continue;

                        TextRenderer.DrawText(graphics, text, font, new Rectangle(left, room.Y, scroller.TextWidth + 8, room.Height), color, flags);

                        if (separator.Length > 0)
                            TextRenderer.DrawText(graphics, separator, font, new Rectangle(left + scroller.TextWidth + scroller.Gap, room.Y, scroller.SeparatorWidth + 8, room.Height), color, flags);
                    }
                }
                finally
                {
                    graphics.Restore(state);
                }
            };

            pass(onTrack, room);

            Rectangle covered = Rectangle.Intersect(room, fillBounds);

            if (!covered.IsEmpty)
                pass(onFill, covered);
        }

        /// <summary>The size of a text as it is drawn by <see cref="PaintText"/>.</summary>
        public static Size MeasureText(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFlags | TextFormatFlags.NoClipping);
        }
    }
}
