using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = System.Windows.Forms.ProgressBar;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>What the factories of the progress bars make.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarFactoryTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    public static IEnumerable<object[]> Bars()
    {
        yield return new object[] { "CreateStandard", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateStandard()) };
        yield return new object[] { "CreateGreen", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateGreen()) };
        yield return new object[] { "CreatePrimary", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreatePrimary()) };
        yield return new object[] { "CreateStatus", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateStatus()) };
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void EveryFactoryMethod_MakesAThemedProgressBar_InTheContinuousStyle(string name, Func<ProgressBar> make)
    {
        using ProgressBar bar = make();

        Assert.IsType<ErikwnkWFUI.Controls.ProgressBar>(bar);
        Assert.Equal(ProgressBarStyle.Continuous, bar.Style);
        Assert.Equal(0, bar.Minimum);
    }

    [Fact]
    public void WithoutAColorSet_TheFillIsTheNeutralGray_AsItWas()
    {
        using ProgressBar standard = UIStyles.ProgressBars.CreateStandard();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Equal(UIColors.DisabledGray, standard.ForeColor);
        Assert.Equal(UIColors.DisabledGray, slim.ForeColor);
    }

    [Fact]
    public void WithAColorSet_TheFillIsTheMainColorOfTheSet_ForTheBarAndTheSlimBar()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        using ProgressBar standard = UIStyles.ProgressBars.CreateStandard(teal);
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard(teal);

        Assert.Equal(teal.Hover, standard.ForeColor);
        Assert.Equal(teal.Hover, slim.ForeColor);

        // A plain color is a set too: the bar has that color.
        using ProgressBar orange = UIStyles.ProgressBars.CreateStandard(Color.FromArgb(210, 110, 20));
        Assert.Equal(UIStyles.Colors.CreateSet(Color.FromArgb(210, 110, 20)).Hover, orange.ForeColor);
        Assert.Equal(Color.FromArgb(210, 110, 20), orange.ForeColor);
    }

    [Fact]
    public void ASetLooksTheSame_InTheDarkAndInTheLightTheme()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;
        using ProgressBar inDark = UIStyles.ProgressBars.CreateStandard(green);

        UIColors.ApplyTheme(UIThemes.Light);
        using ProgressBar inLight = UIStyles.ProgressBars.CreateStandard(green);

        Assert.Equal(inDark.ForeColor, inLight.ForeColor);
    }

    [Fact]
    public void Green_IsTheStandardBarInTheGreenSet_AndPrimaryIsTheOneInTheAccentSet()
    {
        using ProgressBar green = UIStyles.ProgressBars.CreateGreen();
        using ProgressBar primary = UIStyles.ProgressBars.CreatePrimary();
        using SlimProgressBar slimGreen = UIStyles.SlimProgressBars.CreateGreen();
        using SlimProgressBar slimPrimary = UIStyles.SlimProgressBars.CreatePrimary();

        Assert.Equal(UIColors.Green, green.ForeColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Hover, green.ForeColor);
        Assert.Equal(green.ForeColor, slimGreen.ForeColor);

        Assert.Equal(UIColors.Primary, primary.ForeColor);
        Assert.Equal(UIStyles.Colors.Sets.Primary.Hover, primary.ForeColor);
        Assert.Equal(primary.ForeColor, slimPrimary.ForeColor);
    }

    [Fact]
    public void ADisabledBarIsGray_AndTheColorOfTheSetIsBackWhenItIsEnabled()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using ProgressBar bar = UIStyles.ProgressBars.CreateStandard(teal);

        bar.Enabled = false;
        Assert.Equal(UIColors.DisabledGray, bar.ForeColor);

        bar.Enabled = true;
        Assert.Equal(teal.Hover, bar.ForeColor);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void TheBorderIsOnByDefault_AndTheEdgeIsTheBorderColor_OrTheTrackWithoutIt(string name, Func<ProgressBar> make)
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)make();
        bar.BackColor = Color.Purple;

        Assert.Equal(BorderStyle.FixedSingle, bar.BorderStyle);
        Assert.Equal(UIColors.BorderMedium, bar.EdgeColor);

        bar.BorderStyle = BorderStyle.None;

        Assert.Equal(BorderStyle.None, bar.BorderStyle);
        Assert.Equal(Color.Purple, bar.EdgeColor);

        bar.BorderStyle = BorderStyle.Fixed3D;

        Assert.Equal(UIColors.BorderMedium, bar.EdgeColor);

        bar.Dispose();
    }

    [Fact]
    public void ChangingTheBorder_PaintsTheBarAgain_ButNotWhenItStaysAsItWas()
    {
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();
        int invalidated = 0;
        bar.Invalidated += (sender, e) => invalidated++;

        bar.BorderStyle = BorderStyle.FixedSingle;
        Assert.Equal(0, invalidated);

        bar.BorderStyle = BorderStyle.None;
        Assert.True(invalidated > 0);
    }

    [Fact]
    public void TheBorderAndTheTrackAreTwoThings_TheBorderDoesNotChangeTheTrack()
    {
        using Panel panel = new Panel { BackColor = Color.Purple };
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        panel.Controls.Add(bar);

        Assert.Equal(UIColors.BackgroundMedium, bar.BackColor);

        bar.BorderStyle = BorderStyle.None;
        Assert.Equal(UIColors.BackgroundMedium, bar.BackColor);
        Assert.Equal(UIColors.BackgroundMedium, bar.EdgeColor);

        bar.BorderStyle = BorderStyle.FixedSingle;
        Assert.Equal(UIColors.BackgroundMedium, bar.BackColor);
    }

    [Fact]
    public void ATransparentTrack_IsTheColorOfThePanel_WithOrWithoutABorder_AlsoWhenThePanelChanges()
    {
        using Panel panel = new Panel { BackColor = Color.Purple };
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        panel.Controls.Add(bar);

        bar.BackColor = Color.Transparent;
        Assert.Equal(Color.Purple, bar.BackColor);
        Assert.Equal(UIColors.BorderMedium, bar.EdgeColor);

        bar.BorderStyle = BorderStyle.None;
        Assert.Equal(Color.Purple, bar.BackColor);
        Assert.Equal(Color.Purple, bar.EdgeColor);

        panel.BackColor = Color.Orange;
        Assert.Equal(Color.Orange, bar.BackColor);

        bar.BackColor = Color.Empty;
        Assert.Equal(UIColors.BackgroundMedium, bar.BackColor);
    }

    [Fact]
    public void ATransparentTrack_FollowsThePanelThroughAChangeOfTheTheme_TheOtherOneTheTheme()
    {
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(200, 100) };
        Panel panel = UIStyles.Panels.CreateElevated();
        ErikwnkWFUI.Controls.ProgressBar transparent = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        transparent.BackColor = Color.Transparent;
        panel.Controls.Add(transparent);

        // (On a panel whose color does not change with the theme: only the theme itself can tell the bar.)
        ErikwnkWFUI.Controls.ProgressBar recessed = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        Panel fixedPanel = new Panel { BackColor = Color.Purple, Top = 50 };
        fixedPanel.Controls.Add(recessed);
        host.Controls.Add(panel);
        host.Controls.Add(fixedPanel);
        host.Show();
        Application.DoEvents();

        Assert.Equal(panel.BackColor, transparent.BackColor);
        Color darkTrack = recessed.BackColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(UIColors.BackgroundMediumElevated, panel.BackColor);
        Assert.Equal(panel.BackColor, transparent.BackColor);
        Assert.Equal(UIColors.BackgroundMedium, recessed.BackColor);
        Assert.NotEqual(darkTrack, recessed.BackColor);
    }

    [Fact]
    public void ATrackColorSetByHand_Stays_AlsoWhenTheBorderChanges()
    {
        using Panel panel = new Panel { BackColor = Color.Purple };
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        panel.Controls.Add(bar);

        bar.BackColor = Color.Teal;
        bar.BorderStyle = BorderStyle.None;
        Assert.Equal(Color.Teal, bar.BackColor);

        bar.BorderStyle = BorderStyle.FixedSingle;
        Assert.Equal(Color.Teal, bar.BackColor);
    }

    [Fact]
    public void ABarWithoutAPanel_CanBeSetToTransparentWithoutAnError_AndLooksRecessed()
    {
        using ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();

        Assert.Null(Record.Exception(() => bar.BackColor = Color.Transparent));
        Assert.Equal(UIColors.BackgroundMedium, bar.BackColor);
    }
}
