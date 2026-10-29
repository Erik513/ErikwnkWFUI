using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// What the check box and the radio button know of the mouse and the keyboard, asked of both the same way: hovered, pressed (left button, space bar), and
/// what ends them. The events are raised as the system raises them (the protected On... methods of the control). [Collection]: the theme is read.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class BoxStateTests
{
    public static IEnumerable<object[]> Boxes()
    {
        yield return new object[] { "CheckBox", (Func<ButtonBase>)(() => UIStyles.CheckBoxes.CreateStandard("A", false)) };
        yield return new object[] { "RadioButton", (Func<ButtonBase>)(() => UIStyles.RadioButtons.CreateStandard("A", false)) };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetUpdateRect(IntPtr window, out Rect rect, bool erase);

    private static void Raise(Control box, string handler, EventArgs e)
    {
        box.InvokePrivate(handler, e);
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void TheMouse_ShowsHover_AndLeavingEndsHoverAndPress(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();

        Raise(box, "OnMouseEnter", EventArgs.Empty);
        Assert.True(box.IsHot());

        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.True(box.IsPressed());

        Raise(box, "OnMouseLeave", EventArgs.Empty);
        Assert.False(box.IsHot());
        Assert.False(box.IsPressed());
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void OnlyTheLeftButton_Presses_AndTheReleaseEndsIt(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();

        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));
        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Middle, 1, 5, 5, 0));
        Assert.False(box.IsPressed());

        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.True(box.IsPressed());

        Raise(box, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.False(box.IsPressed());
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void TheSpaceBar_Presses_OtherKeysDoNot_AndLosingTheFocusEndsIt(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();

        Raise(box, "OnKeyDown", new KeyEventArgs(Keys.Enter));
        Assert.False(box.IsPressed());

        Raise(box, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Assert.True(box.IsPressed());

        Raise(box, "OnKeyUp", new KeyEventArgs(Keys.Enter));
        Assert.True(box.IsPressed());

        Raise(box, "OnKeyUp", new KeyEventArgs(Keys.Space));
        Assert.False(box.IsPressed());

        Raise(box, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Raise(box, "OnLostFocus", EventArgs.Empty);
        Assert.False(box.IsPressed());
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void ThePress_EndsWhenTheMouseIsTakenAway_AndHiddenBoxesAreNotHoveredOrPressed(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        Raise(box, "OnMouseEnter", EventArgs.Empty);
        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        Raise(box, "OnMouseCaptureChanged", EventArgs.Empty);
        Assert.False(box.IsPressed());
        Assert.True(box.IsHot());

        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        box.Visible = false;
        Assert.False(box.IsHot());
        Assert.False(box.IsPressed());
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void WhenItIsDisabledAndEnabledAgain_NothingIsPressed_AndTheMouseIsLookedAtAgain(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        Raise(box, "OnMouseEnter", EventArgs.Empty);
        Raise(box, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        box.Enabled = false;
        box.Enabled = true;

        Assert.False(box.IsPressed());
        Assert.False(box.IsHot());
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void AChangeOfTheState_RepaintsTheBox(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(200, 60) };
        box.Location = new Point(5, 5);
        host.Controls.Add(box);
        host.Show();
        Application.DoEvents();
        box.Update();

        // (The box does not raise Paint - it draws everything itself - so what is asked is whether its window has something to be painted again.)
        Assert.False(GetUpdateRect(box.Handle, out _, false), name + " is not up to date before the test");

        Raise(box, "OnMouseEnter", EventArgs.Empty);

        Assert.True(GetUpdateRect(box.Handle, out _, false), name + " did not ask to be painted when the mouse came");
    }
}
