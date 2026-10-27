using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The text boxes with a button in the box: the one that is locked until the pencil is used, and the one for a password with an eye; and the tooltip with the whole text when it is cut.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxInnerButtonTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_MOUSEMOVE = 0x0200;

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private T Show<T>(T box, int width = 160) where T : TextBox
    {
        box.TabStop = false;
        box.Width = width;
        Form form = OffscreenForm.Show(box, new Size(width + 20, box.Multiline ? 100 : 40), UIColors.BackgroundMedium, new Point(10, 8));
        _forms.Add(form);

        return box;
    }

    private static IntPtr At(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static Point Middle(Rectangle bounds) => new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static Rectangle InnerBounds(TextBox box) => (Rectangle)box.GetType().GetProperty("InnerBounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(box)!;

    // A real click on the button in the box: the messages that Windows sends.
    private static void ClickInner(TextBox box)
    {
        Point point = Middle(InnerBounds(box));
        SendMessage(box.Handle, WM_LBUTTONDOWN, IntPtr.Zero, At(point.X, point.Y));
        SendMessage(box.Handle, WM_LBUTTONUP, IntPtr.Zero, At(point.X, point.Y));
    }

    private static bool Key(TextBox box, Keys keys)
    {
        Message message = new Message();
        object[] args = { message, keys };

        return (bool)typeof(Control).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(box, args)!;
    }

    private static string ToolTipOf(TextBox box)
    {
        ToolTip tip = box.GetPrivateField<ToolTip>("_toolTip")!;

        return tip.GetToolTip(box);
    }

    // ---- the box that is locked ----

    [Fact]
    public void ALockableBox_StartsLocked_WithAPencil_AndCanStartUnlocked()
    {
        LockableTextBox locked = Show(UIStyles.TextBoxes.CreateLockable("name", "hint"));
        LockableTextBox open = Show(UIStyles.TextBoxes.CreateLockable("name", locked: false));

        Assert.True(locked.IsLocked);
        Assert.True(locked.ReadOnly);
        Assert.Equal("name", locked.Text);
        Assert.Equal("✎", locked.GetPrivateField<string>("_innerGlyph"));

        Assert.False(open.IsLocked);
        Assert.False(open.ReadOnly);
        Assert.Equal("✔", open.GetPrivateField<string>("_innerGlyph"));
    }

    [Fact]
    public void TheButtonInTheBox_IsPainted_AtTheRightEnd_AndTheTextKeepsAwayFromIt()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        LockableTextBox plain = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        typeof(ThemedTextBox).GetProperty("InnerGlyph", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(plain, null);
        Rectangle bounds = InnerBounds(box);

        Assert.True(bounds.Right <= box.ClientSize.Width);
        Assert.True(bounds.Width > 10, "no room for the button");
        Assert.Equal(box.ClientSize.Width, bounds.Right);

        // Something is painted there that is not the background; without the button nothing is.
        using Bitmap with = Draw(box);
        using Bitmap without = Draw(plain);
        bool ink = false;

        for (int x = bounds.Left + 2; x < bounds.Right - 2 && !ink; x++)
        {
            for (int y = bounds.Top + 3; y < bounds.Bottom - 3; y++)
            {
                if (with.GetPixel(x, y).ToArgb() != box.BackColor.ToArgb())
                {
                    ink = true;
                    break;
                }
            }
        }

        Assert.True(ink, "the pencil is not painted");
        Assert.False(SamePixels(with, without));
    }

    [Fact]
    public void ThePencil_UnlocksTheBox_GivesItTheFocusAndSelectsItsText_AndTheCheckMarkAcceptsIt()
    {
        StaThread.Run(() =>
        {
            LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("old name"));
            int unlocked = 0;
            int accepted = 0;
            box.LockedChanged += (s, e) => unlocked++;
            box.EditAccepted += (s, e) => accepted++;

            ClickInner(box);

            Assert.False(box.IsLocked);
            Assert.False(box.ReadOnly);
            Assert.Equal("old name", box.SelectedText);
            Assert.Equal("✔", box.GetPrivateField<string>("_innerGlyph"));
            Assert.Equal(1, unlocked);
            Assert.Equal(0, accepted);

            box.Text = "new name";
            ClickInner(box);

            Assert.True(box.IsLocked);
            Assert.True(box.ReadOnly);
            Assert.Equal("new name", box.Text);
            Assert.Equal(1, accepted);
            Assert.Equal(2, unlocked);
            Assert.Equal("✎", box.GetPrivateField<string>("_innerGlyph"));
        });
    }

    [Fact]
    public void AClickOnTheTextOfALockedBox_DoesNotUnlockIt()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));

        SendMessage(box.Handle, WM_LBUTTONDOWN, IntPtr.Zero, At(10, 8));
        SendMessage(box.Handle, WM_LBUTTONUP, IntPtr.Zero, At(10, 8));

        Assert.True(box.IsLocked);
    }

    [Fact]
    public void APressOnTheButton_LetGoElsewhere_DoesNothing()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        Point point = Middle(InnerBounds(box));

        SendMessage(box.Handle, WM_LBUTTONDOWN, IntPtr.Zero, At(point.X, point.Y));
        SendMessage(box.Handle, WM_LBUTTONUP, IntPtr.Zero, At(5, 8));

        Assert.True(box.IsLocked);
        Assert.False(box.GetPrivateField<bool>("_innerPressed"));
    }

    [Fact]
    public void TheEscapeKey_GivesTheOldTextBack_AndLocksTheBox_EnterAcceptsIt()
    {
        StaThread.Run(() =>
        {
            LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("old"));
            int cancelled = 0;
            int accepted = 0;
            box.EditCancelled += (s, e) => cancelled++;
            box.EditAccepted += (s, e) => accepted++;

            box.BeginEdit();
            box.Text = "typed";
            Assert.True(Key(box, Keys.Escape));

            Assert.True(box.IsLocked);
            Assert.Equal("old", box.Text);
            Assert.Equal(1, cancelled);

            box.BeginEdit();
            box.Text = "typed";
            Assert.True(Key(box, Keys.Enter));

            Assert.True(box.IsLocked);
            Assert.Equal("typed", box.Text);
            Assert.Equal(1, accepted);
        });
    }

    [Fact]
    public void TheKeysF2AndEnter_StartTheEditingOfALockedBox()
    {
        StaThread.Run(() =>
        {
            LockableTextBox first = Show(UIStyles.TextBoxes.CreateLockable("abc"));
            LockableTextBox second = Show(UIStyles.TextBoxes.CreateLockable("abc"));

            Assert.True(Key(first, Keys.F2));
            Assert.False(first.IsLocked);

            Assert.True(Key(second, Keys.Enter));
            Assert.False(second.IsLocked);
        });
    }

    [Fact]
    public void OtherKeys_AreNotTaken_AndEnterInAMultilineBoxIsANewLine_CtrlEnterAccepts()
    {
        StaThread.Run(() =>
        {
            LockableTextBox locked = Show(UIStyles.TextBoxes.CreateLockable("abc"));
            Assert.False(Key(locked, Keys.A));
            Assert.False(Key(locked, Keys.Escape));
            Assert.True(locked.IsLocked);

            LockableTextBox multi = UIStyles.TextBoxes.CreateLockable("abc", locked: false);
            multi.Multiline = true;
            Show(multi);

            Assert.False(Key(multi, Keys.Enter));
            Assert.False(multi.IsLocked);
            Assert.True(Key(multi, Keys.Enter | Keys.Control));
            Assert.True(multi.IsLocked);
        });
    }

    [Fact]
    public void SettingIsLockedFromCode_LocksOrUnlocks_RaisesOnlyLockedChanged_AndKeepsTheText()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        int changed = 0;
        int accepted = 0;
        int cancelled = 0;
        box.LockedChanged += (s, e) => changed++;
        box.EditAccepted += (s, e) => accepted++;
        box.EditCancelled += (s, e) => cancelled++;

        box.IsLocked = false;
        box.Text = "other";
        box.IsLocked = true;
        box.IsLocked = true;

        Assert.Equal(2, changed);
        Assert.Equal(0, accepted);
        Assert.Equal(0, cancelled);
        Assert.Equal("other", box.Text);
        Assert.False(box.Focused);
    }

    [Fact]
    public void ADisabledLockableBox_CannotBeUnlocked()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        box.Enabled = false;

        ClickInner(box);
        box.BeginEdit();
        Key(box, Keys.F2);

        Assert.True(box.IsLocked);
    }

    [Fact]
    public void ALockedBox_LooksSetApart_AndLocksBackToTheOrdinaryLookWhenUnlocked()
    {
        LockableTextBox box = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        Color locked = box.BackColor;

        box.IsLocked = false;

        Assert.NotEqual(locked, box.BackColor);
        Assert.Equal(UIColors.BackgroundLight, box.BackColor);
    }

    // ---- the box for a password ----

    [Fact]
    public void APasswordBox_HidesWhatIsTyped_AndTheEyeShowsItAndHidesItAgain()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreatePassword("secret"));

        Assert.True(box.UseSystemPasswordChar);
        Assert.Equal("secret", box.Text);
        Assert.Equal("\U0001F441", box.GetPrivateField<string>("_innerGlyph"));
        Assert.False(box.GetPrivateField<bool>("_innerActive"));

        ClickInner(box);
        Assert.False(box.UseSystemPasswordChar);
        Assert.True(box.GetPrivateField<bool>("_innerActive"));
        Assert.Equal("secret", box.Text);

        ClickInner(box);
        Assert.True(box.UseSystemPasswordChar);
        Assert.False(box.GetPrivateField<bool>("_innerActive"));
    }

    [Fact]
    public void APasswordBox_ShowsItsTextInATooltipOnlyWhileThePasswordIsShown()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreatePassword(), 70);
        string password = "a very long password that does not fit into the box";

        box.Text = password;
        Assert.Equal(string.Empty, ToolTipOf(box));

        ClickInner(box);
        Assert.Equal(password, ToolTipOf(box));

        ClickInner(box);
        Assert.Equal(string.Empty, ToolTipOf(box));
    }

    // ---- the tooltip with the whole text ----

    [Fact]
    public void ATextThatIsCut_IsTheTooltipOfTheBox_AndATextThatFitsIsNot()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard(), 80);

        box.Text = "short";
        Assert.Equal(string.Empty, ToolTipOf(box));

        box.Text = "a text that is much wider than this little box";
        Assert.Equal("a text that is much wider than this little box", ToolTipOf(box));

        box.Text = "ok";
        Assert.Equal(string.Empty, ToolTipOf(box));
    }

    [Fact]
    public void TheTooltipOfACutText_FollowsTheWidthOfTheBox_AndCanBeSwitchedOff()
    {
        ThemedTextBox box = Show((ThemedTextBox)UIStyles.TextBoxes.CreateStandard(), 80);
        box.Text = "a text that is wider than eighty pixels";
        Assert.NotEqual(string.Empty, ToolTipOf(box));

        box.Width = 400;
        Assert.Equal(string.Empty, ToolTipOf(box));

        box.Width = 80;
        Assert.NotEqual(string.Empty, ToolTipOf(box));

        box.ToolTipWhenCut = false;
        Assert.Equal(string.Empty, ToolTipOf(box));
        Assert.True(UIStyles.TextBoxes.CreateStandard() is ThemedTextBox { ToolTipWhenCut: true });
    }

    [Fact]
    public void AMultilineBox_HasNoTooltipForItsText()
    {
        TextBox box = UIStyles.TextBoxes.CreateStandard();
        box.Multiline = true;
        Show(box, 60);

        box.Text = "a long text in a narrow box that wraps";

        Assert.Equal(string.Empty, ToolTipOf(box));
    }

    // ---- the language ----

    [Fact]
    public void TheTooltipsOfTheButtons_AreInTheLanguage_AndFollowASwitchOfIt()
    {
        LockableTextBox lockable = Show(UIStyles.TextBoxes.CreateLockable("abc"));
        TextBox password = Show(UIStyles.TextBoxes.CreatePassword("abc"));

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.Equal("Bearbeiten", lockable.GetPrivateField<string>("_innerToolTip"));
            Assert.Equal("Passwort anzeigen", password.GetPrivateField<string>("_innerToolTip"));

            lockable.BeginEdit();
            Assert.Equal("Speichern", lockable.GetPrivateField<string>("_innerToolTip"));

            ClickInner(password);
            Assert.Equal("Passwort verbergen", password.GetPrivateField<string>("_innerToolTip"));
        });

        Assert.Equal("Save", lockable.GetPrivateField<string>("_innerToolTip"));
        Assert.Equal("Hide password", password.GetPrivateField<string>("_innerToolTip"));
    }

    [Fact]
    public void EveryText_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "TextBox.Edit", "TextBox.Accept", "TextBox.ShowPassword", "TextBox.HidePassword" });
    }
}
