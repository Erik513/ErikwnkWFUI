using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The moving text at its limits: a text that just fits, a label of no size or with more padding than room,
/// a text too long for a picture, a label that is moved to another window, many labels at once, labels that
/// are made and disposed while the clock runs, changes during the movement. The time is a clock the tests set.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelScrollEdgeCaseTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly Func<long> _originalClock = ThemedLabel.Milliseconds;
    private readonly List<Form> _forms = new List<Form>();
    private long _now = 1_000_000;

    public LabelScrollEdgeCaseTests()
    {
        ThemedLabel.Milliseconds = () => _now;
    }

    public void Dispose()
    {
        ThemedLabel.Milliseconds = _originalClock;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private Form NewForm()
    {
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(40, 40, 40),
            ClientSize = new Size(400, 120)
        };
        form.Show();
        _forms.Add(form);

        return form;
    }

    private ThemedLabel Create(LabelScroll scroll, string text = LongText, int width = 120)
    {
        ThemedLabel label = UIStyles.Labels.CreateStandard(text, scroll: scroll);
        label.Bounds = new Rectangle(5, 5, width, 24);
        NewForm().Controls.Add(label);

        return label;
    }

    private static float Offset(ThemedLabel label) => label.GetPrivateField<float>("_offset");

    private static int MaxOffset(ThemedLabel label) => label.GetPrivateField<int>("_maxOffset");

    private static int Cycle(ThemedLabel label) => label.GetPrivateField<int>("_cycle");

    private static bool Moving(ThemedLabel label) => label.GetPrivateField<bool>("_animating");

    private void Tick(ThemedLabel label, int milliseconds)
    {
        _now += milliseconds;
        label.InvokePrivate("OnFrame", label, EventArgs.Empty);
    }

    private static Bitmap Paint(ThemedLabel label)
    {
        Bitmap bitmap = new Bitmap(Math.Max(1, label.Width), Math.Max(1, label.Height));
        label.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

        return bitmap;
    }

    public static IEnumerable<object[]> BothModes()
    {
        yield return new object[] { LabelScrollMode.BackAndForth };
        yield return new object[] { LabelScrollMode.Endless };
    }

    private static LabelScroll For(LabelScrollMode mode) => mode == LabelScrollMode.Endless ? LabelScroll.Endless() : LabelScroll.BackAndForth();

    // ---- the text that just fits ----

    // (The endless text moves whether it fits or not.)
    [Fact]
    public void AText_ThatJustFits_StandsStill()
    {
        LabelScrollMode mode = LabelScrollMode.BackAndForth;
        using ThemedLabel probe = Create(For(mode));
        int needed = probe.GetPrivateField<int>("_textWidth") + 2 * probe.InvokePrivate<int>("GetTextInset");

        using ThemedLabel label = Create(For(mode), width: needed);

        Assert.Equal(0, MaxOffset(label));
        Assert.Equal(0, Cycle(label));
        Assert.False(Moving(label));
    }

    [Theory]
    [MemberData(nameof(BothModes))]
    public void AText_OnePixelTooLong_Moves(LabelScrollMode mode)
    {
        using ThemedLabel probe = Create(For(mode));
        int needed = probe.GetPrivateField<int>("_textWidth") + 2 * probe.InvokePrivate<int>("GetTextInset");

        using ThemedLabel label = Create(For(mode), width: needed - 1);

        Assert.True(MaxOffset(label) > 0 || Cycle(label) > 0);
        Assert.True(Moving(label));
    }

    // ---- no room ----

    [Theory]
    [MemberData(nameof(BothModes))]
    public void ALabelOfNoSize_OrWithMorePaddingThanRoom_DoesNotFail(LabelScrollMode mode)
    {
        using ThemedLabel empty = Create(For(mode), width: 0);
        using ThemedLabel padded = Create(For(mode));
        padded.Padding = new Padding(200, 0, 200, 0);

        Assert.Null(Record.Exception(() =>
        {
            // (A control of no width cannot be drawn into a picture: it is only brought to paint.)
            empty.Refresh();
            using Bitmap second = Paint(padded);

            for (int i = 0; i < 5; i++)
            {
                Tick(empty, 500);
                Tick(padded, 500);
            }
        }));
    }

    [Theory]
    [MemberData(nameof(BothModes))]
    public void AnEmptyText_AndAText_OfOnlySpaces_DoNotMove(LabelScrollMode mode)
    {
        using ThemedLabel empty = Create(For(mode), text: "");
        using ThemedLabel spaces = Create(For(mode), text: "     ");

        Assert.False(Moving(empty));
        Assert.False(Moving(spaces));
    }

    // ---- too long for a picture ----

    [Theory]
    [MemberData(nameof(BothModes))]
    public void AText_TooLongForAPicture_EndsInAnEllipsis_WithoutAnError(LabelScrollMode mode)
    {
        string huge = string.Concat(Enumerable.Repeat("Wide text, wide text, wide text. ", 800));

        using ThemedLabel label = Create(For(mode), text: huge);

        Assert.Equal(0, MaxOffset(label));
        Assert.Equal(0, Cycle(label));
        Assert.False(Moving(label));
        Assert.Null(Record.Exception(() => Paint(label).Dispose()));
    }

    // ---- ampersand and tab in the moving text ----

    [Fact]
    public void AnAmpersand_IsPartOfTheMovingText_AsLongAsMnemonicsAreOff()
    {
        using ThemedLabel plain = Create(LabelScroll.BackAndForth(), text: "Fish and Chips and a good deal more text to run");
        using ThemedLabel withAmpersand = Create(LabelScroll.BackAndForth(), text: "Fish & Chips & a good deal more text to run");

        // The ampersands take room: the text with them is wider than the one with "and" written differently.
        Assert.True(withAmpersand.GetPrivateField<int>("_textWidth") != plain.GetPrivateField<int>("_textWidth"));
        Assert.True(withAmpersand.GetPrivateField<int>("_textWidth") > 100);
    }

    [Fact]
    public void WithMnemonics_TheAmpersandIsNotInTheMovingText_AsSoonAsTheLabelIsPainted()
    {
        using ThemedLabel off = Create(LabelScroll.BackAndForth(), text: "&Open the file and read all of it, from top to bottom");
        using ThemedLabel on = Create(LabelScroll.BackAndForth(), text: "&Open the file and read all of it, from top to bottom");
        int before = on.GetPrivateField<int>("_textWidth");

        // UseMnemonic does not tell the label; it sees it when it paints.
        on.UseMnemonic = true;
        using Bitmap painted = Paint(on);

        Assert.True(on.GetPrivateField<int>("_textWidth") < before);
        Assert.True(on.GetPrivateField<int>("_textWidth") < off.GetPrivateField<int>("_textWidth"));
    }

    [Fact]
    public void SwitchingMnemonicsBack_BringsTheAmpersandBack()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(), text: "&Open the file and read all of it, from top to bottom");
        int withAmpersand = label.GetPrivateField<int>("_textWidth");

        label.UseMnemonic = true;
        using Bitmap first = Paint(label);
        label.UseMnemonic = false;
        using Bitmap second = Paint(label);

        Assert.Equal(withAmpersand, label.GetPrivateField<int>("_textWidth"));
    }

    [Fact]
    public void AMnemonicThatMakesTheTextFit_StopsTheMovement()
    {
        // Text that fits only without its ampersands: "&&&&&&" is six signs, but with mnemonics only half.
        using ThemedLabel probe = Create(LabelScroll.BackAndForth(), text: "Fits");
        int fits = probe.GetPrivateField<int>("_textWidth") + 2 * probe.InvokePrivate<int>("GetTextInset");

        using ThemedLabel label = Create(LabelScroll.BackAndForth(), text: "F&i&t&s", width: fits);
        Assert.True(Moving(label), "with the ampersands the text does not fit");

        label.UseMnemonic = true;
        using Bitmap painted = Paint(label);

        Assert.False(Moving(label));
    }

    // ---- a label that is moved around ----

    [Theory]
    [MemberData(nameof(BothModes))]
    public void ALabel_TakenToAnotherWindow_MovesThere_AndLeavesTheClockWhileItHasNone(LabelScrollMode mode)
    {
        Form first = NewForm();
        Form second = NewForm();
        ThemedLabel label = UIStyles.Labels.CreateStandard(LongText, scroll: For(mode));
        label.Bounds = new Rectangle(5, 5, 120, 24);

        // Not in a window yet: no handle, no movement.
        Assert.False(Moving(label));

        first.Controls.Add(label);
        Assert.True(Moving(label));

        first.Controls.Remove(label);
        second.Controls.Add(label);
        Assert.True(Moving(label));

        second.Controls.Remove(label);
        label.Dispose();
        Assert.False(Moving(label));
    }

    [Theory]
    [MemberData(nameof(BothModes))]
    public void WhenTheWindowIsDisposed_TheLabelsInItLeaveTheClock(LabelScrollMode mode)
    {
        int before = AnimationClock.Count;
        Form form = NewForm();

        for (int i = 0; i < 5; i++)
        {
            ThemedLabel label = UIStyles.Labels.CreateStandard(LongText, scroll: For(mode));
            label.Bounds = new Rectangle(5, 5 + i * 25, 120, 24);
            form.Controls.Add(label);
        }

        Assert.Equal(before + 5, AnimationClock.Count);

        form.Dispose();

        Assert.Equal(before, AnimationClock.Count);
    }

    [Fact]
    public void ALabelThatIsHiddenWithItsWindow_LeavesTheClock_AndComesBackWithIt()
    {
        Form form = NewForm();
        ThemedLabel label = UIStyles.Labels.CreateStandard(LongText, scroll: LabelScroll.Endless());
        label.Bounds = new Rectangle(5, 5, 120, 24);
        form.Controls.Add(label);
        Assert.True(Moving(label));

        // The window does not tell its labels that it is hidden: at the next beat the label finds out.
        form.Hide();
        label.RequestFrame();

        for (int i = 0; i < 100 && Moving(label); i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.False(Moving(label));

        form.Show();
        Assert.True(Moving(label));
    }

    // ---- many labels, made and disposed ----

    [Fact]
    public void ManyLabels_AreAllOnTheClock_AndAllLeaveIt()
    {
        int before = AnimationClock.Count;
        Form form = NewForm();
        form.ClientSize = new Size(400, 1300);
        List<ThemedLabel> labels = new List<ThemedLabel>();

        for (int i = 0; i < 50; i++)
        {
            ThemedLabel label = UIStyles.Labels.CreateStandard(LongText + " " + i, scroll: i % 2 == 0 ? LabelScroll.Endless() : LabelScroll.BackAndForth());
            label.Bounds = new Rectangle(5, 5 + i * 25, 120, 24);
            form.Controls.Add(label);
            labels.Add(label);
        }

        Assert.Equal(before + 50, AnimationClock.Count);

        foreach (ThemedLabel label in labels)
            label.Dispose();

        Assert.Equal(before, AnimationClock.Count);
    }

    [Fact]
    public void LabelsMadeAndDisposed_WhileTheClockRuns_DoNotDisturbIt()
    {
        Form form = NewForm();
        Exception? thrown = null;

        try
        {
            for (int i = 0; i < 100; i++)
            {
                ThemedLabel label = UIStyles.Labels.CreateStandard(LongText, scroll: LabelScroll.Endless());
                label.Bounds = new Rectangle(5, 5, 120, 24);
                form.Controls.Add(label);
                Application.DoEvents();
                label.Dispose();
            }
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        Assert.Null(thrown);
    }

    [Fact]
    public void TheClock_StopsItsThreadWhenNoLabelMoves_AndStartsAnotherWhenOneDoes()
    {
        FieldInfo thread = typeof(AnimationClock).GetField("_thread", BindingFlags.NonPublic | BindingFlags.Static)!;

        ThemedLabel first = Create(LabelScroll.Endless());
        first.Dispose();

        for (int i = 0; i < 100 && thread.GetValue(null) != null; i++)
            Thread.Sleep(10);

        Assert.Null(thread.GetValue(null));

        using ThemedLabel second = Create(LabelScroll.Endless());
        Assert.NotNull(thread.GetValue(null));

        // The new thread really delivers.
        _now += 1000;
        for (int i = 0; i < 100 && Offset(second) == 0f; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(Offset(second) > 0f, "the new thread did not deliver a beat");
    }

    // ---- changes during the movement ----

    [Fact]
    public void ANewSpeed_TakesEffectAtOnce_WithoutRestarting()
    {
        using ThemedLabel label = Create(LabelScroll.Endless());
        Tick(label, 1000);
        float slowStep = Offset(label);

        label.ScrollSpeed = LabelScrollSpeed.Fast;
        Tick(label, 1000);

        Assert.Equal(slowStep + 90f, Offset(label), 0);
    }

    [Fact]
    public void ANewForeColor_ChangesThePictureOfTheMovingText()
    {
        using ThemedLabel label = Create(LabelScroll.Endless());
        label.ScrollSeparator = LabelScrollSeparator.None;
        using Bitmap before = Paint(label);

        label.ForeColor = Color.Red;
        using Bitmap after = Paint(label);

        Assert.False(SamePixels(before, after));

        bool red = false;

        for (int y = 0; y < after.Height && !red; y++)
        {
            for (int x = 0; x < after.Width && !red; x++)
                red = after.GetPixel(x, y).R > 150 && after.GetPixel(x, y).G < 80;
        }

        Assert.True(red, "no red text after the change");
    }

    [Fact]
    public void ANewTextAlign_MovesTheMovingTextUpOrDown()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth());
        label.Height = 60;
        label.TextAlign = ContentAlignment.TopLeft;
        using Bitmap top = Paint(label);

        label.TextAlign = ContentAlignment.BottomLeft;
        using Bitmap bottom = Paint(label);

        Assert.False(SamePixels(top, bottom));
    }

    [Fact]
    public void AnOpaqueBackColor_IsKeptBehindTheMovingText()
    {
        using ThemedLabel label = Create(LabelScroll.Endless());
        label.BackColor = Color.FromArgb(120, 20, 20);
        using Bitmap painted = Paint(label);

        Assert.Equal(label.BackColor.ToArgb(), painted.GetPixel(0, painted.Height - 1).ToArgb());
    }

    [Fact]
    public void AnImage_IsNotShownWhileTheTextMoves_ButAfterItStops()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth());
        using Bitmap image = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(image))
            g.Clear(Color.Orange);

        label.Image = image;
        label.ImageAlign = ContentAlignment.MiddleRight;
        using Bitmap whileMoving = Paint(label);

        label.ScrollMode = LabelScrollMode.None;
        using Bitmap standing = Paint(label);

        bool orangeWhileMoving = whileMoving.GetPixel(whileMoving.Width - 8, whileMoving.Height / 2).R > 200;
        bool orangeStanding = standing.GetPixel(standing.Width - 8, standing.Height / 2).R > 200;

        Assert.False(orangeWhileMoving);
        Assert.True(orangeStanding);
    }

    [Fact]
    public void AutoSize_SwitchedOnWhileMoving_StopsTheMovement()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth());
        Assert.True(Moving(label));

        label.AutoSize = true;

        Assert.False(Moving(label));
    }

    // ---- after the end ----

    [Fact]
    public void SettingsOnALabelThatIsDisposed_AreHarmless_AndItStaysOffTheClock()
    {
        ThemedLabel label = Create(LabelScroll.Endless());
        int before = AnimationClock.Count;
        label.Dispose();
        label.Dispose();

        Assert.Null(Record.Exception(() =>
        {
            label.Scroll = LabelScroll.BackAndForth();
            label.ScrollMode = LabelScrollMode.Endless;
            label.ScrollSpeed = LabelScrollSpeed.Fast;
            label.Text = "Another text that is long enough to move around in the label";
        }));

        Assert.False(Moving(label));
        Assert.Equal(before - 1, AnimationClock.Count);
    }

    // ---- a mouse that stays, a label that is resized ----

    [Fact]
    public void ALongHover_AfterTheRestWasOver_DoesNotMakeTheNextRestLonger()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short, pauseOnHover: true));
        Tick(label, 1000);
        Tick(label, 500);
        float partway = Offset(label);
        Assert.True(partway > 0f);

        // The mouse stays on the label for a long time, while the text is not at rest.
        label.InvokePrivate("OnMouseEnter", EventArgs.Empty);

        for (int i = 0; i < 200; i++)
            Tick(label, 100);

        Assert.Equal(partway, Offset(label));

        // It goes on at once when the mouse has left, without a rest that the hover has made.
        label.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        Tick(label, 100);

        Assert.True(Offset(label) > partway, "the text stayed at rest after the hover");
    }

    [Fact]
    public void ALabelThatIsHiddenOrDisabled_IsNotHoveredAnyMore()
    {
        using ThemedLabel label = Create(LabelScroll.Endless(pauseOnHover: true));
        FieldInfo hovered = typeof(ThemedLabel).GetField("_hovered", BindingFlags.NonPublic | BindingFlags.Instance)!;

        label.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        label.Visible = false;
        Assert.False((bool)hovered.GetValue(label)!);

        label.Visible = true;
        label.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        label.Enabled = false;
        Assert.False((bool)hovered.GetValue(label)!);

        label.Enabled = true;
        Tick(label, 1000);
        Assert.Equal(45f, Offset(label), 0);
    }

    [Fact]
    public void WhenALabelIsResized_BackAndForthKeepsItsPlace_AsFarAsItStillExists()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        Tick(label, 1000);
        Tick(label, 1500);
        float place = Offset(label);
        Assert.True(place > 20f);
        int before = MaxOffset(label);

        // A little narrower: the way is longer, the place stays.
        label.Width -= 10;
        Assert.Equal(before + 10, MaxOffset(label));
        Assert.Equal(place, Offset(label));
        Assert.True(Moving(label));

        // Much narrower or wider: it stays where there is room, and goes on its way.
        label.Width += 40;
        Assert.Equal(Math.Min(place, MaxOffset(label)), Offset(label));
    }

    [Fact]
    public void WhenALabelIsResized_TheEndlessTextKeepsItsPlace()
    {
        using ThemedLabel label = Create(LabelScroll.Endless());
        Tick(label, 2000);
        float place = Offset(label);
        Assert.True(place > 50f);

        for (int i = 0; i < 5; i++)
            label.Width += 3;

        Assert.Equal(place, Offset(label));
    }

    [Fact]
    public void WhenAResizedLabelHasRoomForTheText_ItStopsAtTheStart_AndWhenNarrowAgainItStartsAtTheStart()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        Tick(label, 1000);
        Tick(label, 1500);
        Assert.True(Offset(label) > 0f);

        label.Width = 1500;
        Assert.Equal(0f, Offset(label));
        Assert.False(Moving(label));

        label.Width = 120;
        Assert.Equal(0f, Offset(label));
        Assert.True(Moving(label));
    }
}
