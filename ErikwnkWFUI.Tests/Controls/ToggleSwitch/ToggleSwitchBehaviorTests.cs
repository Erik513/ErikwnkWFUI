using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>What the switch does with the mouse and the keyboard, as a standard check box does it: the press has to end over it, the space bar works, nothing stays pressed or hovered, and a screen reader is told what it is. [Collection]: the focus belongs to the whole process.</summary>
[Collection(FocusTestCollection.Name)]
public class ToggleSwitchBehaviorTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Form Show(ToggleSwitch toggle)
    {
        Form form = OffscreenForm.Create(new Size(toggle.Width + 10, toggle.Height + 10), Color.FromArgb(20, 20, 20));
        toggle.Location = new Point(5, 5);
        form.Controls.Add(toggle);
        form.Show();
        form.Activate();
        Application.DoEvents();

        return form;
    }

    private static MouseEventArgs Mouse(int x, int y, MouseButtons button = MouseButtons.Left) => new MouseEventArgs(button, 1, x, y, 0);

    private static void Click(ToggleSwitch toggle, MouseButtons button = MouseButtons.Left)
    {
        toggle.InvokePrivate("OnMouseDown", Mouse(20, 10, button));
        toggle.InvokePrivate("OnMouseUp", Mouse(20, 10, button));
    }

    private static void KeyDown(ToggleSwitch toggle, Keys key) => toggle.InvokePrivate("OnKeyDown", new KeyEventArgs(key));

    private static void KeyUp(ToggleSwitch toggle, Keys key) => toggle.InvokePrivate("OnKeyUp", new KeyEventArgs(key));

    private static bool Pressed(ToggleSwitch toggle) => toggle.GetPrivateField<bool>("_isPressed");

    private static bool Hovered(ToggleSwitch toggle) => toggle.GetPrivateField<bool>("_isHovered");

    // ---- the mouse ----

    [Fact]
    public void AClick_Toggles_OnceEachTime_AndRaisesTheEventOnce()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        int events = 0;
        toggle.CheckedChanged += (s, e) => events++;

        Click(toggle);
        Assert.True(toggle.Checked);

        Click(toggle);
        Assert.False(toggle.Checked);
        Assert.Equal(2, events);
    }

    [Fact]
    public void OtherMouseButtons_DoNothing()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        Click(toggle, MouseButtons.Right);
        Click(toggle, MouseButtons.Middle);

        Assert.False(toggle.Checked);
        Assert.False(Pressed(toggle));
    }

    [Fact]
    public void APressThatIsLetGoOutsideTheSwitch_DoesNothing()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("OnMouseDown", Mouse(20, 10));
        toggle.InvokePrivate("OnMouseUp", Mouse(200, 10));

        Assert.False(toggle.Checked);
        Assert.False(Pressed(toggle));
    }

    [Fact]
    public void APressWhoseCaptureIsLost_StaysNoLongerPressed_AndItsReleaseDoesNotToggle()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("OnMouseDown", Mouse(20, 10));
        Assert.True(Pressed(toggle));

        toggle.InvokePrivate("OnMouseCaptureChanged", EventArgs.Empty);
        Assert.False(Pressed(toggle));

        toggle.InvokePrivate("OnMouseUp", Mouse(20, 10));
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void AReleaseWithoutAPress_DoesNotToggle()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("OnMouseUp", Mouse(20, 10));

        Assert.False(toggle.Checked);
    }

    [Fact]
    public void DisablingWhilePressed_EndsThePress_AndTheHoverIsLookedAtAgain()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        toggle.InvokePrivate("OnMouseDown", Mouse(20, 10));
        Assert.True(Pressed(toggle));
        Assert.True(Hovered(toggle));

        toggle.Enabled = false;

        Assert.False(Pressed(toggle));
        Assert.False(Hovered(toggle));
        toggle.InvokePrivate("OnMouseUp", Mouse(20, 10));
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void HidingTheSwitch_ClearsTheHoverAndThePress()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        toggle.InvokePrivate("OnMouseDown", Mouse(20, 10));

        toggle.Visible = false;

        Assert.False(Hovered(toggle));
        Assert.False(Pressed(toggle));
    }

    // ---- the keyboard ----

    [Fact]
    public void TheSpaceBar_Toggles_WhenItIsLetGo_NotWhenItGoesDown()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        int events = 0;
        toggle.CheckedChanged += (s, e) => events++;

        KeyDown(toggle, Keys.Space);
        Assert.False(toggle.Checked);
        Assert.True(Pressed(toggle));

        KeyUp(toggle, Keys.Space);
        Assert.True(toggle.Checked);
        Assert.False(Pressed(toggle));
        Assert.Equal(1, events);
    }

    [Fact]
    public void AHeldSpaceBar_ThatRepeats_TogglesOnlyOnce()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        for (int i = 0; i < 5; i++)
            KeyDown(toggle, Keys.Space);

        KeyUp(toggle, Keys.Space);

        Assert.True(toggle.Checked);
    }

    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.A)]
    [InlineData(Keys.Left)]
    [InlineData(Keys.Back)]
    public void OtherKeys_DoNothing(Keys key)
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        KeyDown(toggle, key);
        KeyUp(toggle, key);

        Assert.False(toggle.Checked);
        Assert.False(Pressed(toggle));
    }

    [Fact]
    public void ASpaceBarThatIsLetGoWithoutItsPress_DoesNotToggle()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        // (The space bar went down in another control, and the focus came here before it was let go.)
        KeyUp(toggle, Keys.Space);

        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ALostFocus_CancelsASpaceBarPress()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        KeyDown(toggle, Keys.Space);
        toggle.InvokePrivate("OnLostFocus", EventArgs.Empty);
        KeyUp(toggle, Keys.Space);

        Assert.False(toggle.Checked);
        Assert.False(Pressed(toggle));
    }

    [Fact]
    public void ADisabledSwitch_IgnoresTheSpaceBar()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.Enabled = false;

        KeyDown(toggle, Keys.Space);
        KeyUp(toggle, Keys.Space);

        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ASpaceBarWithAModifier_IsNotAToggle()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Space | Keys.Control));

        Assert.False(Pressed(toggle));
    }

    // ---- the focus cue ----

    [Fact]
    public void TheFocusFrame_IsShownOnlyWithTheFocusAndAfterTheKeyboardWasUsed()
    {
        StaThread.Run(() =>
        {
            using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(true);
            using Form form = Show(toggle);
            using Bitmap resting = Draw(toggle);

            toggle.Focus();
            Application.DoEvents();
            Assert.True(toggle.Focused);

            // Hidden until the keyboard is used (UIS_CLEAR + UISF_HIDEFOCUS shows the cues, as a key press does).
            using Bitmap withoutCues = Draw(toggle);
            Assert.True(SamePixels(resting, withoutCues), "the frame is there before the keyboard has been used");

            SendMessage(toggle.Handle, 0x0128, (IntPtr)((1 << 16) | 2), IntPtr.Zero);
            Application.DoEvents();
            using Bitmap withCues = Draw(toggle);
            Assert.False(SamePixels(resting, withCues), "no focus frame");
        });
    }

    // ---- what a screen reader is told ----

    [Fact]
    public void AScreenReader_IsToldACheckButton_ItsState_AndHowToToggleIt()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        AccessibleObject accessible = toggle.AccessibilityObject;

        Assert.Equal(AccessibleRole.CheckButton, accessible.Role);
        Assert.Equal(AccessibleStates.None, accessible.State & AccessibleStates.Checked);

        toggle.Checked = true;
        Assert.Equal(AccessibleStates.Checked, accessible.State & AccessibleStates.Checked);

        accessible.DoDefaultAction();
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ADisabledSwitch_HasNoDefaultAction_AndTheAccessibleToggleDoesNothing()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.Enabled = false;

        toggle.AccessibilityObject.DoDefaultAction();

        Assert.False(toggle.Checked);
        Assert.Equal(string.Empty, toggle.AccessibilityObject.DefaultAction);
    }

    [Fact]
    public void TheAccessibleName_IsTheOneThatWasSet()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        toggle.AccessibleName = "Dark mode";

        Assert.Equal("Dark mode", toggle.AccessibilityObject.Name);
    }
}
