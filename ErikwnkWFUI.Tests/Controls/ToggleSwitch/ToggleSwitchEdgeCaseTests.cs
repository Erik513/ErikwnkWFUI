using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>The switch with odd input: real window messages (also a double click), handlers that answer or take it away, odd sizes, the tooltips, and the state of the parent. [Collection]: the setting of the animations is static state of the process.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToggleSwitchEdgeCaseTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;

    private readonly Func<bool> _originalAnimations = AnimationSettings.Enabled;

    public ToggleSwitchEdgeCaseTests()
    {
        // (No glide: the state is read right after each message.)
        AnimationSettings.Enabled = () => false;
    }

    public void Dispose()
    {
        AnimationSettings.Enabled = _originalAnimations;
    }

    private static Form Show(ToggleSwitch toggle)
    {
        return OffscreenForm.Show(toggle, new Size(Math.Max(toggle.Width + 10, 20), Math.Max(toggle.Height + 10, 20)), Color.Black, new Point(5, 5));
    }

    private static IntPtr At(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static void Message(ToggleSwitch toggle, int message, int x = 20, int y = 10) => SendMessage(toggle.Handle, message, IntPtr.Zero, At(x, y));

    // ---- real messages ----

    [Fact]
    public void ARealClick_Toggles_AndASecondOneBringsItBack()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP);
        Assert.True(toggle.Checked);

        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP);
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ADoubleClick_ToggleTwice_AsTwoClicks()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        int events = 0;
        toggle.CheckedChanged += (s, e) => events++;

        // What Windows sends for a double click: down, up, then the second down as a double-click message, up.
        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP);
        Message(toggle, WM_LBUTTONDBLCLK);
        Message(toggle, WM_LBUTTONUP);

        Assert.False(toggle.Checked);
        Assert.Equal(2, events);
    }

    [Fact]
    public void ARealRightClick_DoesNothing()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        Message(toggle, WM_RBUTTONDOWN);
        Message(toggle, 0x0205);   // WM_RBUTTONUP

        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ARealPress_LetGoFarOutside_DoesNothing()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP, 400, 300);

        Assert.False(toggle.Checked);
    }

    // ---- handlers ----

    [Fact]
    public void AHandlerThatTakesTheStateBack_DoesNotMakeItLoop_AndTheKnobStandsWhereTheStateIs()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        int events = 0;
        toggle.CheckedChanged += (s, e) =>
        {
            events++;

            if (toggle.Checked)
                toggle.Checked = false;   // (a veto)
        };

        toggle.InvokePrivate("ToggleByUser");

        Assert.False(toggle.Checked);
        Assert.Equal(2, events);
        Assert.Equal(0.0, toggle.GetPrivateField<double>("_position"));
    }

    [Fact]
    public void AHandlerThatDisablesTheSwitch_LeavesItDisabled_WithNothingPressed()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.CheckedChanged += (s, e) => toggle.Enabled = false;

        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP);

        Assert.True(toggle.Checked);
        Assert.False(toggle.Enabled);
        Assert.False(toggle.GetPrivateField<bool>("_isPressed"));
    }

    [Fact]
    public void AHandlerThatDisposesTheSwitch_DoesNotThrow()
    {
        ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        Form form = Show(toggle);
        toggle.CheckedChanged += (s, e) => toggle.Dispose();

        Exception? failure = Record.Exception(() =>
        {
            Message(toggle, WM_LBUTTONDOWN);
            Message(toggle, WM_LBUTTONUP);
        });

        Assert.Null(failure);
        Assert.True(toggle.IsDisposed);
        form.Dispose();
    }

    [Fact]
    public void AHandlerThatHidesTheSwitch_DoesNotThrow_AndNothingStaysPressed()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.CheckedChanged += (s, e) => toggle.Visible = false;

        Message(toggle, WM_LBUTTONDOWN);
        Message(toggle, WM_LBUTTONUP);

        Assert.True(toggle.Checked);
        Assert.False(toggle.GetPrivateField<bool>("_isPressed"));
    }

    [Fact]
    public void ASwitchWithAnOwnBackColor_PaintsItInsteadOfThePage()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        toggle.BackColor = Color.FromArgb(200, 30, 30);
        using Form form = Show(toggle);

        using Bitmap picture = Draw(toggle);

        Assert.Equal(Color.FromArgb(200, 30, 30), picture.GetPixel(0, 0));
    }

    [Fact]
    public void TheTooltipText_SetOnADisposedSwitch_DoesNotThrow()
    {
        ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        toggle.Dispose();

        Exception? failure = Record.Exception(() =>
        {
            toggle.ToolTipText = "late";
            toggle.Checked = true;
        });

        Assert.Null(failure);
    }

    // ---- sizes ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 40)]
    [InlineData(200, 2)]
    [InlineData(4, 4)]
    [InlineData(500, 120)]
    public void AnyOddSize_PaintsWithoutThrowing_InEveryState(int width, int height)
    {
        foreach (bool isChecked in new[] { true, false })
        {
            foreach (bool enabled in new[] { true, false })
            {
                using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(isChecked, size: new Size(width, height));
                toggle.Enabled = enabled;
                using Form form = Show(toggle);

                Exception? failure = Record.Exception(() =>
                {
                    // (The paint code itself, on a bitmap of the size: DrawToBitmap refuses a control that has no size.)
                    using Bitmap picture = new Bitmap(Math.Max(width, 1), Math.Max(height, 1));
                    using Graphics graphics = Graphics.FromImage(picture);
                    toggle.InvokePrivate("OnPaint", new PaintEventArgs(graphics, new Rectangle(0, 0, width, height)));
                    toggle.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Space));
                    toggle.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                });

                Assert.Null(failure);
            }
        }
    }

    [Fact]
    public void ResizingWhileItGlides_DoesNotThrow()
    {
        AnimationSettings.Enabled = () => true;
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(false);
        using Form form = Show(toggle);

        toggle.InvokePrivate("ToggleByUser");
        toggle.Size = new Size(90, 40);
        Pump(60);
        toggle.Size = new Size(20, 12);

        Exception? failure = Record.Exception(() => Pump(300));

        Assert.Null(failure);
        Assert.Equal(1.0, toggle.GetPrivateField<double>("_position"));
    }

    // ---- the tooltips ----

    [Fact]
    public void TheTooltips_FollowTheState_AndANullIsNoText()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false, "Is on", "Is off");
        using Form form = Show(toggle);

        Assert.Equal("Is off", toggle.GetPrivateField<ToolTip>("_toolTip")!.GetToolTip(toggle));

        toggle.Checked = true;
        Assert.Equal("Is on", toggle.GetPrivateField<ToolTip>("_toolTip")!.GetToolTip(toggle));

        toggle.ToolTipTextChecked = null!;
        Assert.Equal(string.Empty, toggle.ToolTipTextChecked);
        Assert.Equal(string.Empty, toggle.GetPrivateField<ToolTip>("_toolTip")!.GetToolTip(toggle));
    }

    [Fact]
    public void TheShorthandTooltip_SetsBoth()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);

        toggle.ToolTipText = "Dark mode";

        Assert.Equal("Dark mode", toggle.ToolTipTextChecked);
        Assert.Equal("Dark mode", toggle.ToolTipTextUnchecked);
    }

    // ---- the parent ----

    [Fact]
    public void ADisabledParent_MakesTheSwitchDisabled_AndNothingStaysHovered()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);
        toggle.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        toggle.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 10, 0));

        form.Enabled = false;

        Assert.False(toggle.GetPrivateField<bool>("_isPressed"));
        toggle.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 20, 10, 0));
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void TheSwitch_CanBeTabbedTo_AndDoesNotTakeTheFocusFromAMouseClick_Badly()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = Show(toggle);

        Assert.True(toggle.TabStop);
        Assert.True(toggle.CanFocus);
        Assert.Equal(Cursors.Hand, toggle.Cursor);
    }
}
