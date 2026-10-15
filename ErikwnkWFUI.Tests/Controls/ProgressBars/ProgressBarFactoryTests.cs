using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>What the factories of the progress bars make.</summary>
public class ProgressBarFactoryTests
{
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
}
