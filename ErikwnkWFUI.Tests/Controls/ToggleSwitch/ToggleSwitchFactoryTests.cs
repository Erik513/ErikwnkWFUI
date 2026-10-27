using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>What the factories of the switch make: gray, the accent, the colors of a set, the size, the state and the tooltips.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToggleSwitchFactoryTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Form Show(ToggleSwitch toggle)
    {
        Form form = OffscreenForm.Create(new Size(toggle.Width + 10, toggle.Height + 10), Color.FromArgb(20, 20, 20));
        toggle.Location = new Point(5, 5);
        form.Controls.Add(toggle);
        form.Show();
        Application.DoEvents();

        return form;
    }

    // The color of the track, a pixel inside it where the knob is not (the knob is at the other end).
    private static Color Track(ToggleSwitch toggle)
    {
        using Bitmap picture = Draw(toggle);
        int x = toggle.Checked ? 9 : toggle.Width - 10;

        return picture.GetPixel(x, toggle.Height / 2);
    }

    [Fact]
    public void TheFactories_MakeTheDefaultSwitch_OnAndInTheDefaultSize()
    {
        using ToggleSwitch standard = UIStyles.ToggleSwitches.CreateStandard();
        using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary();

        Assert.True(standard.Checked);
        Assert.True(primary.Checked);
        Assert.Equal(new Size(45, 25), standard.Size);
        Assert.Equal(new Size(45, 25), primary.Size);
        Assert.Null(standard.Colors);
    }

    [Fact]
    public void TheState_TheTooltips_AndTheSize_AreGiven()
    {
        using ToggleSwitch off = UIStyles.ToggleSwitches.CreateStandard(false, "Is on", "Is off", size: new Size(35, 20));
        using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary(false, "Is on", "Is off", new Size(55, 30));

        Assert.False(off.Checked);
        Assert.Equal("Is on", off.ToolTipTextChecked);
        Assert.Equal("Is off", off.ToolTipTextUnchecked);
        Assert.Equal(new Size(35, 20), off.Size);
        Assert.False(primary.Checked);
        Assert.Equal("Is on", primary.ToolTipTextChecked);
        Assert.Equal(new Size(55, 30), primary.Size);
    }

    [Fact]
    public void Standard_IsGrayWhileOn_Primary_FollowsTheAccent_AndASetColorsIt()
    {
        using ToggleSwitch standard = UIStyles.ToggleSwitches.CreateStandard();
        using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary();
        using ToggleSwitch yellow = UIStyles.ToggleSwitches.CreateStandard(colors: UIStyles.Colors.Sets.Yellow);
        using Form first = Show(standard);
        using Form second = Show(primary);
        using Form third = Show(yellow);

        Color gray = Track(standard);
        Color accent = Track(primary);
        Color yellowTrack = Track(yellow);

        // Gray: the three channels are (nearly) the same; the accent and the yellow are not gray.
        Assert.InRange(Math.Max(gray.R, Math.Max(gray.G, gray.B)) - Math.Min(gray.R, Math.Min(gray.G, gray.B)), 0, 12);
        Assert.True(Math.Max(accent.R, Math.Max(accent.G, accent.B)) - Math.Min(accent.R, Math.Min(accent.G, accent.B)) > 40, "the accent is gray");
        Assert.True(yellowTrack.R > yellowTrack.B + 40, "the set is not yellow");
        Assert.NotEqual(gray.ToArgb(), accent.ToArgb());
        Assert.NotEqual(accent.ToArgb(), yellowTrack.ToArgb());
    }

    [Fact]
    public void ASingleColor_IsTakenAsTheMainColorOfASet()
    {
        using ToggleSwitch purple = UIStyles.ToggleSwitches.CreateStandard(colors: Color.MediumPurple);
        using Form form = Show(purple);

        Color track = Track(purple);

        Assert.True(track.B > track.G + 30, "the track is not purple");
    }

    [Fact]
    public void Primary_FollowsAChangeOfTheAccent_WhileItIsShown()
    {
        using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary();
        using Form form = Show(primary);
        Color before = Track(primary);

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Color after = Track(primary);
        Assert.NotEqual(before.ToArgb(), after.ToArgb());
        Assert.True(after.R > after.B + 40, "the track did not take the new accent");
    }

    [Fact]
    public void TheColors_CanBeChangedLater_AndReplaceWhatThePrimaryFollowed()
    {
        using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary();
        using Form form = Show(primary);
        Color accent = Track(primary);

        primary.Colors = UIStyles.Colors.Sets.Green;
        Color green = Track(primary);

        Assert.NotEqual(accent.ToArgb(), green.ToArgb());
        Assert.True(green.G > green.R + 30, "the track is not green");
    }

    [Fact]
    public void ADisabledSwitch_LooksTheSame_WhateverTheColors()
    {
        using ToggleSwitch gray = UIStyles.ToggleSwitches.CreateStandard();
        using ToggleSwitch red = UIStyles.ToggleSwitches.CreateStandard(colors: UIStyles.Colors.Sets.Red);
        gray.Enabled = false;
        red.Enabled = false;
        using Form first = Show(gray);
        using Form second = Show(red);

        Assert.Equal(Track(gray).ToArgb(), Track(red).ToArgb());
    }
}
