using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// What the moving text respects: the animation setting of Windows (no movement where the user turned
/// animations off, and the change is followed at once), and the mouse (an option: the text stands still while
/// the mouse is on the label). Plus the color set of the title and the muted label. The time is a clock the tests
/// set. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelScrollOptionsTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly Func<long> _originalClock = ThemedLabel.Milliseconds;
    private readonly Func<bool> _originalAnimations = ThemedLabel.AnimationsEnabled;
    private readonly List<Form> _forms = new List<Form>();
    private long _now = 1_000_000;

    public LabelScrollOptionsTests()
    {
        ThemedLabel.Milliseconds = () => _now;
    }

    public void Dispose()
    {
        ThemedLabel.Milliseconds = _originalClock;
        ThemedLabel.AnimationsEnabled = _originalAnimations;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private ThemedLabel Create(LabelScroll scroll, string text = LongText)
    {
        ThemedLabel label = UIStyles.Labels.CreateStandard(text, scroll: scroll);
        label.Size = new Size(120, 24);

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

    private static bool Moving(ThemedLabel label) => label.GetPrivateField<bool>("_animating");

    private void Tick(ThemedLabel label, int milliseconds)
    {
        _now += milliseconds;
        label.InvokePrivate("OnFrame", label, EventArgs.Empty);
    }

    private static Color TextColor(Label label) => label.InvokePrivate<Color>("GetTextColor");

    // ---- the animation setting of Windows ----

    [Fact]
    public void ReadingTheSettingOfWindows_Works_AndSaysYesOrNo()
    {
        Assert.Null(Record.Exception(() => ReadSystemSetting()));
    }

    // Reads the same setting directly, as the label does.
    private static bool ReadSystemSetting()
    {
        int on = 1;

        return !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) || on != 0;
    }

    [Fact]
    public void TheDefault_IsTheSettingOfWindows()
    {
        Assert.Equal(ReadSystemSetting(), ThemedLabel.AnimationsEnabled());
    }

    [Theory]
    [InlineData(LabelScrollMode.BackAndForth)]
    [InlineData(LabelScrollMode.Endless)]
    public void WhereTheAnimationsAreOff_TheTextDoesNotMove_AndEndsInAnEllipsis(LabelScrollMode mode)
    {
        ThemedLabel.AnimationsEnabled = () => false;
        ThemedLabel scrollingLabel = Create(mode == LabelScrollMode.Endless ? LabelScroll.Endless() : LabelScroll.BackAndForth());

        using (scrollingLabel)
        {
            Assert.Equal(mode, scrollingLabel.ScrollMode);
            Assert.False(Moving(scrollingLabel));
            Assert.Equal(0, scrollingLabel.GetPrivateField<int>("_maxOffset"));
            Assert.Equal(0, scrollingLabel.GetPrivateField<int>("_cycle"));
            Assert.True(scrollingLabel.AutoEllipsis);
        }
    }

    [Fact]
    public void WhenWindowsChangesTheSetting_TheTextStopsAtOnce_AndMovesAgainWhenItIsBackOn()
    {
        bool animations = true;
        ThemedLabel.AnimationsEnabled = () => animations;
        using ThemedLabel label = Create(LabelScroll.Endless());
        Assert.True(Moving(label));

        // Windows tells the labels through LabelSettingsWatcher (a window hears of a setting only for itself).
        animations = false;
        LabelSettingsWatcher.Raise();
        PumpUntil(() => !Moving(label));
        Assert.False(Moving(label));

        animations = true;
        LabelSettingsWatcher.Raise();
        PumpUntil(() => Moving(label));
        Assert.True(Moving(label));
    }

    private static void PumpUntil(Func<bool> reached)
    {
        for (int i = 0; i < 100 && !reached(); i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    // The labels that could move wait for the system; the others do not, and the watcher is off then.
    [Fact]
    public void OnlyLabelsThatCouldMove_WaitForTheSettingsOfWindows()
    {
        int before = LabelSettingsWatcher.Count;
        ThemedLabel idle = Create(LabelScroll.None);
        Assert.Equal(before, LabelSettingsWatcher.Count);

        ThemedLabel moving = Create(LabelScroll.Endless());
        Assert.Equal(before + 1, LabelSettingsWatcher.Count);

        // Also one whose text fits: the setting may matter when it is made smaller.
        ThemedLabel fits = Create(LabelScroll.BackAndForth(), text: "Short");
        Assert.Equal(before + 2, LabelSettingsWatcher.Count);

        moving.ScrollMode = LabelScrollMode.None;
        Assert.Equal(before + 1, LabelSettingsWatcher.Count);

        moving.Dispose();
        fits.Dispose();
        idle.Dispose();
        Assert.Equal(before, LabelSettingsWatcher.Count);
    }

    [Fact]
    public void AFixedLabel_ThatHasTheAnimationsSwitchedOnLater_StartsToMove()
    {
        bool animations = false;
        ThemedLabel.AnimationsEnabled = () => animations;
        using ThemedLabel label = Create(LabelScroll.BackAndForth());
        Assert.False(Moving(label));

        animations = true;
        LabelSettingsWatcher.Raise();
        PumpUntil(() => Moving(label));

        Assert.True(Moving(label));
    }

    // ---- the mouse ----

    private static void MouseEnters(ThemedLabel label) => label.InvokePrivate("OnMouseEnter", EventArgs.Empty);

    private static void MouseLeaves(ThemedLabel label) => label.InvokePrivate("OnMouseLeave", EventArgs.Empty);

    [Fact]
    public void ByDefault_TheMouseChangesNothing()
    {
        using ThemedLabel label = Create(LabelScroll.Endless());
        Assert.False(label.ScrollPauseOnHover);

        MouseEnters(label);
        Tick(label, 1000);

        Assert.Equal(45f, Offset(label), 0);
    }

    [Fact]
    public void WithThePauseOnHover_TheEndlessTextStandsStill_WhileTheMouseIsOnTheLabel()
    {
        using ThemedLabel label = Create(LabelScroll.Endless(pauseOnHover: true));
        Tick(label, 1000);
        float before = Offset(label);
        Assert.True(before > 0f);

        MouseEnters(label);
        Tick(label, 3000);
        Tick(label, 3000);

        Assert.Equal(before, Offset(label));

        // It goes on from where it stood, not from where it would be.
        MouseLeaves(label);
        Tick(label, 1000);

        Assert.Equal(before + 45f, Offset(label), 0);
    }

    [Fact]
    public void WithThePauseOnHover_TheBackAndForthTextStandsStill_AndItsRestIsNotUsedUp()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(LabelScrollSpeed.Medium, LabelScrollPause.Short, pauseOnHover: true));

        // The rest is a second; half of it has passed, then the mouse comes.
        Tick(label, 500);
        MouseEnters(label);
        Tick(label, 5000);
        Assert.Equal(0f, Offset(label));

        // The mouse goes: half a second of the rest is left.
        MouseLeaves(label);
        Tick(label, 400);
        Assert.Equal(0f, Offset(label));

        Tick(label, 1000);
        Assert.True(Offset(label) > 0f, "the text did not move after the rest");
    }

    [Fact]
    public void WithThePauseOnHover_TheBackAndForthTextStandsStill_InTheMiddleOfTheWay()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short, pauseOnHover: true));
        Tick(label, 1000);
        Tick(label, 1000);
        float before = Offset(label);
        Assert.True(before > 0f);

        MouseEnters(label);
        Tick(label, 2000);

        Assert.Equal(before, Offset(label));
    }

    [Fact]
    public void TheMouse_OnALabelThatDoesNotWaitForIt_ChangesNothing()
    {
        using ThemedLabel label = Create(LabelScroll.BackAndForth(pauseOnHover: false));
        Tick(label, 2200);

        MouseEnters(label);
        Tick(label, 1000);

        Assert.Equal(45f, Offset(label), 0);
    }

    [Fact]
    public void ThePauseOnHover_IsPartOfTheScroll_AndComesBackFromIt()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard("x", scroll: LabelScroll.Endless(pauseOnHover: true));
        Assert.True(label.ScrollPauseOnHover);
        Assert.True(label.Scroll.PauseOnHover);

        label.Scroll = LabelScroll.BackAndForth(pauseOnHover: false);
        Assert.False(label.ScrollPauseOnHover);

        label.ScrollPauseOnHover = true;
        Assert.True(label.Scroll.PauseOnHover);
        Assert.False(LabelScroll.None.PauseOnHover);
        Assert.True(LabelScroll.BackAndForth(pauseOnHover: true).PauseOnHover);
        Assert.True(LabelScroll.Endless(pauseOnHover: true).PauseOnHover);
    }

    // ---- the colors of the title and the muted label ----

    [Fact]
    public void TheTitle_TakesAColorSet_LikeTheStandardLabel()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;

        using ThemedLabel title = UIStyles.Labels.CreateTitle("x", set);
        using ThemedLabel standard = UIStyles.Labels.CreateStandard("x", set);

        using Panel host = new Panel { BackColor = UIColors.BackgroundMedium };
        host.Controls.AddRange(new Control[] { title, standard });

        Assert.Equal(TextColor(standard), TextColor(title));
        Assert.NotEqual(UIColors.TextPrimary, TextColor(title));
        Assert.Equal(UIFonts.Title, title.Font);
    }

    [Fact]
    public void TheMutedLabel_WithASet_IsDimmed_StillReadable_AndNeverLouderThanTheStandardOne()
    {
        List<string> problems = new List<string>();

        try
        {
            foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
            {
                UIColors.ApplyTheme(theme);

                foreach (System.Reflection.PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    UIColorSet set = (UIColorSet)property.GetValue(null)!;
                    using ThemedLabel muted = UIStyles.Labels.CreateMuted("x", set);
                    using ThemedLabel standard = UIStyles.Labels.CreateStandard("x", set);
                    using Panel host = new Panel { BackColor = UIColors.BackgroundMedium };
                    host.Controls.AddRange(new Control[] { muted, standard });

                    double quiet = ColorContrastHelper.GetContrastRatio(TextColor(muted), UIColors.BackgroundMedium);
                    double loud = ColorContrastHelper.GetContrastRatio(TextColor(standard), UIColors.BackgroundMedium);

                    if (quiet < 2.99)
                        problems.Add(property.Name + ": too faint " + quiet.ToString("0.0"));

                    if (quiet > loud + 0.01)
                        problems.Add(property.Name + ": the muted label is louder (" + quiet.ToString("0.0") + " vs " + loud.ToString("0.0") + ")");
                }
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(problems.Count == 0, string.Join(", ", problems));
    }

    [Fact]
    public void TheMutedLabel_WithASet_IsNotTheColorOfTheStandardOne()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using ThemedLabel muted = UIStyles.Labels.CreateMuted("x", set);
        using ThemedLabel standard = UIStyles.Labels.CreateStandard("x", set);
        using Panel host = new Panel { BackColor = UIColors.BackgroundMedium };
        host.Controls.AddRange(new Control[] { muted, standard });

        Assert.NotEqual(TextColor(standard), TextColor(muted));
        Assert.Equal(UIFonts.Small, muted.Font);
    }

    [Fact]
    public void WithoutASet_TheTitleAndTheMutedLabel_KeepTheirOwnColors()
    {
        using ThemedLabel title = UIStyles.Labels.CreateTitle("x");
        using ThemedLabel muted = UIStyles.Labels.CreateMuted("x", null);

        Assert.Equal(UIColors.TextPrimary, TextColor(title));
        Assert.Equal(UIColors.TextMuted, TextColor(muted));
    }

    [Fact]
    public void ADisabledTitleWithASet_IsTheGrayOfTheLibrary()
    {
        using ThemedLabel title = UIStyles.Labels.CreateTitle("x", UIStyles.Colors.Sets.Green);
        title.Enabled = false;

        Assert.Equal(UIColors.DisabledGray, TextColor(title));
    }

    [Fact]
    public void TheTitleTakesTheSetBeforeTheScroll_AsTheStandardLabelDoes()
    {
        using ThemedLabel title = UIStyles.Labels.CreateTitle("x", UIStyles.Colors.Sets.Green, LabelScroll.Endless());
        using ThemedLabel muted = UIStyles.Labels.CreateMuted("x", scroll: LabelScroll.BackAndForth());

        Assert.Equal(LabelScrollMode.Endless, title.ScrollMode);
        Assert.Equal(LabelScrollMode.BackAndForth, muted.ScrollMode);
    }
}
