using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = System.Windows.Forms.ProgressBar;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>What the standard progress bar offers (Step, PerformStep, Increment, the Styles, the range) in both of ours.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarBehaviorTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private static Form Host(Control bar, int width = 200, int height = 30)
    {
        Form host = OffscreenForm.Create(new Size(width, height), Color.Black);
        bar.SetBounds(0, 0, width, bar.Height > 8 ? bar.Height : 6);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();

        return host;
    }

    private static Color Pixel(Control bar, int x)
    {
        using Bitmap picture = Draw(bar);

        return picture.GetPixel(x, bar.Height / 2);
    }

    private static MarqueeAnimator Marquee(Control bar) => bar.GetPrivateField<MarqueeAnimator>("_marquee")!;

    // --- Step, PerformStep, Increment ---

    [Fact]
    public void StepPerformStepAndIncrement_ActTheSameInBothBars_AsInTheStandardControl()
    {
        using ProgressBar full = UIStyles.ProgressBars.CreateStandard();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        using Form host = Host(full);
        host.Controls.Add(slim);
        Application.DoEvents();

        Assert.Equal(10, full.Step);
        Assert.Equal(10, slim.Step);

        List<(int Full, int Slim)> seen = new List<(int, int)>();

        // The same action on both (the two have no common interface).
        void Run(Action<Action, Action<int>, Action<int>> action)
        {
            action(full.PerformStep, full.Increment, step => full.Step = step);
            action(slim.PerformStep, slim.Increment, step => slim.Step = step);
            seen.Add((full.Value, slim.Value));
        }

        Run((perform, increment, setStep) => perform());
        Run((perform, increment, setStep) => perform());
        Run((perform, increment, setStep) => increment(65));
        Run((perform, increment, setStep) => increment(50));    // beyond the maximum
        Run((perform, increment, setStep) => increment(-250));  // below the minimum
        Run((perform, increment, setStep) => { setStep(30); perform(); perform(); });
        Run((perform, increment, setStep) => { increment(60); perform(); });

        Assert.All(seen, pair => Assert.Equal(pair.Full, pair.Slim));
        Assert.Equal(new[] { 10, 20, 85, 100, 0, 60, 100 }, seen.Select(pair => pair.Slim).ToArray());
    }

    [Fact]
    public void InTheMarqueeStyle_StepsAreRefused_InBothBars_AsInTheStandardControl()
    {
        using ProgressBar full = UIStyles.ProgressBars.CreateStandard();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        full.Style = ProgressBarStyle.Marquee;
        slim.Style = ProgressBarStyle.Marquee;

        Assert.Throws<InvalidOperationException>(() => full.PerformStep());
        Assert.Throws<InvalidOperationException>(() => slim.PerformStep());
        Assert.Throws<InvalidOperationException>(() => full.Increment(5));
        Assert.Throws<InvalidOperationException>(() => slim.Increment(5));
    }

    [Fact]
    public void AValueOutsideTheRange_ThrowsInTheStandardBar_AndIsLimitedInTheSlimOne()
    {
        using ProgressBar full = UIStyles.ProgressBars.CreateStandard();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Throws<ArgumentOutOfRangeException>(() => full.Value = 101);

        slim.Value = 101;
        Assert.Equal(100, slim.Value);

        slim.Value = -5;
        Assert.Equal(0, slim.Value);
    }

    // --- The styles ---

    public static IEnumerable<object[]> Bars()
    {
        yield return new object[] { "ErikwnkWFUI.Controls.ProgressBar", (Func<Control>)(() => UIStyles.ProgressBars.CreatePrimary()) };
        yield return new object[] { "SlimProgressBar", (Func<Control>)(() => UIStyles.SlimProgressBars.CreatePrimary()) };
    }

    private static void SetStyle(Control bar, ProgressBarStyle style)
    {
        bar.GetType().GetProperty("Style")!.SetValue(bar, style);
    }

    private static void SetValue(Control bar, int value)
    {
        bar.GetType().GetProperty("Value")!.SetValue(bar, value);
    }

    private static int InnerLeft(Control bar) => bar is ErikwnkWFUI.Controls.ProgressBar ? 1 : 0;

    [Theory]
    [MemberData(nameof(Bars))]
    public void Continuous_FillsTheValueInOnePiece(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar);
        SetValue(bar, 50);
        Application.DoEvents();

        using Bitmap picture = Draw(bar);
        int y = bar.Height / 2;
        int inner = InnerLeft(bar);

        for (int x = inner; x < 98; x++)
            Assert.Equal(PaintedColors.Fill(bar).ToArgb(), picture.GetPixel(x, y).ToArgb());

        Assert.Equal(bar.BackColor.ToArgb(), picture.GetPixel(150, y).ToArgb());
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void Blocks_FillsTheValueInSeparateBlocks(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar);
        SetStyle(bar, ProgressBarStyle.Blocks);
        SetValue(bar, 50);
        Application.DoEvents();

        using Bitmap picture = Draw(bar);
        int y = bar.Height / 2;
        int gaps = 0;

        for (int x = InnerLeft(bar); x < 98; x++)
        {
            if (picture.GetPixel(x, y).ToArgb() == bar.BackColor.ToArgb())
                gaps++;
        }

        Assert.True(gaps >= 3, "gaps between the blocks: " + gaps);
        Assert.Equal(bar.BackColor.ToArgb(), picture.GetPixel(150, y).ToArgb());
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void Marquee_ShowsABlockThatMoves_AndStopsAtSpeedZeroAndWhileHidden(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar);
        SetStyle(bar, ProgressBarStyle.Marquee);
        bar.GetType().GetProperty("MarqueeAnimationSpeed")!.SetValue(bar, 10);
        Pump(300);

        MarqueeAnimator marquee = Marquee(bar);
        Assert.True(marquee.Offset > 0, "the block has not moved");

        int moving = marquee.Offset;
        bar.GetType().GetProperty("MarqueeAnimationSpeed")!.SetValue(bar, 0);
        Pump(150);
        int stopped = marquee.Offset;
        Pump(150);
        Assert.Equal(stopped, marquee.Offset);
        Assert.True(stopped >= moving);

        bar.GetType().GetProperty("MarqueeAnimationSpeed")!.SetValue(bar, 10);
        Pump(100);
        bar.Visible = false;
        Pump(50);
        int hidden = marquee.Offset;
        Pump(200);
        Assert.Equal(hidden, marquee.Offset);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void Marquee_PaintsOneBlockOnTheTrack_NotTheValue(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar);
        SetValue(bar, 100);
        SetStyle(bar, ProgressBarStyle.Marquee);

        // Let the block run into the bar (it starts outside of it), then hold it.
        bar.GetType().GetProperty("MarqueeAnimationSpeed")!.SetValue(bar, 10);
        Pump(200);
        bar.GetType().GetProperty("MarqueeAnimationSpeed")!.SetValue(bar, 0);
        Application.DoEvents();

        using Bitmap picture = Draw(bar);
        int y = bar.Height / 2;
        int fill = 0;
        int track = 0;

        for (int x = InnerLeft(bar); x < bar.Width - InnerLeft(bar); x++)
        {
            Color pixel = picture.GetPixel(x, y);

            if (pixel.ToArgb() == PaintedColors.Fill(bar).ToArgb())
                fill++;
            else if (pixel.ToArgb() == bar.BackColor.ToArgb())
                track++;
        }

        // A block of a fifth of the bar - and not the whole bar, as the value 100 would give.
        Assert.InRange(fill, 1, bar.Width / 3);
        Assert.True(track > bar.Width / 2, "track pixels: " + track);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void TheMarqueeClock_IsDisposedWithTheBar(string name, Func<Control> make)
    {
        Control bar = make();
        Form host = Host(bar);
        SetStyle(bar, ProgressBarStyle.Marquee);
        Pump(60);
        MarqueeAnimator marquee = Marquee(bar);

        host.Dispose();
        int atTheEnd = marquee.Offset;
        Pump(100);

        Assert.Equal(atTheEnd, marquee.Offset);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void ARightToLeftSetting_IsIgnored_TheFillStartsOnTheLeft(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar);
        SetValue(bar, 30);
        bar.RightToLeft = RightToLeft.Yes;
        Application.DoEvents();

        Assert.Equal(PaintedColors.Fill(bar).ToArgb(), Pixel(bar, InnerLeft(bar) + 2).ToArgb());
        Assert.Equal(bar.BackColor.ToArgb(), Pixel(bar, bar.Width - 5).ToArgb());
    }

    // --- Only the slim bar ---

    [Fact]
    public void TheSlimBar_PaintsAgain_WhenTheStatusGradientOrTheSizeChanges()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        slim.Value = 100;
        using Form host = Host(slim);
        int invalidated = 0;
        slim.Invalidated += (sender, e) => invalidated++;

        slim.UseStatusGradient = true;
        Assert.True(invalidated > 0, "the gradient switch did not repaint");
        Assert.Equal(ProgressBarPainting.GetStatusFill(100, 0, 100, slim.BackColor).ToArgb(), Pixel(slim, 20).ToArgb());

        invalidated = 0;
        slim.UseStatusGradient = true;
        Assert.Equal(0, invalidated);

        slim.Width = 120;
        Application.DoEvents();
        Assert.True(invalidated > 0, "the new width did not repaint");
    }

    [Fact]
    public void TheSlimBar_IsAProgressBar_ForAScreenReader()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Equal(AccessibleRole.ProgressBar, slim.AccessibleRole);
    }

    [Fact]
    public void ANegativeMarqueeSpeed_IsRefused_AsInTheStandardControl()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Throws<ArgumentOutOfRangeException>(() => slim.MarqueeAnimationSpeed = -1);
    }
}
