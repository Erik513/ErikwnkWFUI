using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>The knob glides when the user switches, and stands at the other end at once for everything else; and every measure follows the dpi of the screen. [Collection]: the setting of the animations is static state of the process.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToggleSwitchAnimationTests : IDisposable
{
    private readonly Func<bool> _originalAnimations = AnimationSettings.Enabled;

    public ToggleSwitchAnimationTests()
    {
        AnimationSettings.Enabled = () => true;
    }

    public void Dispose()
    {
        AnimationSettings.Enabled = _originalAnimations;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Form Show(ToggleSwitch toggle)
    {
        toggle.TabStop = false;
        return OffscreenForm.Show(toggle, new Size(toggle.Width + 10, toggle.Height + 10), Color.FromArgb(20, 20, 20), new Point(5, 5));
    }

    private static void Click(ToggleSwitch toggle)
    {
        toggle.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 10, 0));
        toggle.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 20, 10, 0));
    }

    private static double Position(ToggleSwitch toggle) => toggle.GetPrivateField<double>("_position");

    private static bool Gliding(ToggleSwitch toggle) => toggle.GetPrivateField<FrameTicker>("_ticker")!.IsRunning;

    // ---- the glide ----

    [Fact]
    public void AClick_MakesTheKnobGlide_AndItEndsAtTheOtherEnd()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        Click(toggle);

        Assert.True(toggle.Checked, "the state is the new one at once");
        Assert.True(Position(toggle) < 1.0, "the knob jumped");
        Assert.True(Gliding(toggle));

        Pump(500);

        Assert.Equal(1.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    private static Rectangle KnobAt(ToggleSwitch toggle, double position)
    {
        toggle.SetPrivateField("_position", position);

        return toggle.InvokePrivate<Rectangle>("GetKnobRectangle", toggle.InvokePrivate<Rectangle>("GetToggleRectangle"));
    }

    [Fact]
    public void WhileItGlides_TheKnobAndTheColorsAreBetweenTheTwoEnds()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);
        using Bitmap off = Draw(toggle);
        int left = KnobAt(toggle, 0.0).X;
        int right = KnobAt(toggle, 1.0).X;

        // (The position is put where it would be halfway; the state is the new one, as after a click.)
        toggle.Checked = true;
        Rectangle knob = KnobAt(toggle, 0.5);
        using Bitmap middle = Draw(toggle);
        KnobAt(toggle, 1.0);
        using Bitmap on = Draw(toggle);

        Assert.InRange(knob.X, left + 1, right - 1);
        Assert.False(SamePixels(off, middle));
        Assert.False(SamePixels(middle, on));
    }

    [Fact]
    public void WhenTheGlideIsOver_ItLooksAsOneThatWasMadeInThatState()
    {
        using ToggleSwitch glided = UIStyles.ToggleSwitches.CreatePrimary(false);
        using ToggleSwitch made = UIStyles.ToggleSwitches.CreatePrimary(true);
        using Form first = Show(glided);
        using Form second = Show(made);

        Click(glided);
        Pump(500);

        using Bitmap one = Draw(glided);
        using Bitmap two = Draw(made);
        Assert.True(SamePixels(one, two));
    }

    [Fact]
    public void ASecondClickWhileItGlides_TurnsTheKnobAroundFromWhereItIs()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        Click(toggle);
        Pump(60);
        double halfway = Position(toggle);
        Assert.InRange(halfway, 0.01, 0.99);

        Click(toggle);
        Assert.False(toggle.Checked);
        Assert.True(Position(toggle) >= halfway - 0.0001 && Position(toggle) < 1.0, "the knob jumped");

        Pump(500);
        Assert.Equal(0.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    // ---- no glide ----

    [Fact]
    public void WithTheAnimationsOffInWindows_TheKnobStandsAtTheOtherEndAtOnce()
    {
        AnimationSettings.Enabled = () => false;
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        Click(toggle);

        Assert.Equal(1.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    [Fact]
    public void SettingCheckedFromCode_StandsTheKnobAtTheEndAtOnce()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        toggle.Checked = true;
        Assert.Equal(1.0, Position(toggle));
        Assert.False(Gliding(toggle));

        toggle.Checked = false;
        Assert.Equal(0.0, Position(toggle));
    }

    [Fact]
    public void SettingCheckedFromCode_WhileItGlides_EndsTheGlideAtTheEndOfTheNewState()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);
        Click(toggle);
        Pump(40);

        toggle.Checked = false;

        Assert.Equal(0.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    [Fact]
    public void AnAnimationSettingSwitchedOffInWindows_EndsAGlideInProgress()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);
        Click(toggle);
        Pump(30);
        Assert.True(Gliding(toggle));

        AnimationSettings.Enabled = () => false;
        AnimationSettings.Raise();
        Pump(100);

        Assert.Equal(1.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    [Fact]
    public void ASwitchThatIsNotShown_DoesNotGlide()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);

        toggle.InvokePrivate("ToggleByUser");

        Assert.True(toggle.Checked);
        Assert.Equal(1.0, Position(toggle));
        Assert.False(Gliding(toggle));
    }

    [Fact]
    public void ADisposedSwitch_ThatWasGliding_LeavesNothingBehind()
    {
        int before = AnimationSettings.Count;
        ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        Form form = Show(toggle);
        Click(toggle);

        form.Dispose();
        toggle.Dispose();
        Exception? failure = Record.Exception(() => Pump(100));

        Assert.Null(failure);
        Assert.Equal(before, AnimationSettings.Count);
    }

    [Fact]
    public void TheKeyboardAndTheScreenReader_MakeTheKnobGlideToo()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Space));
        toggle.InvokePrivate("OnKeyUp", new KeyEventArgs(Keys.Space));
        Assert.True(Gliding(toggle));
        Pump(500);

        toggle.AccessibilityObject.DoDefaultAction();
        Assert.True(Gliding(toggle));
        Pump(500);
        Assert.Equal(0.0, Position(toggle));
    }

    // ---- the dpi ----

    [Fact]
    public void EveryMeasure_FollowsTheDpi_OfTheScreen()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(true, size: new Size(90, 50));
        toggle.DpiOverride = 192;

        Rectangle track = toggle.InvokePrivate<Rectangle>("GetToggleRectangle");
        Rectangle knob = toggle.InvokePrivate<Rectangle>("GetKnobRectangle", track);

        // 2 px padding and 2 px between the track and the knob at 96 dpi: 4 and 4 at 192.
        Assert.Equal(new Rectangle(4, 4, 82, 42), track);
        Assert.Equal(34, knob.Height);
        Assert.Equal(track.Right - 34 - 4, knob.X);

        toggle.DpiOverride = 96;
        Rectangle small = toggle.InvokePrivate<Rectangle>("GetToggleRectangle");
        Assert.Equal(new Rectangle(2, 2, 86, 46), small);
    }

    [Fact]
    public void TheLineAroundTheTrack_IsAsThickAsTheDpiSays()
    {
        using ToggleSwitch one = UIStyles.ToggleSwitches.CreatePrimary(true, size: new Size(90, 50));
        using ToggleSwitch two = UIStyles.ToggleSwitches.CreatePrimary(true, size: new Size(90, 50));
        two.DpiOverride = 192;
        using Form first = Show(one);
        using Form second = Show(two);

        using Bitmap at96 = Draw(one);
        using Bitmap at192 = Draw(two);

        // The same switch twice, as big as the control is: only the measures differ - at 192 the track starts further in.
        Assert.False(SamePixels(at96, at192));
    }

    [Fact]
    public void WithoutASize_TheSwitchIsAsBigAsItsDefaultIsOnThisScreen()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard();

        Assert.Equal(Dpi.Scale(toggle, 45), toggle.Width);
        Assert.Equal(Dpi.Scale(toggle, 25), toggle.Height);
    }

    [Fact]
    public void ASizeThatIsGiven_IsTakenAsItIs()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(size: new Size(70, 40));

        Assert.Equal(new Size(70, 40), toggle.Size);
    }
}
