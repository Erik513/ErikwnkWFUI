using System;
using System.Drawing;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// WCAG 2.1 contrast-ratio math (https://www.w3.org/TR/WCAG21/#contrast-minimum) -
/// shared across every control's own readability tests instead of each one
/// re-deriving the same relative-luminance formula. WHICH color pairs
/// actually need checking stays per control (each control's own colors
/// mean something different) - this only ever answers "given these two
/// colors, what's their contrast ratio", the same way ColumnLayoutMath only
/// ever answers pure layout math for whichever columns a caller hands it.
/// Deliberately its own thing, not a copy of UIColors.GetContrastingForeColor's
/// internal RelativeLuminance - that one is a cheap, unweighted heuristic
/// good enough for picking "dark or light text", not a real ratio; this is
/// the actual, gamma-corrected WCAG formula, precise enough to assert a
/// numeric pass/fail bar against.
/// </summary>
internal static class ColorContrastHelper
{
    // WCAG AA minimums - normal text needs the stricter ratio; large text
    // (≥18pt, or ≥14pt bold) and non-text UI components/graphics only need
    // the lower one. Callers pick whichever applies to what they're
    // actually checking.
    public const double MinimumRatioNormalText = 4.5;
    public const double MinimumRatioLargeTextOrUiComponents = 3.0;

    public static double GetContrastRatio(Color first, Color second)
    {
        double firstLuminance = GetRelativeLuminance(first);
        double secondLuminance = GetRelativeLuminance(second);
        double lighter = Math.Max(firstLuminance, secondLuminance);
        double darker = Math.Min(firstLuminance, secondLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    public static bool MeetsMinimumContrast(Color foreground, Color background, double minimumRatio)
    {
        return GetContrastRatio(foreground, background) >= minimumRatio;
    }

    private static double GetRelativeLuminance(Color color)
    {
        double red = ToLinear(color.R);
        double green = ToLinear(color.G);
        double blue = ToLinear(color.B);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private static double ToLinear(byte channel)
    {
        double normalized = channel / 255.0;

        return normalized <= 0.03928
            ? normalized / 12.92
            : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }
}
