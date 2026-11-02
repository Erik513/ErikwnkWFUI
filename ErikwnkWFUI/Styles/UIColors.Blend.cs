using System;
using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    // Mixing colors: lightness shifts, blends, darker and lighter shades, the status color.
    public static partial class UIColors
    {
        /// <summary>
        /// The color with its HSL lightness moved by <paramref name="delta"/>
        /// (-1 to 1), hue and saturation kept. Unlike blending toward white or
        /// black this keeps a color saturated, so a lighter blue is still a
        /// blue and not a pale one.
        /// </summary>
        internal static Color ShiftLightness(Color color, double delta)
        {
            double hue = color.GetHue();
            double saturation = color.GetSaturation();
            double lightness = Math.Max(0, Math.Min(1, color.GetBrightness() + delta));

            double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
            double m = lightness - chroma / 2;
            double r, g, b;

            if (hue < 60) { r = chroma; g = x; b = 0; }
            else if (hue < 120) { r = x; g = chroma; b = 0; }
            else if (hue < 180) { r = 0; g = chroma; b = x; }
            else if (hue < 240) { r = 0; g = x; b = chroma; }
            else if (hue < 300) { r = x; g = 0; b = chroma; }
            else { r = chroma; g = 0; b = x; }

            return Color.FromArgb(
                color.A,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        /// <summary>The color with the share <paramref name="amount"/> (0-1) taken off each channel: 0.3 is 30 % darker. (<see cref="Darken(Color, int)"/> takes RGB units off instead.)</summary>
        internal static Color DarkenByShare(Color color, double amount)
        {
            return Color.FromArgb(
                (int)(color.R * (1 - amount)),
                (int)(color.G * (1 - amount)),
                (int)(color.B * (1 - amount)));
        }

        // The red -> yellow -> green of a status progress bar. Two segments rather than one
        // straight blend, which would pass through a muddy brown around the middle instead of a
        // clear yellow. The breakpoint is not the middle (0.5): green should only dominate near
        // the top, so red -> yellow gets the larger share (0-65%) and only the last third blends
        // yellow -> green; a flat 50/50 split turned green too early.
        private const double YellowBreakpoint = 0.65;

        /// <summary>
        /// The status color of <paramref name="value"/> between <paramref name="minimum"/> and
        /// <paramref name="maximum"/>: red at the low end, yellow towards the middle, green at the top.
        /// </summary>
        internal static Color GetStatusColor(int value, int minimum, int maximum)
        {
            double range = maximum - minimum;
            double fraction = range > 0 ? (value - minimum) / range : 0;
            fraction = Math.Max(0, Math.Min(1, fraction));

            if (fraction <= YellowBreakpoint)
                return Blend(Red, Yellow, fraction / YellowBreakpoint);

            return Blend(Yellow, Green, (fraction - YellowBreakpoint) / (1 - YellowBreakpoint));
        }

        /// <summary>The color the share <paramref name="t"/> (0-1) of the way from <paramref name="from"/> to <paramref name="to"/>, rounded.</summary>
        internal static Color Blend(Color from, Color to, double t)
        {
            t = Math.Max(0, Math.Min(1, t));

            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }

        /// <summary>
        /// The color a given share (0-1) of the way from <paramref name="color"/>
        /// to <paramref name="target"/> - for muting a text toward its
        /// background.
        /// </summary>
        internal static Color BlendToward(Color color, Color target, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));

            return Color.FromArgb(
                color.A,
                color.R + (int)((target.R - color.R) * amount),
                color.G + (int)((target.G - color.G) * amount),
                color.B + (int)((target.B - color.B) * amount));
        }

        /// <summary>The color the share <paramref name="amount"/> (0-1) of the way to white. (<see cref="Lighten(Color, int)"/> adds RGB units instead.)</summary>
        private static Color LightenByShare(Color color, double amount)
        {
            return Color.FromArgb(
                color.R + (int)((255 - color.R) * amount),
                color.G + (int)((255 - color.G) * amount),
                color.B + (int)((255 - color.B) * amount));
        }

        // Flat RGB-unit variants - a different blend than the fractional
        // (0-1 "amount") private overloads just above, which this palette's
        // own generation uses to derive Primary/Secondary shades from the
        // accent. These exist for the much smaller "row N alternates with a
        // slightly darker shade of row N-1" case several controls
        // (ListBox, ListView, ReadOnlyDataGridView) each used to carry
        // their own identical copy of - consolidated here since the exact
        // same formula doesn't belong duplicated three times over.
        /// <summary>The color with <paramref name="amount"/> (0-255) taken off each channel, not below 0 - for the alternating row of a list.</summary>
        public static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        /// <summary>The color with <paramref name="amount"/> (0-255) added to each channel, not above 255 - for the alternating row of a list.</summary>
        public static Color Lighten(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }
    }
}
