using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>A click with the left button anywhere else takes the focus out of the text box.</summary>
[Collection(FocusTestCollection.Name)]
public class TextBoxFocusTests : IDisposable
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

    // A form with a box, a label (which cannot take the focus), another box and a button; the first box has the focus.
    private (Form Form, TextBox Box, Label Label, TextBox Other, Button Button) Show(TextBox box)
    {
        Form form = OffscreenForm.Create(new Size(320, 200), UIColors.BackgroundMedium);
        Label label = new Label { Text = "Not focusable", Location = new Point(10, 90), Size = new Size(200, 24) };
        TextBox other = UIStyles.TextBoxes.CreateStandard("Other");
        other.Location = new Point(10, 50);
        Button button = UIStyles.Buttons.CreateStandard("Button");
        button.Location = new Point(10, 130);
        box.Location = new Point(10, 10);
        box.Width = 200;
        form.Controls.AddRange(new Control[] { box, other, label, button });
        _forms.Add(form);
        form.Show();
        Application.DoEvents();
        box.Focus();
        Application.DoEvents();

        return (form, box, label, other, button);
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

    [Fact]
    public void ALeftClickOnSomethingThatCannotTakeTheFocus_TakesTheFocusOutOfTheBox()
    {
        var shown = Show(UIStyles.TextBoxes.CreateStandard("Text"));
        Assert.True(shown.Box.Focused, "the box has the focus to begin with");

        Click(shown.Label);

        Assert.False(shown.Box.Focused);
        Assert.Null(shown.Form.ActiveControl);
    }

    [Fact]
    public void AClickOnTheBackgroundOfTheWindow_DoesTheSame()
    {
        var shown = Show(UIStyles.TextBoxes.CreateStandard("Text"));

        Click(shown.Form);

        Assert.False(shown.Box.Focused);
    }

    [Fact]
    public void AClickOnAnotherBox_GivesTheFocusToThatOne_OnlyOneBoxHasIt()
    {
        var shown = Show(UIStyles.TextBoxes.CreateStandard("Text"));

        shown.Other.Focus();
        Click(shown.Other);

        Assert.False(shown.Box.Focused);
        Assert.True(shown.Other.Focused);
    }

    [Fact]
    public void AClickInTheBox_KeepsTheFocus_AndSoDoesARightClick()
    {
        var shown = Show(UIStyles.TextBoxes.CreateStandard("Text"));

        Click(shown.Box);
        Assert.True(shown.Box.Focused);

        Click(shown.Label, WM_RBUTTONDOWN);
        Assert.True(shown.Box.Focused, "only the left button counts");
    }

    [Fact]
    public void AClickOnTheInnerButton_IsAClickInTheBox()
    {
        var shown = Show(UIStyles.TextBoxes.CreatePassword("secret", ""));

        Click(shown.Box);

        Assert.True(shown.Box.Focused);
    }

    [Fact]
    public void ADisposedBox_NoLongerWatchesTheClicks()
    {
        var shown = Show(UIStyles.TextBoxes.CreateStandard("Text"));
        shown.Form.Controls.Remove(shown.Box);
        shown.Box.Dispose();

        Assert.Null(Record.Exception(() => Click(shown.Label)));
        Assert.Null(Record.Exception(() => Click(shown.Form)));
    }

    [Fact]
    public void AClickOnTheLabel_WhileTheBoxIsLockableAndBeingEdited_EndsTheFocusButNotTheEditing()
    {
        ErikwnkWFUI.Controls.LockableTextBox box = UIStyles.TextBoxes.CreateLockable("Playlist", "", false);
        var shown = Show(box);

        Click(shown.Label);

        Assert.False(box.Focused);
        Assert.False(box.IsLocked, "losing the focus is not accepting or cancelling");
    }
}
