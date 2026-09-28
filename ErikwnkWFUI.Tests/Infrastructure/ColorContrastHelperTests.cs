using System.Drawing;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// ColorContrastHelper's own correctness, checked against well-known WCAG
/// reference values rather than just re-deriving the same formula here -
/// #767676 on white is the commonly-cited "just barely passes AA" gray
/// (~4.54:1), and black-on-white is the theoretical maximum (21:1).
/// </summary>
public class ColorContrastHelperTests
{
    [Fact]
    public void GetContrastRatio_BlackOnWhite_IsTheMaximumPossibleRatio()
    {
        double ratio = ColorContrastHelper.GetContrastRatio(Color.Black, Color.White);

        Assert.Equal(21.0, ratio, precision: 1);
    }

    [Fact]
    public void GetContrastRatio_SameColorTwice_IsOne()
    {
        double ratio = ColorContrastHelper.GetContrastRatio(Color.Gray, Color.Gray);

        Assert.Equal(1.0, ratio, precision: 5);
    }

    [Fact]
    public void GetContrastRatio_IsSymmetric_RegardlessOfArgumentOrder()
    {
        double ratio1 = ColorContrastHelper.GetContrastRatio(Color.FromArgb(20, 20, 20), Color.FromArgb(240, 240, 240));
        double ratio2 = ColorContrastHelper.GetContrastRatio(Color.FromArgb(240, 240, 240), Color.FromArgb(20, 20, 20));

        Assert.Equal(ratio1, ratio2, precision: 10);
    }

    [Fact]
    public void GetContrastRatio_KnownWcagReferenceGray_MatchesThePubliclyCitedRatio()
    {
        // #767676 on white - the standard textbook example of a gray that
        // just barely clears the 4.5:1 AA bar for normal text.
        double ratio = ColorContrastHelper.GetContrastRatio(Color.FromArgb(0x76, 0x76, 0x76), Color.White);

        Assert.Equal(4.54, ratio, precision: 2);
    }

    [Fact]
    public void MeetsMinimumContrast_RatioAboveThreshold_ReturnsTrue()
    {
        bool meets = ColorContrastHelper.MeetsMinimumContrast(Color.Black, Color.White, ColorContrastHelper.MinimumRatioNormalText);

        Assert.True(meets);
    }

    [Fact]
    public void MeetsMinimumContrast_RatioBelowThreshold_ReturnsFalse()
    {
        // Mid-gray on mid-gray - barely any contrast at all.
        bool meets = ColorContrastHelper.MeetsMinimumContrast(
            Color.FromArgb(130, 130, 130), Color.FromArgb(120, 120, 120), ColorContrastHelper.MinimumRatioNormalText);

        Assert.False(meets);
    }

    [Fact]
    public void Composite_FullyOpaqueForeground_ReturnsForegroundUnchanged()
    {
        Color result = ColorContrastHelper.Composite(Color.FromArgb(255, 10, 20, 30), Color.White);

        Assert.Equal(Color.FromArgb(10, 20, 30), result);
    }

    [Fact]
    public void Composite_FullyTransparentForeground_ReturnsBackgroundUnchanged()
    {
        Color result = ColorContrastHelper.Composite(Color.FromArgb(0, 10, 20, 30), Color.FromArgb(200, 150, 100));

        Assert.Equal(Color.FromArgb(200, 150, 100), result);
    }

    [Fact]
    public void Composite_HalfAlphaBlack_OverWhite_IsMidGray()
    {
        Color result = ColorContrastHelper.Composite(Color.FromArgb(128, 0, 0, 0), Color.White);

        Assert.InRange(result.R, 125, 130);
        Assert.InRange(result.G, 125, 130);
        Assert.InRange(result.B, 125, 130);
    }
}
