using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.ComboBoxes;

/// <summary>A click with the left button anywhere else takes the focus out of the combo box and the number box, as out of the text box.</summary>
[Collection(FocusTestCollection.Name)]
public class InputFocusReleaseTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;

    public static IEnumerable<object[]> Boxes()
    {
        yield return new object[] { "ComboBox", (Func<Control>)(() => UIStyles.ComboBoxes.CreateStandard()) };
        yield return new object[] { "ComboBox (list)", (Func<Control>)(() => { System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard(); box.DropDownStyle = ComboBoxStyle.DropDownList; return box; }) };
        yield return new object[] { "NumericUpDown", (Func<Control>)(() => UIStyles.NumericUpDowns.CreateStandard()) };
        yield return new object[] { "TextBox", (Func<Control>)(() => UIStyles.TextBoxes.CreateStandard("x")) };
    }

    // A form with the box that has the focus, a label (which cannot take the focus), another text box and a button.
    private (Form Form, Control Box, Label Label, TextBox Other) Show(Control box)
    {
        Form form = OffscreenForm.Create(new Size(320, 200), UIColors.BackgroundMedium);
        Label label = new Label { Text = "Not focusable", Location = new Point(10, 110), Size = new Size(200, 24) };
        TextBox other = UIStyles.TextBoxes.CreateStandard("Other");
        other.Location = new Point(10, 60);
        Button button = UIStyles.Buttons.CreateStandard("Button");
        button.Location = new Point(10, 140);
        box.Location = new Point(10, 10);
        box.Width = 200;
        form.Controls.AddRange(new Control[] { box, other, label, button });
        _forms.Add(form);
        form.Show();
        Application.DoEvents();
        box.Focus();
        Application.DoEvents();

        return (form, box, label, other);
    }

    private static void Click(Control target, int message = WM_LBUTTONDOWN)
    {
        PostMessage(target.Handle, message, IntPtr.Zero, (IntPtr)((5 << 16) | 5));

        for (int i = 0; i < 5; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static bool SwitchOf(Control box)
    {
        return (bool)box.GetType().GetProperty("ReleaseFocusOnOutsideClick")!.GetValue(box)!;
    }

    private static void SetSwitch(Control box, bool value)
    {
        box.GetType().GetProperty("ReleaseFocusOnOutsideClick")!.SetValue(box, value);
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void ALeftClickOnSomethingThatCannotTakeTheFocus_OrOnTheWindow_TakesTheFocusOutOfTheBox(string name, Func<Control> make)
    {
        var shown = Show(make());
        Assert.True(shown.Box.ContainsFocus, name + ": the box has the focus to begin with");

        Click(shown.Label);
        Assert.False(shown.Box.ContainsFocus, name);

        shown.Box.Focus();
        Click(shown.Form);
        Assert.False(shown.Box.ContainsFocus, name + ": the background of the window");
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void AClickInTheBox_KeepsTheFocus_AndSoDoesARightClick_AndAnotherBoxTakesIt(string name, Func<Control> make)
    {
        var shown = Show(make());

        Click(shown.Box);
        Assert.True(shown.Box.ContainsFocus, name);

        Click(shown.Label, WM_RBUTTONDOWN);
        Assert.True(shown.Box.ContainsFocus, name + ": only the left button counts");

        shown.Other.Focus();
        Click(shown.Other);
        Assert.False(shown.Box.ContainsFocus, name);
        Assert.True(shown.Other.Focused, name);
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void TheSwitch_IsOnByDefault_AndTurnedOff_TheFocusStays(string name, Func<Control> make)
    {
        var shown = Show(make());
        Assert.True(SwitchOf(shown.Box), name);

        SetSwitch(shown.Box, false);
        Click(shown.Label);
        Assert.True(shown.Box.ContainsFocus, name + ": switched off");

        SetSwitch(shown.Box, true);
        Click(shown.Label);
        Assert.False(shown.Box.ContainsFocus, name + ": switched on again");
    }

    [Fact]
    public void TheOpenListOfAComboBox_IsAWindowOfTheBox_NotSomewhereElse()
    {
        ErikwnkWFUI.Controls.ComboBox combo = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard();
        combo.Items.AddRange(new object[] { "one", "two" });
        var shown = Show(combo);

        IntPtr list = combo.InvokePrivate<IntPtr>("GetListHandle");
        object watcher = combo.GetPrivateField<object>("_clickWatcher")!;

        Assert.NotEqual(IntPtr.Zero, list);
        Assert.False(watcher.InvokePrivate<bool>("IsOutsideClick", list));
        Assert.True(watcher.InvokePrivate<bool>("IsOutsideClick", shown.Label.Handle));
        Assert.False(watcher.InvokePrivate<bool>("IsOutsideClick", combo.Handle));
    }

    [Fact]
    public void TheTextAndTheArrowsOfTheNumberBox_AreInsideIt()
    {
        ErikwnkWFUI.Controls.NumericUpDown number = (ErikwnkWFUI.Controls.NumericUpDown)UIStyles.NumericUpDowns.CreateStandard();
        var shown = Show(number);
        object watcher = number.GetPrivateField<object>("_clickWatcher")!;

        foreach (Control child in number.Controls)
            Assert.False(watcher.InvokePrivate<bool>("IsOutsideClick", child.Handle), child.GetType().Name);

        Assert.True(watcher.InvokePrivate<bool>("IsOutsideClick", shown.Label.Handle));
    }

    [Fact]
    public void ADisposedBox_NoLongerWatchesTheClicks()
    {
        foreach (Func<Control> make in new Func<Control>[] { () => UIStyles.ComboBoxes.CreateStandard(), () => UIStyles.NumericUpDowns.CreateStandard() })
        {
            var shown = Show(make());
            shown.Form.Controls.Remove(shown.Box);
            shown.Box.Dispose();

            Assert.Null(Record.Exception(() => Click(shown.Label)));
            Assert.Null(Record.Exception(() => Click(shown.Form)));
        }
    }
}