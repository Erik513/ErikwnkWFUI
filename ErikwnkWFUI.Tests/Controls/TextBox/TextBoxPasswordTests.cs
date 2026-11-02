using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>Showing and hiding a password with the eye: the window stays the same one, so what was typed, the caret and the focus stay.</summary>
[Collection(FocusTestCollection.Name)]
public class TextBoxPasswordTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private TextBox Show(UIColorSet? colors = null)
    {
        TextBox box = UIStyles.TextBoxes.CreatePassword("", "Password", colors);
        Form form = OffscreenForm.Create(new Size(320, 60), UIColors.BackgroundMedium);
        box.SetBounds(10, 10, 280, box.Height);
        form.Controls.Add(box);
        _forms.Add(form);
        form.Show();
        Application.DoEvents();
        box.Focus();
        Application.DoEvents();

        return box;
    }

    private static void Type(TextBox box, string text)
    {
        foreach (char c in text)
            SendMessage(box.Handle, 0x0102, (IntPtr)c, IntPtr.Zero);

        Application.DoEvents();
    }

    // The click on the eye, the way the mouse sends it.
    private static void ClickTheEye(TextBox box)
    {
        IntPtr position = (IntPtr)((box.ClientSize.Height / 2 << 16) | (box.ClientSize.Width - 10));
        PostMessage(box.Handle, 0x0201, (IntPtr)1, position);
        PostMessage(box.Handle, 0x0202, IntPtr.Zero, position);

        for (int i = 0; i < 6; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhatWasTyped_StaysWhenTheEyeIsClicked_AndTheBoxIsTheSameWindow_WithTheFocusAndTheCaret(bool yellow)
    {
        TextBox box = Show(yellow ? UIStyles.Colors.Sets.Yellow : null);
        int handleCreated = 0;
        box.HandleCreated += (sender, e) => handleCreated++;
        IntPtr handle = box.Handle;

        Type(box, "my secret");
        int caret = box.SelectionStart;

        ClickTheEye(box);

        Assert.Equal("my secret", box.Text);
        Assert.Equal(handle, box.Handle);
        Assert.Equal(0, handleCreated);
        Assert.True(box.Focused);
        Assert.Equal(caret, box.SelectionStart);
        Assert.Equal(0, box.SelectionLength);
        Assert.True(((ErikwnkWFUI.Controls.TextField)box).GetType().GetProperty("IsPasswordShown")!.GetValue(box) is true);

        ClickTheEye(box);

        Assert.Equal("my secret", box.Text);
        Assert.Equal(handle, box.Handle);
        Assert.True(PasswordProbe.Hides(box));

        Type(box, "!");
        Assert.Equal("my secret!", box.Text);
    }

    [Fact]
    public void ShownMeansNoPasswordChar_HiddenMeansTheDot_AndThePicturesDiffer()
    {
        TextBox box = Show();
        Type(box, "abc");
        using Bitmap hidden = Draw(box);
        Assert.Equal('\u25CF', box.PasswordChar);

        ClickTheEye(box);
        using Bitmap shown = Draw(box);

        Assert.Equal('\0', box.PasswordChar);
        Assert.False(PasswordProbe.Hides(box));
        Assert.False(SamePixels(hidden, shown), "the text looks the same shown and hidden");

        ClickTheEye(box);
        using Bitmap hiddenAgain = Draw(box);

        Assert.True(SamePixels(hidden, hiddenAgain), "hidden again does not look as it did");
    }

    [Fact]
    public void ThePasswordThatIsShown_HasTheTooltipOfACutText_TheOneThatIsHiddenHasNone()
    {
        TextBox box = Show();
        string text = string.Join(" ", Enumerable.Repeat("a password that is much too long for the box", 4));
        Type(box, text);

        Assert.Equal(string.Empty, ((ErikwnkWFUI.Controls.TextField)box).ToolTipTextNow);

        ClickTheEye(box);

        Assert.Equal(text, ((ErikwnkWFUI.Controls.TextField)box).ToolTipTextNow);
    }

    [Fact]
    public void TheKeysAltF8_ShowAndHideItAsWell_AndNothingIsLost()
    {
        TextBox box = Show();
        Type(box, "typed");
        IntPtr handle = box.Handle;
        object[] args = { new Message(), Keys.Alt | Keys.F8 };
        System.Reflection.MethodInfo method = typeof(Control).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        Assert.True((bool)method.Invoke(box, args)!);
        Assert.False(PasswordProbe.Hides(box));
        Assert.Equal("typed", box.Text);
        Assert.Equal(handle, box.Handle);

        Assert.True((bool)method.Invoke(box, args)!);
        Assert.True(PasswordProbe.Hides(box));
        Assert.Equal("typed", box.Text);
    }
}
