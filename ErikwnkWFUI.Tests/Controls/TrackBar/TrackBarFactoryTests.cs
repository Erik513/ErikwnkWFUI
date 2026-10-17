using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
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

        using VolumeSlider volume = UIStyles.VolumeSliders.CreateStandard(0.4);
        Assert.Equal(0.4, volume.Value);
        Assert.Equal(1.0, volume.Maximum);
    }

    [Fact]
    public void WithoutAColorSet_TheFillIsTheNeutralGray_AsItWas()
    {
        using TrackBar standard = UIStyles.TrackBars.CreateStandard(0.5);
        using VolumeSlider volume = UIStyles.VolumeSliders.CreateStandard(0.5);

        Assert.Equal(UIColors.DisabledGray, standard.GetFillColor());
        Assert.Equal(UIColors.DisabledGray, volume.GetFillColor());
        Assert.Null(standard.Colors);
    }

    [Fact]
    public void WithAColorSet_TheFillIsTheMainColorOfTheSet()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        using TrackBar standard = UIStyles.TrackBars.CreateStandard(0.5, 1.0, teal);
        using VolumeSlider volume = UIStyles.VolumeSliders.CreateStandard(0.5, teal);
        using TrackBar plain = UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.CreateSet(Color.FromArgb(210, 110, 20)));

        Assert.Equal(teal.Hover, standard.GetFillColor());
        Assert.Equal(teal.Hover, volume.GetFillColor());
        Assert.Same(teal, standard.Colors);
        Assert.Equal(Color.FromArgb(210, 110, 20), plain.GetFillColor());
    }

    [Fact]
    public void Primary_FollowsTheAccent_ForBoth()
    {
        using TrackBar primary = UIStyles.TrackBars.CreatePrimary(0.5);
        using VolumeSlider volume = UIStyles.VolumeSliders.CreatePrimary(0.5);

        Assert.Equal(UIColors.PrimarySet.Hover, primary.GetFillColor());
        Assert.Equal(UIColors.PrimarySet.Hover, volume.GetFillColor());

        Color before = primary.GetFillColor();
        UIColors.SetAccent(Color.OrangeRed);

        Assert.NotEqual(before, primary.GetFillColor());
        Assert.Equal(UIColors.PrimarySet.Hover, primary.GetFillColor());
        Assert.Equal(UIColors.PrimarySet.Hover, volume.GetFillColor());
    }

    [Fact]
    public void TheColorsSetOneAfterTheOther_EachReplacesTheLast_AndAColorByHandWinsOverAll()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.5);

        Assert.Equal(UIColors.PrimarySet.Hover, bar.GetFillColor());

        bar.Colors = teal;
        UIColors.SetAccent(Color.OrangeRed);
        Assert.Equal(teal.Hover, bar.GetFillColor());

        bar.Colors = null;
        Assert.Equal(UIColors.DisabledGray, bar.GetFillColor());

        bar.FollowColors(() => UIColors.PrimarySet);
        Assert.Equal(UIColors.PrimarySet.Hover, bar.GetFillColor());

        bar.FillColor = Color.Purple;
        Assert.Equal(Color.Purple, bar.GetFillColor());

        bar.FillColor = null;
        Assert.Equal(UIColors.PrimarySet.Hover, bar.GetFillColor());
    }

    [Fact]
    public void ADisabledTrackBar_HasTheDisabledColor_WhateverTheSet()
    {
        using TrackBar bar = UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Teal);
        bar.FillColor = Color.Purple;
        bar.Enabled = false;

        Assert.Equal(UIStyles.Colors.TextDisabled, bar.GetFillColor());
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
        Assert.Equal(teal.Hover.ToArgb(), picture.GetPixel(50, 10).ToArgb());
        Assert.Equal(teal.Hover.ToArgb(), picture.GetPixel(25, 10).ToArgb());
        Assert.NotEqual(teal.Hover.ToArgb(), picture.GetPixel(80, 10).ToArgb());
    }
}
