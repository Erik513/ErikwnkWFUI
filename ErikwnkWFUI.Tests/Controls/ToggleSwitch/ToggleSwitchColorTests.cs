using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>The colors of the switch in both themes: the track of an enabled one is seen on the surface and the white knob on it, a disabled one is quiet in both states and in both themes, quieter than an enabled one.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToggleSwitchColorTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Color SurfaceOf(bool lightTheme) => lightTheme ? Color.FromArgb(243, 243, 243) : Color.FromArgb(30, 30, 30);

    // The color of the track (a pixel inside it where the knob is not) and of the knob, painted on the surface of the theme.
    private static (Color Track, Color Knob) Colors(ToggleSwitch toggle, bool lightTheme)
    {
        using Form form = OffscreenForm.Create(new Size(toggle.Width + 10, toggle.Height + 10), SurfaceOf(lightTheme));
        toggle.Location = new Point(5, 5);
        form.Controls.Add(toggle);
        form.Show();
        Application.DoEvents();
        using Bitmap picture = Draw(toggle);

        Color track = picture.GetPixel(toggle.Checked ? 9 : toggle.Width - 10, toggle.Height / 2);
        Color knob = picture.GetPixel(toggle.Checked ? toggle.Width - 14 : 13, toggle.Height / 2);
        form.Controls.Remove(toggle);

        return (track, knob);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTrackWhileOn_IsSeenOnTheSurface_AndTheKnobOnTheTrack_InEveryVariant(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            Color surface = SurfaceOf(lightTheme);

            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(true, colors: set);
                (Color track, Color knob) = Colors(toggle, lightTheme);
                Check(shortfalls, "track, " + name, track, surface, 1.9);
                Check(shortfalls, "knob on the track, " + name, knob, track, 1.4);
            }

            using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary(true);
            (Color primaryTrack, Color primaryKnob) = Colors(primary, lightTheme);
            Check(shortfalls, "track, primary", primaryTrack, surface, 1.9);
            Check(shortfalls, "knob on the track, primary", primaryKnob, primaryTrack, 1.4);
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ADisabledSwitch_IsQuieterThanAnEnabledOne_AndStillSeen_InBothStates(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            Color surface = SurfaceOf(lightTheme);

            using ToggleSwitch enabledOn = UIStyles.ToggleSwitches.CreateStandard(true);
            using ToggleSwitch disabledOn = UIStyles.ToggleSwitches.CreateStandard(true);
            using ToggleSwitch disabledOff = UIStyles.ToggleSwitches.CreateStandard(false);
            disabledOn.Enabled = false;
            disabledOff.Enabled = false;

            Color enabledTrack = Colors(enabledOn, lightTheme).Track;
            (Color onTrack, Color onKnob) = Colors(disabledOn, lightTheme);
            Color offTrack = Colors(disabledOff, lightTheme).Track;

            double enabledContrast = ColorContrastHelper.GetContrastRatio(enabledTrack, surface);
            double onContrast = ColorContrastHelper.GetContrastRatio(onTrack, surface);
            double offContrast = ColorContrastHelper.GetContrastRatio(offTrack, surface);

            Check(shortfalls, "disabled, on", onTrack, surface, 1.4);
            Check(shortfalls, "disabled, off", offTrack, surface, 1.2);

            if (onContrast >= enabledContrast)
                shortfalls.Add($"a disabled switch (on, {onContrast:F2}:1) is not quieter than an enabled one ({enabledContrast:F2}:1)");

            if (offContrast >= onContrast)
                shortfalls.Add($"the track of a disabled switch that is off ({offContrast:F2}:1) is not quieter than when it is on ({onContrast:F2}:1)");

            if (ColorContrastHelper.GetContrastRatio(onKnob, onTrack) < 1.3)
                shortfalls.Add("the knob of a disabled switch is lost on its track");
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ADisabledSwitch_LooksTheSame_WhateverItsColors(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            using ToggleSwitch gray = UIStyles.ToggleSwitches.CreateStandard(true);
            using ToggleSwitch red = UIStyles.ToggleSwitches.CreateStandard(true, colors: UIStyles.Colors.Sets.Red);
            using ToggleSwitch primary = UIStyles.ToggleSwitches.CreatePrimary(true);
            gray.Enabled = false;
            red.Enabled = false;
            primary.Enabled = false;

            Color expected = Colors(gray, lightTheme).Track;

            if (Colors(red, lightTheme).Track.ToArgb() != expected.ToArgb())
                shortfalls.Add("a red disabled switch looks different");

            if (Colors(primary, lightTheme).Track.ToArgb() != expected.ToArgb())
                shortfalls.Add("a primary disabled switch looks different");
        });
    }
}
