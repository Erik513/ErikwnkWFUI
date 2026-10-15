using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

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
        yield return new object[] { "CreateStandardBorderless", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateStandardBorderless()) };
        yield return new object[] { "CreateGreen", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateGreen()) };
        yield return new object[] { "CreateGreenBorderless", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateGreenBorderless()) };
        yield return new object[] { "CreatePrimary", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreatePrimary()) };
        yield return new object[] { "CreatePrimaryBorderless", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreatePrimaryBorderless()) };
        yield return new object[] { "CreateStatus", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateStatus()) };
        yield return new object[] { "CreateStatusBorderless", (Func<ProgressBar>)(() => UIStyles.ProgressBars.CreateStatusBorderless()) };
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void EveryFactoryMethod_MakesAThemedProgressBar_InTheContinuousStyle(string name, Func<ProgressBar> make)
    {
        using ProgressBar bar = make();

        Assert.IsType<ThemedProgressBar>(bar);
        Assert.Equal(ProgressBarStyle.Continuous, bar.Style);
        Assert.Equal(0, bar.Minimum);
    }

    [Fact]
    public void WithoutAColorSet_TheFillIsTheNeutralGray_AsItWas()
    {
        using ProgressBar standard = UIStyles.ProgressBars.CreateStandard();
        using ProgressBar borderless = UIStyles.ProgressBars.CreateStandardBorderless();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Equal(UIColors.DisabledGray, standard.ForeColor);
        Assert.Equal(UIColors.DisabledGray, borderless.ForeColor);
        Assert.Equal(UIColors.DisabledGray, slim.ForeColor);
    }

    [Fact]
    public void WithAColorSet_TheFillIsTheMainColorOfTheSet_ForTheBarAndTheSlimBar()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        using ProgressBar standard = UIStyles.ProgressBars.CreateStandard(teal);
        using ProgressBar borderless = UIStyles.ProgressBars.CreateStandardBorderless(teal);
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard(teal);

        Assert.Equal(teal.Hover, standard.ForeColor);
        Assert.Equal(teal.Hover, borderless.ForeColor);
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
        using ProgressBar greenBorderless = UIStyles.ProgressBars.CreateGreenBorderless();
        using ProgressBar primary = UIStyles.ProgressBars.CreatePrimary();
        using ProgressBar primaryBorderless = UIStyles.ProgressBars.CreatePrimaryBorderless();
        using SlimProgressBar slimGreen = UIStyles.SlimProgressBars.CreateGreen();
        using SlimProgressBar slimPrimary = UIStyles.SlimProgressBars.CreatePrimary();

        Assert.Equal(UIColors.Green, green.ForeColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Hover, green.ForeColor);
        Assert.Equal(green.ForeColor, greenBorderless.ForeColor);
        Assert.Equal(green.ForeColor, slimGreen.ForeColor);

        Assert.Equal(UIColors.Primary, primary.ForeColor);
        Assert.Equal(UIStyles.Colors.Sets.Primary.Hover, primary.ForeColor);
        Assert.Equal(primary.ForeColor, primaryBorderless.ForeColor);
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
}
