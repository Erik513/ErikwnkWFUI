using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// What a control that moves gets from its ticker: steps on the beat of the shared clock with the time that has passed, none while it
/// is stopped or hidden. [Collection]: the number of clients of the clock is counted, and other tests start clients.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class FrameTickerTests
{
    private static Form Show(Control control)
    {
        return OffscreenForm.Show(control, new Size(100, 100), bounds: new Rectangle(0, 0, 50, 50));
    }

    [Fact]
    public void ARunningTicker_StepsOnTheBeatOfTheClock_WithTheTimeThatHasPassed_AndStopsStepping()
    {
        using Control owner = new Control();
        using Form host = Show(owner);
        List<double> steps = new List<double>();
        using FrameTicker ticker = new FrameTicker(owner, steps.Add);

        ticker.Start();
        Pump(300);

        Assert.True(steps.Count >= 5, "only " + steps.Count + " steps in 300 ms");
        Assert.All(steps, step => Assert.InRange(step, 0.0, FrameTicker.LongestStepMilliseconds));
        Assert.InRange(steps.Sum(), 150, 450);

        ticker.Stop();
        Pump(50);
        int stopped = steps.Count;
        Pump(200);

        Assert.Equal(stopped, steps.Count);
        Assert.False(ticker.IsRunning);
        Assert.False(ticker.IsOnClock);
    }

    [Fact]
    public void AStallOfTheWindow_IsOneStepOfAtMostTheLongestStep_SoWhatMovesDoesNotJump()
    {
        using Control owner = new Control();
        using Form host = Show(owner);
        double received = -1;
        using FrameTicker ticker = new FrameTicker(owner, step => received = step);
        ticker.Start();

        ticker.SetPrivateField("_lastStep", Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency - 60000L); // the last step was a minute ago
        ticker.InvokePrivate("MakeStep");

        Assert.Equal(FrameTicker.LongestStepMilliseconds, received);
    }

    [Fact]
    public void ATickerOnAHiddenWindow_LeavesTheClock_AndComesBackWhenTheWindowIsShown()
    {
        using Control owner = new Control();
        using Form host = Show(owner);
        int steps = 0;
        using FrameTicker ticker = new FrameTicker(owner, step => steps++);
        int before = AnimationClock.Count;

        ticker.Start();
        Assert.Equal(before + 1, AnimationClock.Count);

        host.Hide();
        Pump(150);
        Assert.True(ticker.IsRunning);
        Assert.False(ticker.IsOnClock, "stays on the clock for a window nobody sees");
        Assert.Equal(before, AnimationClock.Count);

        int hidden = steps;
        Pump(200);
        Assert.Equal(hidden, steps);

        host.Show();
        Pump(500);
        Assert.True(ticker.IsOnClock, "does not come back when the window is shown");
        Assert.True(steps > hidden);
    }

    [Fact]
    public void ATickerStartedOnAHiddenControl_WaitsUntilItIsShown()
    {
        using Control owner = new Control { Visible = false };
        using Form host = Show(owner);
        owner.Visible = false;
        using FrameTicker ticker = new FrameTicker(owner, step => { });
        int before = AnimationClock.Count;

        ticker.Start();

        Assert.True(ticker.IsRunning);
        Assert.False(ticker.IsOnClock);
        Assert.Equal(before, AnimationClock.Count);

        owner.Visible = true;
        Pump(500);

        Assert.True(ticker.IsOnClock);
    }

    [Fact]
    public void StartingTwice_DoesNotJoinTheClockTwice_AndDisposingLeavesIt()
    {
        using Control owner = new Control();
        using Form host = Show(owner);
        FrameTicker ticker = new FrameTicker(owner, step => { });
        int before = AnimationClock.Count;

        ticker.Start();
        ticker.Start();
        Assert.Equal(before + 1, AnimationClock.Count);

        ticker.Dispose();
        ticker.Dispose();
        Assert.Equal(before, AnimationClock.Count);
    }
}
