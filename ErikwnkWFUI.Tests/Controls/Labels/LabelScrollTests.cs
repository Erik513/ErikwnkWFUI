using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The text that moves back and forth when it is too long (ScrollMode.BackAndForth): off by default,
/// rests at both ends, moves at its speed however often the animation clock beats, stands still when it
/// fits, when the label is disabled or hidden, and leaves the clock when it is done. The time is a clock the
/// tests set.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelScrollTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly Func<long> _originalClock = ScrollingLabel.Milliseconds;
    private readonly List<Form> _forms = new List<Form>();
    private long _now = 1_000_000;

    public LabelScrollTests()
    {
        ScrollingLabel.Milliseconds = () => _now;
    }

    public void Dispose()
    {
        ScrollingLabel.Milliseconds = _originalClock;

        foreach (Form form in _forms)
            form.Dispose();
    }

    // A label of 120 pixels with a long text, in a window that is shown (out of sight).
    private ScrollingLabel Create(bool scroll = true, string text = LongText)
    {
        ScrollingLabel label = UIStyles.Labels.CreateStandard(text);
        label.Size = new Size(120, 24);
        label.ScrollMode = scroll ? LabelScrollMode.BackAndForth : LabelScrollMode.None;

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(40, 40, 40),
            ClientSize = new Size(200, 60)
        };
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);

        return label;
    }

    private static float Offset(ScrollingLabel label) => label.GetPrivateField<float>("_offset");

    private static int MaxOffset(ScrollingLabel label) => label.GetPrivateField<int>("_maxOffset");

    // Whether the label is on the beat of the animation clock (the thing that makes it move).
    private static bool Moving(ScrollingLabel label) => label.GetPrivateField<bool>("_animating");

    // The animation clock beats after this long.
    private void Tick(ScrollingLabel label, int milliseconds)
    {
        _now += milliseconds;
        label.InvokePrivate("OnFrame", label, EventArgs.Empty);
    }

    private static Bitmap Paint(ScrollingLabel label)
    {
        Bitmap bitmap = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(bitmap, new Rectangle(0, 0, label.Width, label.Height));

        return bitmap;
    }

    [Fact]
    public void ByDefault_TheTextDoesNotMove_AndEndsInAnEllipsis()
    {
        using ScrollingLabel label = Create(scroll: false);

        Assert.Equal(LabelScrollMode.None, label.ScrollMode);
        Assert.Equal(0, MaxOffset(label));
        Assert.False(Moving(label));
        Assert.True(label.AutoEllipsis);
    }

    [Fact]
    public void ALongText_WhenTheScrollingIsOn_GetsARoomToMove_AndIsOnTheClock()
    {
        using ScrollingLabel label = Create();

        Assert.True(MaxOffset(label) > 0);
        Assert.True(Moving(label));
    }

    [Fact]
    public void TheTextRestsAtTheStart_ThenMoves_AtItsSpeed()
    {
        using ScrollingLabel label = Create();

        Tick(label, 2000);
        Assert.Equal(0f, Offset(label));

        // The pause (2.2 seconds) is over; the movement counts from there, 45 pixels a second.
        Tick(label, 200);
        Assert.Equal(0f, Offset(label));

        Tick(label, 1000);
        Assert.Equal(45f, Offset(label), 1);

        Tick(label, 1000);
        Assert.Equal(90f, Offset(label), 1);
    }

    [Fact]
    public void TheSpeed_DoesNotDependOnHowOftenTheClockBeats()
    {
        // One label whose clock beats every 20 milliseconds, one whose clock beats once a second.
        using ScrollingLabel often = Create();
        Tick(often, 2200);

        for (int i = 0; i < 50; i++)
            Tick(often, 20);

        using ScrollingLabel seldom = Create();
        Tick(seldom, 2200);
        Tick(seldom, 1000);

        Assert.Equal(45f, Offset(often), 0);
        Assert.Equal(45f, Offset(seldom), 0);
    }

    [Fact]
    public void TheThreeSpeeds_AreSlowMediumFast()
    {
        float Moved(LabelScrollSpeed speed)
        {
            using ScrollingLabel label = Create(scroll: false);
            label.ScrollSpeed = speed;
            label.ScrollMode = LabelScrollMode.BackAndForth;
            Tick(label, 2200);
            Tick(label, 1000);

            return Offset(label);
        }

        Assert.Equal(25f, Moved(LabelScrollSpeed.Slow), 0);
        Assert.Equal(45f, Moved(LabelScrollSpeed.Medium), 0);
        Assert.Equal(90f, Moved(LabelScrollSpeed.Fast), 0);
    }

    // Lets time pass in steps until the label's text has got to where it should.
    private void RunUntil(ScrollingLabel label, Func<bool> reached)
    {
        for (int i = 0; i < 400 && !reached(); i++)
            Tick(label, 50);

        Assert.True(reached(), "the text did not get there");
    }

    [Fact]
    public void AtTheEnd_TheTextRests_ThenMovesBack_AndRestsAtTheStartAgain()
    {
        using ScrollingLabel label = Create(scroll: false);
        label.ScrollSpeed = LabelScrollSpeed.Fast;
        label.ScrollPause = LabelScrollPause.Short;
        label.ScrollMode = LabelScrollMode.BackAndForth;

        // The pause at the start (1 second), then it moves to the end.
        Tick(label, 1000);
        Assert.Equal(0f, Offset(label));
        RunUntil(label, () => Offset(label) >= MaxOffset(label));
        Assert.Equal(MaxOffset(label), (int)Offset(label));

        // It rests at the end for the pause and does not move.
        Tick(label, 500);
        Assert.Equal(MaxOffset(label), (int)Offset(label));

        // The pause is over: it moves back.
        Tick(label, 600);
        Assert.InRange(Offset(label), 1f, MaxOffset(label) - 1f);
        RunUntil(label, () => Offset(label) <= 0f);

        // At the start it rests again, then goes forward.
        Tick(label, 500);
        Assert.Equal(0f, Offset(label));
        Tick(label, 600);
        Assert.InRange(Offset(label), 1f, MaxOffset(label) - 1f);
    }

    [Fact]
    public void TheThreePauses_AreShortMediumLong()
    {
        // How long the text rests at the start, found by letting time pass until it moves.
        int Rest(LabelScrollPause pause)
        {
            using ScrollingLabel label = Create(scroll: false);
            label.ScrollPause = pause;
            label.ScrollMode = LabelScrollMode.BackAndForth;
            int rested = 0;

            while (rested < 10000)
            {
                Tick(label, 100);
                rested += 100;

                if (Offset(label) > 0f)
                    break;
            }

            return rested;
        }

        Assert.InRange(Rest(LabelScrollPause.Short), 1000, 1200);
        Assert.InRange(Rest(LabelScrollPause.Medium), 2200, 2400);
        Assert.InRange(Rest(LabelScrollPause.Long), 4000, 4200);
    }

    [Fact]
    public void APictureIsOnlyMade_WhenTheTextHasMoved_AtAnyPlaceBetweenTwoPixels()
    {
        using ScrollingLabel label = Create(scroll: false);
        label.ScrollSpeed = LabelScrollSpeed.Slow;
        label.ScrollMode = LabelScrollMode.BackAndForth;

        // While it rests there is nothing new to show.
        _now += 1000;
        Assert.False(label.InvokePrivate<bool>("Step"));

        // The pause is over; a few milliseconds later it has moved a fraction of a pixel (25 a second):
        // that is a new picture too, the text can stand between two pixels.
        _now += 1200;
        Assert.False(label.InvokePrivate<bool>("Step"));
        _now += 10;
        Assert.True(label.InvokePrivate<bool>("Step"));
        Assert.True(Offset(label) > 0f && Offset(label) < 1f, "not a fraction of a pixel: " + Offset(label));
    }

    [Fact]
    public void BetweenTwoPixels_TheTextIsPaintedBetweenThem()
    {
        using ScrollingLabel label = Create(scroll: false);
        label.ScrollSpeed = LabelScrollSpeed.Slow;
        label.ScrollMode = LabelScrollMode.BackAndForth;
        _now += 2200;

        // Half a pixel further: 20 milliseconds at 25 pixels a second.
        _now += 20;
        label.InvokePrivate<bool>("Step");
        using Bitmap half = Paint(label);

        _now += 20;
        label.InvokePrivate<bool>("Step");
        using Bitmap whole = Paint(label);

        _now += 40;
        label.InvokePrivate<bool>("Step");
        using Bitmap two = Paint(label);

        Assert.False(SamePixels(half, whole));
        Assert.False(SamePixels(whole, two));
    }

    [Fact]
    public void TheClock_ReachesTheLabel_ThroughItsMessageQueue_AndTheLabelMoves()
    {
        using ScrollingLabel label = Create(scroll: false);
        label.ScrollSpeed = LabelScrollSpeed.Fast;
        label.ScrollMode = LabelScrollMode.BackAndForth;
        _now += 2200;
        _now += 1000;

        label.RequestFrame();

        for (int i = 0; i < 50 && Offset(label) == 0f; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(Offset(label) > 0f, "the label did not make its picture");
    }

    [Fact]
    public void ALabelThatIsAskedTwice_BeforeItAnswers_IsAskedOnce()
    {
        using ScrollingLabel label = Create();

        label.RequestFrame();
        label.RequestFrame();
        label.RequestFrame();

        Assert.Equal(1, label.GetPrivateField<int>("_framePending"));

        Application.DoEvents();
    }

    [Fact]
    public void AskingALabelWithoutAWindow_IsHarmless()
    {
        ScrollingLabel label = UIStyles.Labels.CreateStandard("x");

        Assert.Null(Record.Exception(() => label.RequestFrame()));

        label.Dispose();
        Assert.Null(Record.Exception(() => label.RequestFrame()));
    }

    [Fact]
    public void ATextThatFits_StandsStill_OffTheClock()
    {
        using ScrollingLabel label = Create(text: "Short");

        Assert.Equal(0, MaxOffset(label));
        Assert.False(Moving(label));
    }

    [Fact]
    public void WhenTheLabelBecomesWideEnough_ItStopsMoving_AndWhenNarrowAgain_StartsAnew()
    {
        using ScrollingLabel label = Create();
        Tick(label, 2200);
        Tick(label, 500);
        Assert.True(Offset(label) > 0f);

        label.Width = 1000;
        Assert.Equal(0, MaxOffset(label));
        Assert.Equal(0f, Offset(label));
        Assert.False(Moving(label));

        label.Width = 120;
        Assert.True(MaxOffset(label) > 0);
        Assert.True(Moving(label));
        Assert.Equal(0f, Offset(label));
    }

    [Fact]
    public void ANewText_ANewFont_AndANewPadding_StartTheMovementOver()
    {
        using ScrollingLabel label = Create();
        Tick(label, 2200);
        Tick(label, 500);

        label.Text = LongText + " and longer";
        Assert.Equal(0f, Offset(label));

        Tick(label, 2200);
        Tick(label, 500);
        label.Font = UIFonts.Title;
        Assert.Equal(0f, Offset(label));

        Tick(label, 2200);
        Tick(label, 500);
        label.Padding = new Padding(10, 0, 10, 0);
        Assert.Equal(0f, Offset(label));
    }

    [Fact]
    public void AfterAChange_TheTextRestsFirst()
    {
        using ScrollingLabel label = Create();
        label.Text = LongText + " again";

        Tick(label, 2000);

        Assert.Equal(0f, Offset(label));
    }

    [Fact]
    public void ADisabledLabel_DoesNotMove_AndLeavesTheClock()
    {
        using ScrollingLabel label = Create();
        Tick(label, 2200);
        Tick(label, 500);

        label.Enabled = false;

        Assert.Equal(0, MaxOffset(label));
        Assert.Equal(0f, Offset(label));
        Assert.False(Moving(label));

        label.Enabled = true;

        Assert.True(Moving(label));
    }

    [Fact]
    public void AHiddenLabel_IsOffTheClock()
    {
        using ScrollingLabel label = Create();

        label.Visible = false;
        Assert.False(Moving(label));

        label.Visible = true;
        Assert.True(Moving(label));
    }

    [Fact]
    public void ALabelThatIsDisposed_LeavesTheClock()
    {
        ScrollingLabel label = Create();
        Assert.True(AnimationClock.Count >= 1);

        label.Dispose();

        Assert.False(Moving(label));
        Assert.Equal(0, AnimationClock.Count);
        Assert.Null(Record.Exception(() => label.ScrollMode = LabelScrollMode.None));
    }

    [Fact]
    public void AnAutoSizedLabel_AndATextOfSeveralLines_DoNotMove()
    {
        using ScrollingLabel autoSized = Create();
        autoSized.AutoSize = true;
        autoSized.Text = LongText + " and more";

        using ScrollingLabel lines = Create(text: LongText + "\nSecond line");

        Assert.Equal(0, MaxOffset(autoSized));
        Assert.Equal(0, MaxOffset(lines));
    }

    [Fact]
    public void TheMovingText_IsPaintedFurtherLeft_AsItMoves_AndStaysInsideTheLabel()
    {
        using ScrollingLabel label = Create();
        label.Padding = new Padding(10, 0, 10, 0);
        using Bitmap start = Paint(label);

        Tick(label, 2200);
        Tick(label, 2000);
        using Bitmap later = Paint(label);

        Assert.False(SamePixels(start, later), "the picture did not change as the text moved");

        // Nothing is painted in the padding, at the left and at the right.
        for (int y = 0; y < later.Height; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                Assert.True(later.GetPixel(x, y) == later.GetPixel(0, 0) || later.GetPixel(x, y).GetBrightness() < 0.2f, "ink in the left padding at " + x + "," + y);
                Assert.True(later.GetPixel(later.Width - 1 - x, y).GetBrightness() < 0.2f, "ink in the right padding");
            }
        }
    }

    [Fact]
    public void ADisabledLabel_LooksTheSame_WithOrWithoutTheScrolling()
    {
        using ScrollingLabel scrolling = Create();
        using ScrollingLabel still = Create(scroll: false);
        scrolling.Enabled = still.Enabled = false;

        using Bitmap first = Paint(scrolling);
        using Bitmap second = Paint(still);

        Assert.True(SamePixels(first, second));
    }

    [Fact]
    public void AnUnknownSpeedOrPause_IsTakenAsTheMediumOne()
    {
        Assert.Equal(45f, ((LabelScrollSpeed)99).ToPixelsPerSecond());
        Assert.Equal(2200, ((LabelScrollPause)99).ToMilliseconds());
    }

    [Fact]
    public void TheThreeSpeedsAndThePauses_AreInOrder()
    {
        Assert.True(LabelScrollSpeed.Slow.ToPixelsPerSecond() < LabelScrollSpeed.Medium.ToPixelsPerSecond());
        Assert.True(LabelScrollSpeed.Medium.ToPixelsPerSecond() < LabelScrollSpeed.Fast.ToPixelsPerSecond());
        Assert.True(LabelScrollPause.Short.ToMilliseconds() < LabelScrollPause.Medium.ToMilliseconds());
        Assert.True(LabelScrollPause.Medium.ToMilliseconds() < LabelScrollPause.Long.ToMilliseconds());
    }

    [Fact]
    public void TheDefaults_AreTheMediumOnes_AndTheScrollingIsOff()
    {
        using ScrollingLabel label = UIStyles.Labels.CreateStandard("x");

        Assert.Equal(LabelScrollSpeed.Medium, label.ScrollSpeed);
        Assert.Equal(LabelScrollPause.Medium, label.ScrollPause);
        Assert.Equal(LabelScrollMode.None, label.ScrollMode);
        Assert.Equal(1, (int)LabelScrollSpeed.Medium);
        Assert.Equal(1, (int)LabelScrollPause.Medium);
    }

    [Fact]
    public void AllFourKinds_CanScroll()
    {
        foreach (ScrollingLabel label in new[]
        {
            UIStyles.Labels.CreateStandard(LongText), UIStyles.Labels.CreatePrimary(LongText),
            UIStyles.Labels.CreateTitle(LongText), UIStyles.Labels.CreateMuted(LongText)
        })
        {
            using (label)
            {
                label.Size = new Size(120, 24);
                label.ScrollMode = LabelScrollMode.BackAndForth;

                Assert.True(label.GetPrivateField<int>("_maxOffset") >= 0);
            }
        }
    }
}
