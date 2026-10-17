using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>The odd cases: tiny and stretched sizes, a handle that is made again, a spinner disposed while it turns, the colors set one after the other.</summary>
[Collection(AccentColorTestCollection.Name)]
public class SpinnerEdgeCaseTests : IDisposable
{
    private readonly Func<bool> _animations = AnimationSettings.Enabled;

    public void Dispose()
    {
        AnimationSettings.Enabled = _animations;
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static FrameTicker TickerOf(Spinner spinner) => spinner.GetPrivateField<FrameTicker>("_ticker")!;

    private static Form Host(Spinner spinner, Size size)
    {
        Form host = OffscreenForm.Create(new Size(Math.Max(size.Width, 20), Math.Max(size.Height, 20)), Color.Black);
        spinner.SetBounds(0, 0, size.Width, size.Height);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();

        return host;
    }

    public static IEnumerable<object[]> Sizes()
    {
        foreach (Size size in new[] { new Size(1, 1), new Size(3, 3), new Size(10, 40), new Size(40, 10), new Size(200, 24) })
        {
            foreach (int thickness in new[] { 0, 1, 60 })
            {
                foreach (SpinnerTextPosition position in Enum.GetValues(typeof(SpinnerTextPosition)))
                    yield return new object[] { size, thickness, position };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EverySizeThicknessAndTextPosition_CanBeDrawn_WithTheTextAndThePercentage(Size size, int thickness, SpinnerTextPosition position)
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressStatus(24, thickness);
        spinner.Text = "Loading";
        spinner.TextPosition = position;
        spinner.Value = 100;
        using Form host = Host(spinner, size);

        using Bitmap picture = Draw(spinner);

        Assert.Equal(size, picture.Size);
    }

    [Fact]
    public void ARingInAStretchedControl_StaysACircleInTheMiddle()
    {
        using Spinner spinner = UIStyles.Spinners.CreateStandard(24, 4);
        spinner.IsSpinning = false;
        using Form host = Host(spinner, new Size(100, 40));

        using Bitmap picture = Draw(spinner);

        for (int y = 0; y < 40; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                if (x < 28 || x >= 72)
                    Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(x, y).ToArgb());
            }
        }
    }

    // On the beat of the clock (not the slow watching of a spinner on a hidden window).
    private static bool Turning(Spinner spinner) => TickerOf(spinner).IsOnClock;

    [Fact]
    public void ASpinnerOnAHiddenWindow_OnlyWatches_AndTurnsAgainWhenTheWindowIsShown()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        using Form host = Host(spinner, new Size(24, 24));
        Assert.True(Turning(spinner));

        host.Hide();
        Pump(100);
        Assert.False(Turning(spinner), "turns in a hidden window");

        host.Show();
        Pump(400);
        Assert.True(Turning(spinner), "does not turn again when the window is shown");

        host.Controls.Remove(spinner);

        using Form other = OffscreenForm.Create(new Size(40, 40));
        other.Controls.Add(spinner);
        other.Show();
        Pump(400);
        Assert.True(Turning(spinner), "does not turn on another window");
    }

    [Fact]
    public void ASpinnerDisposedWhileItTurns_StopsItsClock_AndMayBeDisposedTwice()
    {
        Spinner spinner = UIStyles.Spinners.CreatePrimary();
        Form host = Host(spinner, new Size(24, 24));
        FrameTicker ticker = TickerOf(spinner);
        Assert.True(ticker.IsRunning);

        spinner.Dispose();
        spinner.Dispose();
        host.Dispose();

        Assert.False(ticker.IsRunning);
    }

    [Fact]
    public void WhenWindowsSwitchesTheAnimationsOffAndOn_TheSpinnerStopsAndTurnsAgain()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        using Form host = Host(spinner, new Size(24, 24));
        Assert.True(Turning(spinner));

        AnimationSettings.Enabled = () => false;
        AnimationSettings.Raise();
        Pump(100);
        Assert.False(TickerOf(spinner).IsRunning, "still turns after Windows switched the animations off");

        AnimationSettings.Enabled = _animations;
        AnimationSettings.Raise();
        Pump(100);
        Assert.True(Turning(spinner), "does not turn again after Windows switched the animations on");
    }

    [Fact]
    public void ASpinnerListensForTheSettingsOfWindows_OnlyWhileItHasAWindow()
    {
        int before = AnimationSettings.Count;
        Spinner spinner = UIStyles.Spinners.CreatePrimary();
        Assert.Equal(before, AnimationSettings.Count);

        Form host = Host(spinner, new Size(24, 24));
        Assert.Equal(before + 1, AnimationSettings.Count);

        host.Dispose();
        spinner.Dispose();
        Assert.Equal(before, AnimationSettings.Count);
    }

    [Fact]
    public void ChangingBetweenAnUnknownAndAKnownLoad_StopsAndStartsTheTurning()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary();
        using Form host = Host(spinner, new Size(40, 40));
        Assert.True(TickerOf(spinner).IsRunning);

        spinner.IsIndeterminate = false;
        Assert.False(TickerOf(spinner).IsRunning);

        spinner.IsIndeterminate = true;
        Assert.True(TickerOf(spinner).IsRunning);
    }

    [Fact]
    public void TheColorsSetOneAfterTheOther_EachReplacesTheLast()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        Color ArcOf() => spinner.InvokePrivate<Color>("ResolveArcColor");
        Color Ensured(Color raw) => UIColors.EnsureContrast(raw, spinner.GetTrackColor(), 3.0);

        Assert.Equal(Ensured(UIColors.PrimarySet.Hover), ArcOf());

        spinner.Colors = teal;
        UIColors.SetAccent(Color.OrangeRed);
        Assert.Equal(Ensured(teal.Hover), ArcOf());

        spinner.Colors = null;
        Assert.Equal(Ensured(UIColors.DisabledGray), ArcOf());

        spinner.FollowColors(() => UIColors.PrimarySet);
        Assert.Equal(Ensured(UIColors.PrimarySet.Hover), ArcOf());
        spinner.Colors = teal;
        Assert.Equal(Ensured(teal.Hover), ArcOf());
    }

    [Fact]
    public void TheThickness_CannotBeNegative()
    {
        using Spinner spinner = new Spinner();

        spinner.Thickness = -4;

        Assert.Equal(0, spinner.Thickness);
    }

    [Fact]
    public void ANameSetByHand_WinsOverTheText_ForAScreenReader()
    {
        using Spinner spinner = new Spinner { Text = "Loading" };
        Assert.Equal("Loading", spinner.AccessibilityObject.Name);

        spinner.AccessibleName = "Data";
        Assert.Equal("Data", spinner.AccessibilityObject.Name);
    }
}
