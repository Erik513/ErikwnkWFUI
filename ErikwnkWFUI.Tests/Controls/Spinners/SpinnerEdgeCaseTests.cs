using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>The odd cases: tiny and stretched sizes, a handle that is made again, a spinner disposed while it turns, the colors set one after the other.</summary>
[Collection(AccentColorTestCollection.Name)]
public class SpinnerEdgeCaseTests : IDisposable
{
    private readonly Func<bool> _animations = ThemedLabel.AnimationsEnabled;

    public void Dispose()
    {
        ThemedLabel.AnimationsEnabled = _animations;
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static System.Windows.Forms.Timer TimerOf(Spinner spinner) => spinner.GetPrivateField<System.Windows.Forms.Timer>("_timer")!;

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

    // The clock at full speed (not the slow watching of a hidden spinner).
    private static bool Turning(Spinner spinner) => TimerOf(spinner).Enabled && TimerOf(spinner).Interval < 100;

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
        System.Windows.Forms.Timer timer = TimerOf(spinner);
        Assert.True(timer.Enabled);

        spinner.Dispose();
        spinner.Dispose();
        host.Dispose();

        Assert.False(timer.Enabled);
    }

    [Fact]
    public void TheAnimationSettingOfWindows_IsAskedAgainWhileItTurns_AndAStillArcStaysStill()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        using Form host = Host(spinner, new Size(24, 24));

        for (int tick = 0; tick < 100; tick++)
            spinner.InvokePrivate("OnTick", spinner, EventArgs.Empty);
        Assert.True(TimerOf(spinner).Enabled);

        ThemedLabel.AnimationsEnabled = () => false;
        for (int tick = 0; tick < 32; tick++)
            spinner.InvokePrivate("OnTick", spinner, EventArgs.Empty);
        Assert.False(TimerOf(spinner).Enabled, "still turns after Windows switched the animations off");
    }

    [Fact]
    public void ChangingBetweenAnUnknownAndAKnownLoad_StopsAndStartsTheTurning()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary();
        using Form host = Host(spinner, new Size(40, 40));
        Assert.True(TimerOf(spinner).Enabled);

        spinner.IsIndeterminate = false;
        Assert.False(TimerOf(spinner).Enabled);

        spinner.IsIndeterminate = true;
        Assert.True(TimerOf(spinner).Enabled);
    }

    [Fact]
    public void TheColorsSetOneAfterTheOther_EachReplacesTheLast()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        Color ArcOf() => spinner.InvokePrivate<Color>("ResolveArcColor");

        Assert.Equal(UIColors.PrimarySet.Hover, ArcOf());

        spinner.Colors = teal;
        UIColors.SetAccent(Color.OrangeRed);
        Assert.Equal(teal.Hover, ArcOf());

        spinner.Colors = null;
        Assert.Equal(UIColors.DisabledGray, ArcOf());

        spinner.FollowColors(() => UIColors.PrimarySet);
        Assert.Equal(UIColors.PrimarySet.Hover, ArcOf());
        spinner.Colors = teal;
        Assert.Equal(teal.Hover, ArcOf());
    }

    [Fact]
    public void ThicknessAndSweep_AreLimited()
    {
        using Spinner spinner = new Spinner();

        spinner.Thickness = -4;
        Assert.Equal(0, spinner.Thickness);

        spinner.ArcSweep = 5;
        Assert.Equal(10f, spinner.ArcSweep);
        spinner.ArcSweep = 500;
        Assert.Equal(350f, spinner.ArcSweep);
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
