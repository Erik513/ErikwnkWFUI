using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>The colors of the volume control that are not those of the slider (that has its own tests): the percentage beside it, in both themes.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VolumeControlColorTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ThePercentage_CanBeReadOnTheSurface_InEveryVariant(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5, set, showPercentage: true);
                using Form host = OffscreenForm.Create(new Size(200, 40), lightTheme ? Color.FromArgb(243, 243, 243) : UIColors.BackgroundDark);
                host.Controls.Add(control);
                host.Show();
                control.PerformLayout();

                Label percent = control.GetPrivateField<Label>("_percent")!;
                Check(shortfalls, "percentage, " + name, percent.ForeColor, SurfaceColor.Of(percent), 4.5);
            }

            using VolumeControl primary = UIStyles.VolumeControls.CreatePrimary(0.5, showPercentage: true);
            using Form primaryHost = OffscreenForm.Create(new Size(200, 40), lightTheme ? Color.FromArgb(243, 243, 243) : UIColors.BackgroundDark);
            primaryHost.Controls.Add(primary);
            primaryHost.Show();
            primary.PerformLayout();

            Label label = primary.GetPrivateField<Label>("_percent")!;
            Check(shortfalls, "percentage, primary", label.ForeColor, SurfaceColor.Of(label), 4.5);
        });
    }

    [Fact]
    public void TheMuteButton_IsNeutral_WhateverTheColorsOfTheSlider()
    {
        using VolumeControl gray = UIStyles.VolumeControls.CreateStandard(0.5);
        using VolumeControl yellow = UIStyles.VolumeControls.CreateStandard(0.5, UIStyles.Colors.Sets.Yellow);
        using VolumeControl primary = UIStyles.VolumeControls.CreatePrimary(0.5);

        Assert.Equal(gray.MuteButton.BackColor, yellow.MuteButton.BackColor);
        Assert.Equal(gray.MuteButton.ForeColor, yellow.MuteButton.ForeColor);
        Assert.Equal(gray.MuteButton.BackColor, primary.MuteButton.BackColor);
        Assert.Equal(gray.MuteButton.ForeColor, primary.MuteButton.ForeColor);
    }
}
