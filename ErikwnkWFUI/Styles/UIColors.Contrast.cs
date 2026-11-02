using System;
using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    // Which text reads on which fill, the contrast ratios and the colors that keep a distance from a surface.
    public static partial class UIColors
    {
        // Above this, a white or gray background is bright enough that dark
        // text wins; at/below it, light text wins. Deliberately above a literal
        // 50% midpoint (not just "which one contrasts more"): light text on a
        // saturated blue/green button is the convention. Colors never get dark
        // text (see UsesDarkText), however bright - this threshold is for the
        // neutral surfaces: white, the light grays, the light theme.
        private const double LightBackgroundThreshold = 0.55;

        /// <summary>
        /// White or near-black, whichever reads better on top of the given
        /// background - e.g. white text is unreadable on a bright yellow
        /// accent even though it's fine on the default dark blue. Not the
        /// current theme's TextPrimary - callers use this specifically for
        /// surfaces (accent/semantic colors) that don't follow the base
        /// theme, so the answer must not depend on it either.
        /// </summary>
        public static Color GetContrastingForeColor(Color background)
        {
            return UsesDarkText(background) ? DarkForeColor : LightForeColor;
        }

        // Only white - and a neutral light gray - is a surface that takes dark text. A color keeps light text however bright it is: white on a
        // yellow or an orange is still plainly readable, and a program in which only some of the accent colors had black text would look as if it
        // had two kinds of buttons. Only a color so bright that white is all but gone on it (a pale lemon yellow, 1.0 to 1.4:1) is a white for
        // this purpose.
        private const double ColoredSaturation = 0.35;
        private const double ColoredMaxLightness = 0.72;
        internal const double ColoredLightTextMinimum = 1.5;

        /// <summary>Whether a color is a gray (red, green and blue equal): white, black and every gray have no hue.</summary>
        internal static bool IsNeutral(Color color)
        {
            return color.R == color.G && color.G == color.B;
        }

        /// <summary>Whether a fill is a color (a hue that is clearly seen: yellow, orange, blue ...) and not white, a gray or a pale tint of one (a pale tint is a light surface: it takes dark text).</summary>
        internal static bool IsColored(Color color)
        {
            return color.GetSaturation() > ColoredSaturation && color.GetBrightness() < ColoredMaxLightness;
        }

        private static bool UsesDarkText(Color fill)
        {
            if (IsColored(fill))
                return GetContrastRatio(LightForeColor, fill) < ColoredLightTextMinimum;

            return IsLight(fill);
        }

        /// <summary>Whether a color is bright enough that dark text is the one to put on it.</summary>
        public static bool IsLight(Color color)
        {
            return RelativeLuminance(color) > LightBackgroundThreshold;
        }

        /// <summary>
        /// The text color for <paramref name="page"/>: <paramref name="preferred"/> moved lighter or darker as far as it takes to reach <paramref name="minimum"/>
        /// contrast, and unchanged where it reads already. On a mid-tone page that is not enough (the color is moved one way only, and neither end of that way may
        /// reach the contrast): black or white, whichever has the higher contrast, which on any page is at least 4.58:1.
        /// </summary>
        internal static Color GetReadableText(Color preferred, Color page, double minimum)
        {
            Color moved = EnsureContrast(preferred, page, minimum);

            if (GetContrastRatio(moved, page) >= minimum)
                return moved;

            return GetContrastRatio(Color.Black, page) >= GetContrastRatio(Color.White, page) ? Color.Black : Color.White;
        }

        /// <summary>
        /// The outline of a control on <paramref name="page"/>: the best text color for the page,
        /// moved toward it just far enough to keep 3:1 against it (the bar for the boundary of a
        /// control), so it stands out on any page color.
        /// </summary>
        internal static Color GetOutlineColor(Color page)
        {
            Color text = GetBestTextColor(page);

            for (double toward = 0.7; toward > 0; toward -= 0.05)
            {
                Color outline = BlendToward(text, page, toward);

                if (GetContrastRatio(outline, page) >= 3.0)
                    return outline;
            }

            return text;
        }

        /// <summary>
        /// The quiet line of a panel (frame, lines of a table or a card) on <paramref name="surface"/>: the theme's
        /// <see cref="BorderMedium"/>, moved only as far as needed to keep a ratio of 1.5:1 against the surface - on a color (green,
        /// accent) the gray of the theme can be as dark as the surface itself.
        /// </summary>
        internal static Color GetLineColor(Color surface)
        {
            return EnsureContrast(BorderMedium, surface, QuietLineContrast);
        }

        private const double QuietLineContrast = 1.5;

        /// <summary>
        /// <paramref name="color"/> moved lighter (on a dark page) or darker (on a light one), in
        /// small steps, until it has at least <paramref name="minimum"/> contrast against the page.
        /// </summary>
        internal static Color EnsureContrast(Color color, Color page, double minimum)
        {
            double step = IsLight(page) ? -0.03 : 0.03;

            for (int i = 0; i < 40 && GetContrastRatio(color, page) < minimum; i++)
                color = ShiftLightness(color, step);

            return color;
        }

        /// <summary>
        /// A track for a slider on <paramref name="surface"/>: the surface itself moved lighter (on a dark one) or darker (on a light one)
        /// until it stands out by the quiet 1.5:1 - and no more, in either theme. (<see cref="GetLineColor"/> keeps the gray of the theme, which
        /// may lie far from a surface of another brightness: no color would then stand out from both.)
        /// </summary>
        internal static Color GetQuietTrackColor(Color surface)
        {
            return EnsureContrast(surface, surface, QuietLineContrast);
        }

        /// <summary>
        /// The same against two colors at once: the nearest lightness of <paramref name="color"/> (hue and saturation kept) that has at least
        /// <paramref name="minimum"/> contrast against the page and at least <paramref name="minimumOther"/> against <paramref name="other"/> (a color that lies on the page, such as the
        /// track of a slider), looking in the direction of the page first (lighter on a dark page, darker on a light one) and then the other way:
        /// the other color may lie on the far side of the color, and moving it for each of the two in turn would move it back and forth.
        /// Where no lightness has both, the one whose worse contrast is the best.
        /// </summary>
        internal static Color EnsureContrast(Color color, Color page, double minimum, Color other, double minimumOther)
        {
            double toward = IsLight(page) ? -0.01 : 0.01;
            Color best = color;
            double bestWorst = -1;

            for (int i = 0; i <= 100; i++)
            {
                foreach (double direction in new[] { toward, -toward })
                {
                    Color candidate = i == 0 ? color : ShiftLightness(color, direction * i);
                    double onPage = GetContrastRatio(candidate, page);
                    double onOther = GetContrastRatio(candidate, other);

                    if (onPage >= minimum && onOther >= minimumOther)
                        return candidate;

                    // (How far short each is, in the share of what it needs: the better of the two worse ones wins.)
                    double worst = Math.Min(onPage / minimum, onOther / minimumOther);

                    if (worst > bestWorst)
                    {
                        bestWorst = worst;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// The text color for a fill. A color (a hue that is clearly seen) always gets the light one, however bright it is; white and the grays get the
        /// dark or the light one, whichever has the higher WCAG contrast. Unlike <see cref="GetContrastingForeColor"/>
        /// this is the true ratio for those, not a brightness rule of thumb.
        /// </summary>
        internal static Color GetBestTextColor(Color background)
        {
            // A color keeps light text (see UsesDarkText); white and the grays get whichever reads better.
            if (IsColored(background))
                return UsesDarkText(background) ? DarkForeColor : LightForeColor;

            return GetContrastRatio(LightForeColor, background) >= GetContrastRatio(DarkForeColor, background)
                ? LightForeColor
                : DarkForeColor;
        }

        /// <summary>The WCAG 2.1 contrast ratio (1 to 21) between two colors.</summary>
        internal static double GetContrastRatio(Color first, Color second)
        {
            double a = GetWcagLuminance(first);
            double b = GetWcagLuminance(second);

            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double GetWcagLuminance(Color color)
        {
            return 0.2126 * ToLinear(color.R) + 0.7152 * ToLinear(color.G) + 0.0722 * ToLinear(color.B);
        }

        private static double ToLinear(int channel)
        {
            double value = channel / 255.0;

            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        // The brightness that IsLight goes by: the cheap perceived brightness (the Rec. 601 weights on the values as they are, not on the
        // light they give off). It is not the WCAG luminance of GetContrastRatio, and that is on purpose: the thresholds that tell a light surface from a
        // colored one (LightBackgroundThreshold, ColoredMaxLightness) were chosen against this scale, whereas the ratio measures how well two colors read.
        private static double RelativeLuminance(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        }

        /// <summary>
        /// Text of a disabled control on <paramref name="surface"/>: the best text color blended
        /// toward the surface, as on the buttons and the check box. The theme's
        /// <see cref="TextDisabled"/> is too faint on the light theme.
        /// </summary>
        internal static Color GetDisabledText(Color surface)
        {
            return BlendToward(GetContrastingForeColor(surface), surface, 0.35);
        }

        /// <summary>
        /// Quiet text on <paramref name="surface"/> (a count, a version): the best text color blended toward the surface by
        /// <paramref name="amount"/> (0-1) - but never so far that it falls below <paramref name="minimumContrast"/>, so it stays
        /// readable on every surface (a fixed blend is too faint on a light one or on a color).
        /// </summary>
        internal static Color GetMutedText(Color surface, double amount, double minimumContrast = 4.5)
        {
            Color text = GetBestTextColor(surface);
            Color muted = BlendToward(text, surface, amount);

            for (double reduced = amount; reduced > 0 && GetContrastRatio(muted, surface) < minimumContrast; )
            {
                reduced = Math.Max(0, reduced - 0.05);
                muted = BlendToward(text, surface, reduced);
            }

            return muted;
        }
    }
}
