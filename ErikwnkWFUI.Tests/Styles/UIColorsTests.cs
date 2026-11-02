using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// SetAccent mutates static state shared by the whole process - every test
/// here restores the real default (SetAccent(UIAccentColors.Blue), which
/// reproduces UIColors' own hardcoded defaults exactly) in a finally block,
/// so an assertion failure can't leave later tests reading a wrong accent.
/// [Collection] (see AccentColorTestCollection) keeps this from racing any
/// other class that reads an accent-derived color while this one changes it.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class UIColorsTests
{
    [Fact]
    public void SetAccent_NeutralGrayAccent_UsesAHigherSelectionAlpha()
    {
        try
        {
            UIColors.SetAccent(Color.FromArgb(90, 90, 90));

            // A hued Selection (the default, alpha 60) barely lifts a gray
            // row's own shade - a gray-on-gray selection needs more alpha
            // than that to actually read as selected. See ListView's own
            // SelectionOverlayColor default (also 130) for the same reasoning.
            Assert.Equal(130, UIColors.Selection.A);
            Assert.Equal(Color.FromArgb(130, 90, 90, 90), UIColors.Selection);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void SetAccent_HuedAccent_KeepsTheLowerSelectionAlpha()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Red);

            Assert.Equal(60, UIColors.Selection.A);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void SetAccent_NeutralAccent_DarkensGentlerThanAHuedOne()
    {
        try
        {
            // A near-white neutral accent - the extreme BlackOrWhite sits
            // at in the Dark theme - would otherwise get crushed by the
            // same percentage that reads fine as a moody dark blue.
            UIColors.SetAccent(Color.FromArgb(240, 240, 240));

            Assert.Equal(Color.FromArgb(204, 204, 204), UIColors.PrimaryDark);
            Assert.Equal(Color.FromArgb(168, 168, 168), UIColors.PrimaryDarkDark);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void SetAccent_HuedAccent_KeepsTheOriginalDarkenAmounts()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Blue);

            Assert.Equal(UIAccentColors.Blue, UIColors.Primary);
            // (int) truncation of a binary-imprecise 0.7/0.45 multiplier -
            // not simply 90*0.7=63 and 90*0.45=40.5 rounded.
            Assert.Equal(Color.FromArgb(0, 62, 110), UIColors.PrimaryDark);
            Assert.Equal(Color.FromArgb(0, 40, 71), UIColors.PrimaryDarkDark);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Theory]
    [InlineData(255, 255, 255, true)]
    [InlineData(240, 240, 240, true)]
    [InlineData(0, 0, 0, false)]
    [InlineData(40, 40, 40, false)]
    public void IsLight_SaysWhetherDarkTextBelongsOnTheColor(int r, int g, int b, bool expected)
    {
        Color color = Color.FromArgb(r, g, b);

        Assert.Equal(expected, UIColors.IsLight(color));
        Assert.Equal(expected, UIColors.GetContrastingForeColor(color) == UIColors.DarkForeColor);
    }

    [Fact]
    public void BlendToward_GoesThePartOfTheWay_AndKeepsTheAlpha()
    {
        Color from = Color.FromArgb(200, 100, 150, 200);
        Color target = Color.FromArgb(255, 200, 50, 0);

        Assert.Equal(from, UIColors.BlendToward(from, target, 0));
        Assert.Equal(Color.FromArgb(200, 200, 50, 0), UIColors.BlendToward(from, target, 1));
        Assert.Equal(Color.FromArgb(200, 135, 115, 130), UIColors.BlendToward(from, target, 0.35));
    }

    [Fact]
    public void BlendToward_ClampsTheAmount()
    {
        Color from = Color.FromArgb(10, 20, 30);
        Color target = Color.FromArgb(110, 120, 130);

        Assert.Equal(from, UIColors.BlendToward(from, target, -3));
        Assert.Equal(target, UIColors.BlendToward(from, target, 7));
    }

    [Fact]
    public void Darken_ByAShare_ScalesEveryChannel()
    {
        Assert.Equal(Color.FromArgb(65, 130, 162), UIColors.DarkenByShare(Color.FromArgb(100, 200, 250), 0.35));
        Assert.Equal(Color.FromArgb(0, 0, 0), UIColors.DarkenByShare(Color.FromArgb(100, 200, 250), 1.0));
    }

    [Fact]
    public void TheOverlays_AreBlackVeils_OfThreeStrengths_NotTransparent()
    {
        // (Once built as FromArgb(0, 0, 0, 180): alpha 0 and a blue, so the veil was not there at all.)
        Assert.Equal(Color.FromArgb(180, 0, 0, 0), UIColors.OverlayDark);
        Assert.Equal(Color.FromArgb(120, 0, 0, 0), UIColors.OverlayMedium);
        Assert.Equal(Color.FromArgb(60, 0, 0, 0), UIColors.OverlayLight);
        Assert.Equal(UIColors.OverlayDark, UIStyles.Colors.OverlayDark);
    }

    [Fact]
    public void ApplyTheme_WithNull_ChangesNothing_AndRaisesNoEvent()
    {
        Color before = UIColors.BackgroundDark;
        int raised = 0;
        EventHandler handler = (sender, e) => raised++;
        UIColors.ThemeChanged += handler;

        try
        {
            UIColors.ApplyTheme(null!);

            Assert.Equal(before, UIColors.BackgroundDark);
            Assert.Equal(0, raised);
        }
        finally
        {
            UIColors.ThemeChanged -= handler;
        }
    }

    [Fact]
    public void EnsureContrast_AgainstTwoColors_WhereNoShadeSatisfiesBoth_ReturnsTheBestCompromise()
    {
        // 15:1 against white wants nearly black, 15:1 against black wants nearly white: no gray has both.
        Color gray = Color.FromArgb(128, 128, 128);
        Color result = UIColors.EnsureContrast(gray, Color.White, 15, Color.Black, 15);

        double Worst(Color color) => Math.Min(UIColors.GetContrastRatio(color, Color.White) / 15, UIColors.GetContrastRatio(color, Color.Black) / 15);

        Assert.True(Worst(result) >= Worst(gray));
        Assert.True(Worst(result) < 1);
        Assert.Equal(result.R, result.G);
        Assert.Equal(result.G, result.B);
    }

    [Fact]
    public void IsNeutral_IsTrueForBlackWhiteAndEveryGray_FalseForAColor()
    {
        Assert.True(UIColors.IsNeutral(Color.Black));
        Assert.True(UIColors.IsNeutral(Color.White));
        Assert.True(UIColors.IsNeutral(Color.FromArgb(90, 90, 90)));
        Assert.False(UIColors.IsNeutral(Color.FromArgb(90, 90, 91)));
        Assert.False(UIColors.IsNeutral(UIAccentColors.Blue));
    }

    [Fact]
    public void GetReadableText_KeepsAColorThatReadsAlready_AndAlwaysReachesTheContrast_OnAnyPage()
    {
        Color text = UIColors.TextPrimary;
        Color unchanged = UIColors.GetReadableText(text, Color.FromArgb(30, 30, 30), 4.5);

        Assert.Equal(text, unchanged);

        // Every gray, from black to white, and a few colors: the mid-tones are the hard case (moving the light text one way reaches only about 3.9:1).
        List<Color> pages = Enumerable.Range(0, 52).Select(step => Color.FromArgb(step * 5, step * 5, step * 5)).ToList();
        pages.AddRange(new[] { Color.Yellow, Color.Red, Color.Navy, Color.Orange, Color.Teal });

        foreach (Color page in pages)
            Assert.True(UIColors.GetContrastRatio(UIColors.GetReadableText(text, page, 4.5), page) >= 4.5, "unread on " + page);
    }
}
