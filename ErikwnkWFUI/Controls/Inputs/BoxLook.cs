using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The colors of the box of a check box and of a radio button for each state - the same for both, so that the two look alike side by side: the fill of
    /// a selected box with the mark on it, and the border.
    /// </summary>
    internal static class BoxLook
    {
        // The border of a box that is hovered or pressed and still empty is brought to this contrast against the page, so the state is seen as a stronger outline.
        private const double HoveredBorderContrast = 4.5;
        private const double PressedBorderContrast = 6.0;

        // The least contrast the mark of a set has on its fill before another mark is taken.
        private const double MarkContrast = 3.0;

        /// <summary>
        /// The fill of a selected box and the mark on it for the current state. Disabled is a fixed gray (not the theme's TextDisabled) so a disabled box looks
        /// the same in both themes, like a disabled button or switch. Otherwise the fill and the mark come from the color set for rest, hovered and pressed;
        /// on a light page the set's own fill (dark, made for the dark theme) hardly looks colored - a blue box looks almost gray - so the box takes the next
        /// brighter shade of the set, where the mark of the set stays readable on the brighter rest shade (a shade it has no readable mark for gets the dark or
        /// light mark that reads best).
        /// </summary>
        public static void GetFill(UIColorSet colors, Color page, bool enabled, bool isHot, bool isPressed, out Color fill, out Color mark)
        {
            if (!enabled)
            {
                fill = UIColors.DisabledGray;
                mark = UIColors.GetContrastingForeColor(UIColors.DisabledGray);

                return;
            }

            bool brighter = UIColors.IsLight(page) && UIColors.GetContrastRatio(colors.HoverText, colors.Hover) >= MarkContrast;

            if (isPressed)
            {
                fill = brighter ? colors.Basic : colors.Pressed;
                mark = brighter ? colors.BasicText : colors.PressedText;
            }
            else if (isHot)
            {
                fill = brighter ? colors.Pressed : colors.Hover;
                mark = brighter ? ReadableMark(colors.PressedText, colors.Pressed) : colors.HoverText;
            }
            else
            {
                fill = brighter ? colors.Hover : colors.Basic;
                mark = brighter ? colors.HoverText : colors.BasicText;
            }
        }

        /// <summary>
        /// The border of the box in the current state: the outline at rest, the fixed gray when disabled, and for an empty box that is hovered or pressed the
        /// color of the set - brought to 4.5:1 (hovered) or 6:1 (pressed) against the page, so the state is seen as a stronger outline in both themes (the
        /// gray set alone would be fainter than the outline in the dark theme).
        /// </summary>
        public static Color GetBorderColor(UIColorSet colors, Color page, bool enabled, bool isHot, bool isPressed, bool isFilled)
        {
            if (!enabled)
                return UIColors.DisabledGray;

            if (!(isHot || isPressed) || isFilled)
                return UIColors.GetOutlineColor(page);

            return UIColors.EnsureContrast(
                isPressed ? colors.Pressed : colors.Hover,
                page,
                isPressed ? PressedBorderContrast : HoveredBorderContrast);
        }

        // The mark of the set when it is readable on the fill, else the dark or the light mark that has the higher contrast on it.
        private static Color ReadableMark(Color setMark, Color fill)
        {
            if (UIColors.GetContrastRatio(setMark, fill) >= MarkContrast)
                return setMark;

            Color dark = Color.FromArgb(20, 20, 20);
            Color light = Color.FromArgb(240, 240, 240);

            return UIColors.GetContrastRatio(dark, fill) >= UIColors.GetContrastRatio(light, fill) ? dark : light;
        }
    }
}
