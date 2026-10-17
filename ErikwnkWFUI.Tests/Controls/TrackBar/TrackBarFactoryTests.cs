using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>What the factories of the track bar and the volume slider make: the value, the range, and the colors (a set, the accent, the neutral gray). [Collection]: the primary set reads the live accent.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TrackBarFactoryTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    // What the fill of a slider is for a color: moved just far enough to stand out from the surface and the track (2.5:1).
    private static Color Stood(TrackBar bar, Color raw)
    {
        Color surface = ErikwnkWFUI.Helpers.SurfaceColor.Of(bar);

        return UIColors.EnsureContrast(raw, surface, 2.5, bar.GetTrackColor(), 1.5);
    }

    [Fact]
    public void TheFactoryMethods_SetTheValueAndTheRange()
    {
        using TrackBar standard = UIStyles.TrackBars.CreateStandard();
        Assert.Equal(0, standard.Value);
        Assert.Equal(1.0, standard.Maximum);

        using TrackBar primary = UIStyles.TrackBars.CreatePrimary(30, 100);
        Assert.Equal(30, primary.Value);
        Assert.Equal(100, primary.Maximum);

        using TrackBar colored = UIStyles.TrackBars.CreateStandard(0.5, 2.0, UIStyles.Colors.Sets.Teal);
        Assert.Equal(0.5, colored.Value);
        Assert.Equal(2.0, colored.Maximum);

        using TrackBar volume = UIStyles.TrackBars.CreateStandard(0.4, showValuePopup: true, formatValue: TrackBar.FormatPercent);
        Assert.Equal(0.4, volume.Value);
        Assert.Equal(1.0, volume.Maximum);
        Assert.True(volume.ShowValuePopup);
        Assert.Equal((0.4).ToString("P0", UIStrings.NumberCulture), volume.FormatValue(volume.Value));

        Assert.False(standard.ShowValuePopup);
        Assert.False(primary.ShowValuePopup);

        using TrackBar primaryVolume = UIStyles.TrackBars.CreatePrimary(0.4, showValuePopup: true, formatValue: TrackBar.FormatPercent);
        Assert.True(primaryVolume.ShowValuePopup);
        Assert.Equal((0.4).ToString("P0", UIStrings.NumberCulture), primaryVolume.FormatValue(primaryVolume.Value));
    }

    [Fact]
    public void WithoutAColorSet_TheFillIsTheNeutralGray_AsItWas()
    {
        using TrackBar standard = UIStyles.TrackBars.CreateStandard(0.5);

        Assert.Equal(Stood(standard, UIColors.DisabledGray), standard.GetFillColor());
        Assert.Null(standard.Colors);
    }

    [Fact]
    public void WithAColorSet_TheFillIsTheMainColorOfTheSet()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        using TrackBar standard = UIStyles.TrackBars.CreateStandard(0.5, 1.0, teal);
        using TrackBar plain = UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.CreateSet(Color.FromArgb(210, 110, 20)));

        Assert.Equal(Stood(standard, teal.Hover), standard.GetFillColor());
        Assert.Same(teal, standard.Colors);
        Assert.Equal(Stood(plain, Color.FromArgb(210, 110, 20)), plain.GetFillColor());
    }

    [Fact]
    public void Primary_FollowsTheAccent()
    {
        using TrackBar primary = UIStyles.TrackBars.CreatePrimary(0.5);

        Assert.Equal(Stood(primary, UIColors.PrimarySet.Hover), primary.GetFillColor());

        Color before = primary.GetFillColor();
        UIColors.SetAccent(Color.OrangeRed);

        Assert.NotEqual(before, primary.GetFillColor());
        Assert.Equal(Stood(primary, UIColors.PrimarySet.Hover), primary.GetFillColor());
    }

    [Fact]
    public void TheColorsSetOneAfterTheOther_EachReplacesTheLast()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.5);

        Assert.Equal(Stood(bar, UIColors.PrimarySet.Hover), bar.GetFillColor());

        bar.Colors = teal;
        UIColors.SetAccent(Color.OrangeRed);
        Assert.Equal(Stood(bar, teal.Hover), bar.GetFillColor());

        bar.Colors = null;
        Assert.Equal(Stood(bar, UIColors.DisabledGray), bar.GetFillColor());

        bar.FollowColors(() => UIColors.PrimarySet);
        Assert.Equal(Stood(bar, UIColors.PrimarySet.Hover), bar.GetFillColor());

    }

    [Fact]
    public void ADisabledTrackBar_HasTheDisabledColor_WhateverTheSet()
    {
        using TrackBar bar = UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Teal);
        bar.Enabled = false;

        Assert.Equal(Stood(bar, UIColors.GetDisabledText(ErikwnkWFUI.Helpers.SurfaceColor.Of(bar))), bar.GetFillColor());
    }

    [Fact]
    public void ThePaintedFillAndThumb_AreTheColorOfTheSet()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using TrackBar bar = UIStyles.TrackBars.CreateStandard(0.5, 1.0, teal);
        bar.SetBounds(0, 0, 100, 20);
        using Form host = OffscreenForm.Create(new Size(100, 20), Color.Black);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();
        using Bitmap picture = Draw(bar);

        // The thumb in the middle, and the filled part of the track left of it.
        Color fill = bar.GetFillColor();
        Assert.Equal(fill.ToArgb(), picture.GetPixel(50, 10).ToArgb());
        Assert.Equal(fill.ToArgb(), picture.GetPixel(25, 10).ToArgb());
        Assert.NotEqual(fill.ToArgb(), picture.GetPixel(80, 10).ToArgb());
        Assert.Equal(bar.GetTrackColor().ToArgb(), picture.GetPixel(80, 10).ToArgb());
    }
}
