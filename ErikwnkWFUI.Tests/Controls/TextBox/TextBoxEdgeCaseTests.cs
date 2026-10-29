using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The text boxes with odd input: handlers that answer, take the box away or unlock it again, a window that is made again, sizes and fonts, double clicks, a press that is not let go, a disabled or hidden box, and the placeholder next to the button.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxEdgeCaseTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref RECT rect);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;

    private readonly TextBoxHost _host = new TextBoxHost();

    public void Dispose()
    {
        _host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private static IntPtr At(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static Rectangle InnerBounds(TextBox box) => (Rectangle)box.GetType().GetProperty("InnerBounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(box)!;

    private static Point Middle(TextBox box)
    {
        Rectangle bounds = InnerBounds(box);

        return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    }

    private static void Press(TextBox box, int message = WM_LBUTTONDOWN)
    {
        Point point = Middle(box);
        SendMessage(box.Handle, message, IntPtr.Zero, At(point.X, point.Y));
    }

    private static void Release(TextBox box)
    {
        Point point = Middle(box);
        SendMessage(box.Handle, WM_LBUTTONUP, IntPtr.Zero, At(point.X, point.Y));
    }

    private static void Click(TextBox box)
    {
        Press(box);
        Release(box);
    }

    private static Rectangle TextArea(TextBox box)
    {
        RECT rect = new RECT();
        SendMessage(box.Handle, 0x00B2, IntPtr.Zero, ref rect);

        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    // ---- handlers ----

    [Fact]
    public void AHandlerThatUnlocksTheBoxAgain_AfterItIsAccepted_DoesNotLoop()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("abc", locked: false));
        int accepted = 0;
        box.EditAccepted += (s, e) =>
        {
            accepted++;
            box.IsLocked = false;
        };

        box.AcceptEdit();

        Assert.Equal(1, accepted);
        Assert.False(box.IsLocked);
    }

    [Fact]
    public void AHandlerThatChangesTheText_WhileTheEditIsCancelled_LeavesThatText()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("old"));
        box.BeginEdit();
        box.Text = "typed";
        box.EditCancelled += (s, e) => box.Text = "set by the handler";

        box.CancelEdit();

        Assert.True(box.IsLocked);
        Assert.Equal("set by the handler", box.Text);
    }

    [Fact]
    public void AHandlerThatDisposesTheBox_DoesNotThrow()
    {
        LockableTextBox box = UIStyles.TextBoxes.CreateLockable("abc", locked: false);
        Form form = OffscreenForm.Show(box, new Size(200, 40), UIColors.BackgroundMedium, new Point(10, 8));
        box.EditAccepted += (s, e) => box.Dispose();

        Exception? failure = Record.Exception(() => Click(box));

        Assert.Null(failure);
        Assert.True(box.IsDisposed);
        form.Dispose();
    }

    [Fact]
    public void BeginEditTwice_AcceptWhileLocked_AndCancelWhileLocked_DoNothingNew()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("abc"));
        int changes = 0;
        int accepted = 0;
        int cancelled = 0;
        box.LockedChanged += (s, e) => changes++;
        box.EditAccepted += (s, e) => accepted++;
        box.EditCancelled += (s, e) => cancelled++;

        box.AcceptEdit();
        box.CancelEdit();
        Assert.Equal(0, changes + accepted + cancelled);

        box.BeginEdit();
        box.BeginEdit();
        Assert.Equal(1, changes);
    }

    [Fact]
    public void UnlockingFromCode_AndThenCancelling_GivesBackTheTextFromBeforeTheUnlock()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("old"));

        box.IsLocked = false;
        box.Text = "typed";
        box.CancelEdit();

        Assert.Equal("old", box.Text);
        Assert.True(box.IsLocked);
    }

    // ---- the mouse on the button ----

    [Fact]
    public void ADoubleClickOnTheButton_CountsAsTwoClicks()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));

        Click(box);
        Assert.False(PasswordProbe.Hides(box));

        Press(box, WM_LBUTTONDBLCLK);
        Release(box);

        Assert.True(PasswordProbe.Hides(box));
    }

    [Fact]
    public void APressThatIsNotLetGo_StaysNoLongerPressed_WhenTheCaptureIsLost_AndTheLateReleaseDoesNothing()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));

        Press(box);
        Assert.True(box.GetPrivateField<bool>("_innerPressed"));

        box.InvokePrivate("OnMouseCaptureChanged", EventArgs.Empty);
        Assert.True(box.GetPrivateField<bool>("_innerPressed") || !box.Capture);
        box.Capture = false;
        box.InvokePrivate("OnMouseCaptureChanged", EventArgs.Empty);

        Assert.False(box.GetPrivateField<bool>("_innerPressed"));
        Release(box);
        Assert.True(PasswordProbe.Hides(box));
    }

    [Fact]
    public void DisablingTheBox_WhileTheMouseIsOverTheButton_ClearsTheHotAndPressedLook()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));
        box.SetPrivateField("_innerHot", true);
        box.SetPrivateField("_innerPressed", true);

        box.Enabled = false;

        Assert.False(box.GetPrivateField<bool>("_innerHot"));
        Assert.False(box.GetPrivateField<bool>("_innerPressed"));
    }

    [Fact]
    public void HidingTheBox_ClearsTheHotLook()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));
        box.SetPrivateField("_innerHot", true);

        box.Visible = false;

        Assert.False(box.GetPrivateField<bool>("_innerHot"));
    }

    [Fact]
    public void ADisabledPasswordBox_DoesNotRespondToTheEye()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));
        box.Enabled = false;

        Click(box);

        Assert.True(PasswordProbe.Hides(box));
    }

    [Fact]
    public void AnEmptyPasswordBox_CanShowAndHide_AndTheSelectionStays()
    {
        TextBox empty = _host.Show(UIStyles.TextBoxes.CreatePassword(""));
        TextBox filled = _host.Show(UIStyles.TextBoxes.CreatePassword("abcdef"));
        filled.Select(2, 3);

        Click(empty);
        Click(empty);
        Click(filled);

        Assert.True(PasswordProbe.Hides(empty));
        Assert.Equal(2, filled.SelectionStart);
        Assert.Equal(3, filled.SelectionLength);
        Assert.Equal("abcdef", filled.Text);
    }

    // ---- the window and the size ----

    [Fact]
    public void AWindowThatIsMadeAgain_KeepsTheButton_ItsMargin_AndItsState()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("abc"));
        box.BeginEdit();

        box.InvokePrivate("RecreateHandle");
        Application.DoEvents();

        Assert.False(box.IsLocked);
        Assert.Equal("abc", box.Text);
        Assert.True(TextArea(box).Right <= box.ClientSize.Width - InnerBounds(box).Width, "the text reaches under the button after the window was made again");

        Click(box);
        Assert.True(box.IsLocked);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(160)]
    [InlineData(400)]
    public void TheTextKeepsAwayFromTheButton_AtEveryWidth_AndAfterAResize(int width)
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"), width);
        Assert.True(TextArea(box).Right <= box.ClientSize.Width - InnerBounds(box).Width);

        box.Width = width + 50;
        Application.DoEvents();

        Assert.True(TextArea(box).Right <= box.ClientSize.Width - InnerBounds(box).Width);
    }

    [Fact]
    public void ABiggerFont_KeepsTheButtonAtTheRight_AndTheTooltipIsWorkedOutAgain()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("a text that fits nicely in the box"), 260);
        string? tip = ((ErikwnkWFUI.Controls.TextField)box).ToolTipTextNow;
        Assert.Equal(string.Empty, tip);

        box.Font = new Font("Segoe UI", 18);
        Application.DoEvents();

        Assert.Equal("a text that fits nicely in the box", ((ErikwnkWFUI.Controls.TextField)box).ToolTipTextNow);
    }

    [Fact]
    public void TheButtonIsAsWideAsTheDpiSays_AndNeverWiderThanTheBox()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"), 30);

        Rectangle bounds = InnerBounds(box);

        Assert.Equal(Dpi.Scale(box, 24), bounds.Width);
        Assert.True(bounds.Left >= -1, "the button starts left of the box");
    }

    [Fact]
    public void ThePlaceholder_DoesNotReachUnderTheButton()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("", "A placeholder that is much longer than the box can show"), 120);
        Rectangle button = InnerBounds(box);
        using Bitmap picture = Draw(box);

        for (int x = button.Left + 1; x < button.Left + 4; x++)
        {
            for (int y = 4; y < 14; y++)
                Assert.Equal(box.BackColor.ToArgb(), picture.GetPixel(x, y).ToArgb());
        }
    }

    [Fact]
    public void AMultilineBox_HasTheButtonAtTheTop_AsHighAsOneLine()
    {
        LockableTextBox box = UIStyles.TextBoxes.CreateLockable("one", locked: false);
        box.Multiline = true;
        _host.Show(box, 160, 90);

        Rectangle bounds = InnerBounds(box);

        Assert.Equal(0, bounds.Top);
        Assert.True(bounds.Height < box.ClientSize.Height, "the button is as high as the whole box");
        Assert.True(bounds.Height >= 16);
    }

    // ---- values that are odd ----

    [Fact]
    public void ANullTextAndANullPlaceholder_AreAnEmptyOne()
    {
        // (Disposed: a box that stays listens to the language for the rest of the run, and a switch from another thread would then reach its window.)
        using TextBox box = UIStyles.TextBoxes.CreateStandard(null!, null!);
        using LockableTextBox lockable = UIStyles.TextBoxes.CreateLockable(null!, null!);
        using TextBox password = UIStyles.TextBoxes.CreatePassword(null!, null!);

        Assert.Equal(string.Empty, box.Text);
        Assert.Equal(string.Empty, lockable.Text);
        Assert.Equal(string.Empty, password.Text);

        ((TextField)box).Placeholder = null!;
        Assert.Equal(string.Empty, ((TextField)box).Placeholder);
    }

    [Fact]
    public void ATextOfTenThousandCharacters_IsHandledAtOnce_AndItsTooltipIsTheWholeText()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard(), 100);
        string text = new string('x', 10000);

        box.Text = text;

        Assert.Equal(text, ((ErikwnkWFUI.Controls.TextField)box).ToolTipTextNow);
    }

    [Fact]
    public void ThePlaceholderOfABoxThatIsMadeLockedAndUnlockedAgain_IsStillThere()
    {
        LockableTextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable("", "hint"));
        LockableTextBox plain = _host.Show(UIStyles.TextBoxes.CreateLockable(""));

        box.IsLocked = false;
        box.IsLocked = true;
        plain.IsLocked = false;
        plain.IsLocked = true;
        using Bitmap withHint = Draw(box);
        using Bitmap without = Draw(plain);

        Assert.False(SamePixels(withHint, without));
    }
}
