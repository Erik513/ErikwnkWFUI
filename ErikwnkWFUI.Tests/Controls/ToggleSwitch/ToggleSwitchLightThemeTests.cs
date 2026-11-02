using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>What is really painted (not the colors that are worked out): the track and the knob of every variant in both themes, with the mouse over the switch, and while it is pressed.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToggleSwitchLightThemeTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    public static IEnumerable<object[]> Variants()
    {
        foreach (bool light in new[] { false, true })
        {
            yield return new object[] { light, "Standard on", (Func<ToggleSwitch>)(() => UIStyles.ToggleSwitches.CreateStandard(true)) };
            yield return new object[] { light, "Standard off", (Func<ToggleSwitch>)(() => UIStyles.ToggleSwitches.CreateStandard(false)) };
            yield return new object[] { light, "Primary on", (Func<ToggleSwitch>)(() => UIStyles.ToggleSwitches.CreatePrimary(true)) };
            yield return new object[] { light, "Yellow on", (Func<ToggleSwitch>)(() => UIStyles.ToggleSwitches.CreateStandard(true, colors: UIStyles.Colors.Sets.Yellow)) };
            yield return new object[] { light, "Disabled on", (Func<ToggleSwitch>)(() => Disabled(true)) };
            yield return new object[] { light, "Disabled off", (Func<ToggleSwitch>)(() => Disabled(false)) };
        }
    }

    [Fact]
    public void TheOffTrack_IsADarkerThanThePageInTheLightTheme_AndLighterInTheDarkTheme()
    {
        foreach (bool light in new[] { false, true })
        {
            UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
            using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
            using Form form = Show(toggle, light);

            Color track = toggle.InvokePrivate<Color>("GetBackgroundColor");
            Color page = form.BackColor;

            if (light)
                Assert.True(track.GetBrightness() < page.GetBrightness(), $"{track} on {page}");
            else
                Assert.True(track.GetBrightness() > page.GetBrightness(), $"{track} on {page}");
        }
    }

    private static ToggleSwitch Disabled(bool isChecked)
    {
        ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(isChecked);
        toggle.Enabled = false;

        return toggle;
    }

    private static Form Show(ToggleSwitch toggle, bool light)
    {
        toggle.TabStop = false;
        return OffscreenForm.Show(toggle, new Size(60, 40), light ? Color.FromArgb(243, 243, 243) : Color.FromArgb(30, 30, 30), new Point(7, 7));
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void TheTrackAndTheKnob_ArePaintedInTheColorsThatAreWorkedOut(bool light, string name, Func<ToggleSwitch> make)
    {
        UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
        using ToggleSwitch toggle = make();
        using Form form = Show(toggle, light);
        using Bitmap picture = Draw(toggle);

        Color track = picture.GetPixel(toggle.Checked ? 9 : toggle.Width - 10, toggle.Height / 2);
        Color knob = picture.GetPixel(toggle.Checked ? toggle.Width - 14 : 13, toggle.Height / 2);

        Assert.Equal(toggle.InvokePrivate<Color>("GetBackgroundColor").ToArgb(), track.ToArgb());
        Assert.Equal(toggle.InvokePrivate<Color>("GetKnobColor").ToArgb(), knob.ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheHoveredAndThePressedSwitch_ChangeWhatIsPainted_AndGoBack(bool light)
    {
        UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle, light);
        using Bitmap resting = Draw(toggle);

        toggle.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        using Bitmap hovered = Draw(toggle);
        toggle.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 10, 0));
        using Bitmap pressed = Draw(toggle);
        toggle.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        using Bitmap left = Draw(toggle);

        Assert.False(SamePixels(resting, hovered), "no hover look");
        Assert.False(SamePixels(hovered, pressed), "no pressed look");
        Assert.True(SamePixels(resting, left), "the look does not come back after the mouse has left");
    }
}
