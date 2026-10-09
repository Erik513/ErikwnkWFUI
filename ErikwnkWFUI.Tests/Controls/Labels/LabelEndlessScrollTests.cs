using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The endless way of the moving text (ScrollMode.Endless): it goes on without rest in one direction, left or
/// right, and comes again behind a gap and a separator. The time is a clock the tests set.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelEndlessScrollTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly Func<long> _originalClock = ThemedLabel.Milliseconds;
    private readonly List<Form> _forms = new List<Form>();
    private long _now = 1_000_000;

    public LabelEndlessScrollTests()
    {
        ThemedLabel.Milliseconds = () => _now;
    }

    public void Dispose()
    {
        ThemedLabel.Milliseconds = _originalClock;

        foreach (Form form in _forms)
            form.Dispose();
    }

    // A label of 120 pixels with a long text, moving endlessly, in a window that is shown (out of sight).
    private ThemedLabel Create(
        LabelScrollDirection direction = LabelScrollDirection.Left,
        LabelScrollSeparator separator = LabelScrollSeparator.Dot,
        string text = LongText,
        bool endless = true)
    {
        ThemedLabel label = UIStyles.Labels.CreateStandard(text);
        label.Size = new Size(120, 24);
        label.ScrollDirection = direction;
        label.ScrollSeparator = separator;
        label.ScrollMode = endless ? LabelScrollMode.Endless : LabelScrollMode.BackAndForth;

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

    private static float Offset(ThemedLabel label) => label.GetPrivateField<float>("_offset");

    private static int Cycle(ThemedLabel label) => label.GetPrivateField<int>("_cycle");

    private static int MaxOffset(ThemedLabel label) => label.GetPrivateField<int>("_maxOffset");

    private static bool Moving(ThemedLabel label) => label.GetPrivateField<bool>("_animating");

    private void Tick(ThemedLabel label, int milliseconds)
    {
        _now += milliseconds;
        label.InvokePrivate("OnFrame", label, EventArgs.Empty);
    }

    // Puts the text at a place of its round, as if the clock had brought it there.
    private static void SetOffset(ThemedLabel label, float offset)
    {
        typeof(ThemedLabel).GetField("_offset", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(label, offset);
    }

    private static Bitmap Paint(ThemedLabel label)
    {
        Bitmap bitmap = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(bitmap, new Rectangle(0, 0, label.Width, label.Height));

        return bitmap;
    }

    private static bool Same(Bitmap a, Bitmap b)
    {
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }

    [Fact]
    public void TheDefaults_AreNoMovement_ToTheLeft_WithADot()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard("x");

        Assert.Equal(LabelScrollMode.None, label.ScrollMode);
        Assert.Equal(LabelScrollDirection.Left, label.ScrollDirection);
        Assert.Equal(LabelScrollSeparator.Dot, label.ScrollSeparator);
    }

    [Fact]
    public void ALongText_InTheEndlessMode_HasARound_AndIsOnTheClock_NotARangeToGo()
    {
        using ThemedLabel label = Create();

        Assert.True(Cycle(label) > 0);
        Assert.Equal(0, MaxOffset(label));
        Assert.True(Moving(label));
    }

    [Fact]
    public void TheRound_IsTheTextAGapTheSeparatorAndAGap_WiderThanTheText()
    {
        using ThemedLabel dot = Create(separator: LabelScrollSeparator.Dot);
        using ThemedLabel none = Create(separator: LabelScrollSeparator.None);
        using ThemedLabel diamond = Create(separator: LabelScrollSeparator.Diamond);
        using ThemedLabel bar = Create(separator: LabelScrollSeparator.Bar);

        int textWidth = none.GetPrivateField<int>("_textWidth");

        Assert.True(Cycle(none) > textWidth, "no gap after the text");
        Assert.True(Cycle(dot) > Cycle(none), "the dot takes no room");
        Assert.True(Cycle(diamond) > Cycle(none), "the diamond takes no room");
        Assert.True(Cycle(bar) > Cycle(none), "the bar takes no room");
    }

    [Fact]
    public void ATextThatFits_StandsStill_InTheEndlessModeToo()
    {
        using ThemedLabel label = Create(text: "Short");

        Assert.Equal(0, Cycle(label));
        Assert.False(Moving(label));
    }

    [Fact]
    public void TheEndlessText_HasNoRestAtTheStart_AndNoneAtAnyPoint()
    {
        using ThemedLabel label = Create();
        label.ScrollPause = LabelScrollPause.Long;

        // 45 pixels a second from the very first moment, whatever the pause says.
        Tick(label, 1000);
        Assert.Equal(45f, Offset(label), 0);

        Tick(label, 1000);
        Assert.Equal(90f, Offset(label), 0);
    }

    [Fact]
    public void AtTheEndOfARound_TheTextBeginsTheNextOne_WithoutAStop()
    {
        using ThemedLabel label = Create();
        label.ScrollSpeed = LabelScrollSpeed.Fast;
        int cycle = Cycle(label);
        float previous = 0f;
        bool wrapped = false;
        float stoodStill = 0f;

        for (int i = 0; i < 2000 && !wrapped; i++)
        {
            Tick(label, 20);
            float now = Offset(label);

            if (now < previous)
                wrapped = true;

            if (now == previous && !wrapped)
                stoodStill++;

            previous = now;
        }

        Assert.True(wrapped, "the text never began a new round");
        Assert.Equal(0f, stoodStill);
        Assert.True(Offset(label) < cycle && Offset(label) >= 0f);
    }

    [Fact]
    public void TheOffset_NeverReachesTheLengthOfARound()
    {
        using ThemedLabel label = Create();
        label.ScrollSpeed = LabelScrollSpeed.Fast;
        int cycle = Cycle(label);

        for (int i = 0; i < 1000; i++)
        {
            Tick(label, 37);

            Assert.InRange(Offset(label), 0f, cycle - 0.0001f);
        }
    }

    [Fact]
    public void ToTheRight_ThePictureIsTheOneToTheLeft_OfTheRestOfTheRound()
    {
        using ThemedLabel left = Create(LabelScrollDirection.Left);
        using ThemedLabel right = Create(LabelScrollDirection.Right);
        int cycle = Cycle(left);
        Assert.Equal(cycle, Cycle(right));

        foreach (float offset in new[] { 0f, 10f, cycle / 3f, cycle / 2f, cycle - 5f })
        {
            SetOffset(left, offset);
            SetOffset(right, (cycle - offset) % cycle);

            using Bitmap first = Paint(left);
            using Bitmap second = Paint(right);

            Assert.True(Same(first, second), "the two ways do not meet at " + offset);
        }
    }

    [Fact]
    public void ToTheRight_ThePictureChangesWithTheOffset()
    {
        using ThemedLabel right = Create(LabelScrollDirection.Right);

        SetOffset(right, 0f);
        using Bitmap start = Paint(right);
        SetOffset(right, 30f);
        using Bitmap later = Paint(right);

        Assert.False(Same(start, later));
    }

    [Fact]
    public void ThePictureOfARound_HoldsTheSeparator_InTheGapAfterTheText()
    {
        // The ink in the columns where the separator is: the text of the label has moved so that its end
        // is at x = 20, the gap follows, then the separator.
        int InkOfTheSeparator(LabelScrollSeparator separator)
        {
            using ThemedLabel label = Create(separator: separator);
            int textWidth = label.GetPrivateField<int>("_textWidth");
            int inset = label.InvokePrivate<int>("GetTextInset");
            int gap = label.InvokePrivate<int>("GetGap");
            SetOffset(label, inset + textWidth - 20f);

            using Bitmap bitmap = Paint(label);
            int ink = 0;

            for (int x = 20 + gap + 1; x < 20 + gap + 4 && x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y).GetBrightness() > 0.35f)
                        ink++;
                }
            }

            return ink;
        }

        Assert.True(InkOfTheSeparator(LabelScrollSeparator.Dot) > 0, "no dot in the gap");
        Assert.True(InkOfTheSeparator(LabelScrollSeparator.Bar) > 0, "no bar in the gap");
        Assert.True(InkOfTheSeparator(LabelScrollSeparator.Diamond) > 0, "no diamond in the gap");
        Assert.Equal(0, InkOfTheSeparator(LabelScrollSeparator.None));
    }

    [Fact]
    public void TheTwoRounds_CoverTheLabel_AtEveryPlace()
    {
        using ThemedLabel label = Create();
        int cycle = Cycle(label);

        // Whatever the place, the label shows text (ink) in its middle part.
        for (float offset = 0f; offset < cycle; offset += cycle / 40f)
        {
            SetOffset(label, offset);
            using Bitmap bitmap = Paint(label);
            int ink = 0;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y).GetBrightness() > 0.45f)
                        ink++;
                }
            }

            // (At the gap with a dot there is still the dot or the start of the text: some ink anywhere.)
            Assert.True(ink > 0, "nothing shown at " + offset);
        }
    }

    [Fact]
    public void ChangingTheMode_TheDirection_OrTheSeparator_StartsTheMovementOver()
    {
        using ThemedLabel label = Create();
        Tick(label, 1000);
        Assert.True(Offset(label) > 0f);

        label.ScrollDirection = LabelScrollDirection.Right;
        Assert.Equal(0f, Offset(label));

        Tick(label, 1000);
        label.ScrollSeparator = LabelScrollSeparator.Bar;
        Assert.Equal(0f, Offset(label));

        Tick(label, 1000);
        label.ScrollMode = LabelScrollMode.BackAndForth;
        Assert.Equal(0f, Offset(label));
        Assert.Equal(0, Cycle(label));
        Assert.True(MaxOffset(label) > 0);
    }

    [Fact]
    public void BackAndForth_StillRestsFirst_AfterTheModeWentThroughTheEndlessOne()
    {
        using ThemedLabel label = Create();
        label.ScrollMode = LabelScrollMode.BackAndForth;

        Tick(label, 2000);
        Assert.Equal(0f, Offset(label));
    }

    [Fact]
    public void ADisabledLabel_HasNoRound_AndAnEnabledOneHasItAgain()
    {
        using ThemedLabel label = Create();

        label.Enabled = false;
        Assert.Equal(0, Cycle(label));
        Assert.False(Moving(label));

        label.Enabled = true;
        Assert.True(Cycle(label) > 0);
        Assert.True(Moving(label));
    }

    [Fact]
    public void AMultiLineText_AndAnAutoSizedLabel_DoNotMove_InTheEndlessModeToo()
    {
        using ThemedLabel lines = Create(text: LongText + "\nSecond line");
        using ThemedLabel autoSized = Create();
        autoSized.AutoSize = true;

        Assert.Equal(0, Cycle(lines));
        Assert.Equal(0, Cycle(autoSized));
    }

    [Fact]
    public void ANewText_ANewWidth_AndANewFont_MakeANewRound()
    {
        using ThemedLabel label = Create();
        int first = Cycle(label);

        label.Text = LongText + " and a good deal more text";
        int longer = Cycle(label);

        label.Font = UIFonts.Title;
        int bigger = Cycle(label);

        Assert.True(longer > first);
        Assert.True(bigger > longer);

        label.Width = 4000;
        Assert.Equal(0, Cycle(label));
    }

    [Fact]
    public void TheSeparatorSigns_AreADotABarADiamondAndNothing()
    {
        Assert.Equal("•", LabelScrollSeparator.Dot.ToText());
        Assert.Equal("|", LabelScrollSeparator.Bar.ToText());
        Assert.Equal("◆", LabelScrollSeparator.Diamond.ToText());
        Assert.Equal("", LabelScrollSeparator.None.ToText());
        Assert.Equal("•", ((LabelScrollSeparator)99).ToText());
    }

    [Fact]
    public void AllFourKinds_CanMoveEndlessly()
    {
        foreach (ThemedLabel label in new[]
        {
            UIStyles.Labels.CreateStandard(LongText), UIStyles.Labels.CreatePrimary(LongText),
            UIStyles.Labels.CreateTitle(LongText), UIStyles.Labels.CreateMuted(LongText)
        })
        {
            using (label)
            {
                label.Size = new Size(120, 24);
                label.ScrollMode = LabelScrollMode.Endless;

                Assert.True(label.GetPrivateField<int>("_cycle") >= 0);
            }
        }
    }
}
