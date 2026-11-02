using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = System.Windows.Forms.ProgressBar;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>The edge cases of the two progress bars: ranges, tiny sizes, the lifetime of the marquee, huge steps.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarEdgeTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    public static IEnumerable<object[]> Bars()
    {
        yield return new object[] { "ProgressBar", (Func<Control>)(() => UIStyles.ProgressBars.CreatePrimary()) };
        yield return new object[] { "SlimProgressBar", (Func<Control>)(() => UIStyles.SlimProgressBars.CreatePrimary()) };
    }

    private static Form Host(Control bar, int width, int height, bool show = true)
    {
        Form host = OffscreenForm.Create(new Size(Math.Max(width, 60), Math.Max(height, 60)), Color.Black);
        bar.SetBounds(0, 0, width, height);
        host.Controls.Add(bar);

        if (show)
        {
            host.Show();
            Application.DoEvents();
        }

        return host;
    }

    private static void Set(Control bar, string property, object value)
    {
        bar.GetType().GetProperty(property)!.SetValue(bar, value);
    }

    private static T Get<T>(Control bar, string property) => (T)bar.GetType().GetProperty(property)!.GetValue(bar)!;

    private static MarqueeAnimator Marquee(Control bar) => bar.GetPrivateField<MarqueeAnimator>("_marquee")!;

    // How many pixels in the row in the middle have the fill color.
    private static int FillPixels(Control bar)
    {
        using Bitmap picture = Draw(bar);
        int count = 0;

        for (int x = 0; x < picture.Width; x++)
            if (picture.GetPixel(x, picture.Height / 2).ToArgb() == PaintedColors.Fill(bar).ToArgb())
                count++;

        return count;
    }

    // --- The range ---

    [Theory]
    [MemberData(nameof(Bars))]
    public void TheFill_IsEmptyAtTheMinimum_AndFullAtTheMaximum(string name, Func<Control> make)
    {
        Control bar = make();
        int inner = bar is SlimProgressBar ? 0 : 2;
        using Form host = Host(bar, 100, 23);

        Set(bar, "Value", 0);
        Application.DoEvents();
        Assert.Equal(0, FillPixels(bar));

        Set(bar, "Value", 100);
        Application.DoEvents();
        Assert.Equal(100 - inner, FillPixels(bar));
    }

    [Fact]
    public void TheSlimBar_AllowsANegativeRange_ThatTheStandardBarRefuses()
    {
        using ProgressBar full = UIStyles.ProgressBars.CreateStandard();
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        using Form host = Host(slim, 102, 23);

        Assert.Throws<ArgumentOutOfRangeException>(() => full.Minimum = -50);

        slim.Minimum = -50;
        slim.Maximum = 50;
        slim.Value = 0;
        Application.DoEvents();

        Assert.InRange(FillPixels(slim), 50, 52);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void ARangeWithoutWidth_FillsNothing_AndDoesNotFail(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar, 100, 23);

        Set(bar, "Minimum", 50);
        Set(bar, "Maximum", 50);
        Set(bar, "Value", 50);
        Application.DoEvents();

        Assert.Equal(0, FillPixels(bar));

        Set(bar, "ShowPercentage", true);
        Assert.Null(Record.Exception(() => Draw(bar).Dispose()));
    }

    [Fact]
    public void TheSlimBar_WithAMaximumBelowTheMinimum_StaysAtTheMinimum_WithoutFailing()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Minimum = 10;
        slim.Maximum = -10;
        slim.Value = 5;

        Assert.Equal(10, slim.Value);
        Assert.Equal(0, ProgressBarPainting.GetFraction(10, -10, 5));
    }

    // --- Huge steps ---

    [Fact]
    public void AHugeStep_IsLimitedToTheRange_NotWrappedAroundToTheOtherEnd()
    {
        // (The standard bar adds in an int and gets a negative number from Increment(int.MaxValue): that is not copied.)
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        slim.Value = 50;

        slim.Increment(int.MaxValue);
        Assert.Equal(100, slim.Value);

        slim.Increment(int.MinValue);
        Assert.Equal(0, slim.Value);

        slim.Value = 50;
        slim.Step = int.MaxValue;
        slim.PerformStep();
        Assert.Equal(100, slim.Value);

        slim.Value = 50;
        slim.Step = int.MinValue;
        slim.PerformStep();
        Assert.Equal(0, slim.Value);
    }

    // --- Tiny sizes ---

    [Theory]
    [MemberData(nameof(Bars))]
    public void ATinyBar_CanBePaintedInEveryStyle_WithATextAndAPercentage(string name, Func<Control> make)
    {
        foreach (Size size in new[] { new Size(1, 1), new Size(2, 2), new Size(3, 3), new Size(2, 40), new Size(40, 2), new Size(5, 23) })
        {
            foreach (ProgressBarStyle style in new[] { ProgressBarStyle.Continuous, ProgressBarStyle.Blocks, ProgressBarStyle.Marquee })
            {
                Control bar = make();
                using Form host = Host(bar, size.Width, size.Height, show: false);
                Set(bar, "Style", style);
                Set(bar, "Text", "Uploading");
                Set(bar, "ShowPercentage", true);
                host.Show();
                Application.DoEvents();

                Assert.Null(Record.Exception(() => Draw(bar).Dispose()));
            }
        }
    }

    // --- Texts that are not plain ---

    [Theory]
    [MemberData(nameof(Bars))]
    public void ANullText_IsAnEmptyOne(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar, 100, 23);
        Set(bar, "ShowPercentage", false);

        Assert.Null(Record.Exception(() => bar.Text = null!));
        Assert.Equal("", bar.Text);
        Assert.Null(Record.Exception(() => Draw(bar).Dispose()));
    }

    // --- The marquee over the life of the bar ---

    [Theory]
    [MemberData(nameof(Bars))]
    public void AMarqueeThatWasSetBeforeTheWindowWasShown_RunsAsSoonAsItIsShown(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar, 200, 23, show: false);
        Set(bar, "Style", ProgressBarStyle.Marquee);
        Set(bar, "MarqueeAnimationSpeed", 10);

        host.Show();
        Pump(250);

        Assert.True(Marquee(bar).Offset > 0);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void TheMarquee_StopsWhenTheStyleChangesAway_AndRunsAgainWhenItComesBack(string name, Func<Control> make)
    {
        Control bar = make();
        using Form host = Host(bar, 200, 23);
        Set(bar, "MarqueeAnimationSpeed", 10);

        Set(bar, "Style", ProgressBarStyle.Marquee);
        Pump(150);
        Assert.True(Marquee(bar).Offset > 0);

        Set(bar, "Style", ProgressBarStyle.Continuous);
        Pump(60);
        int stopped = Marquee(bar).Offset;
        Pump(150);
        Assert.Equal(stopped, Marquee(bar).Offset);

        Set(bar, "Style", ProgressBarStyle.Marquee);
        Pump(250);
        Assert.NotEqual(stopped, Marquee(bar).Offset);
    }

    [Theory]
    [MemberData(nameof(Bars))]
    public void ABarThatIsDisposedWhileItRuns_LeavesNothingBehind(string name, Func<Control> make)
    {
        Control bar = make();
        Form host = Host(bar, 200, 23);
        Set(bar, "Style", ProgressBarStyle.Marquee);
        Set(bar, "MarqueeAnimationSpeed", 10);
        Pump(60);

        bar.Dispose();

        Assert.Null(Record.Exception(() => Pump(150)));
        host.Dispose();
    }

    // --- The size of the slim bar follows its font ---

    [Fact]
    public void TheSlimBar_WithAutoSize_FollowsAChangeOfTheFont_AndWithoutItDoesNot()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Text = "Uploading";
        slim.AutoSize = true;
        using Form host = Host(slim, 200, 4);
        slim.Size = slim.GetPreferredSize(Size.Empty);
        int before = slim.Height;

        slim.Font = new Font(slim.Font.FontFamily, slim.Font.Size * 2);
        Assert.True(slim.Height > before, "height " + slim.Height + " after " + before);

        slim.AutoSize = false;
        int fixedHeight = slim.Height;
        slim.Font = new Font(slim.Font.FontFamily, slim.Font.Size * 2);
        Assert.Equal(fixedHeight, slim.Height);
    }
}
