using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The texts of the buttons in the text boxes (the tooltips of the pencil, the check mark and the eye) are in the language of the library, also when it is switched while the box is shown, and a box that is disposed stops listening. [Collection]: the language is static state of the process.</summary>
[Collection(LanguageTestCollection.Name)]
public class TextBoxLanguageTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private readonly TextBoxHost _host = new TextBoxHost();

    public void Dispose()
    {
        _host.Dispose();
    }

    private static void ClickEye(TextBox box)
    {
        Rectangle bounds = (Rectangle)box.GetType().GetProperty("InnerBounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(box)!;
        int x = bounds.Left + bounds.Width / 2;
        int y = bounds.Top + bounds.Height / 2;
        IntPtr point = (IntPtr)((y << 16) | (x & 0xFFFF));

        SendMessage(box.Handle, 0x0201, IntPtr.Zero, point);
        SendMessage(box.Handle, 0x0202, IntPtr.Zero, point);
    }

    private static string Tip(TextBox box) => box.GetPrivateField<string>("_innerToolTip")!;

    private static int Listeners()
    {
        FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", BindingFlags.NonPublic | BindingFlags.Static);

        return (field?.GetValue(null) as MulticastDelegate)?.GetInvocationList().Length ?? 0;
    }

    [Fact]
    public void EveryText_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "TextBox.Edit", "TextBox.Accept", "TextBox.ShowPassword", "TextBox.HidePassword" });
    }

    [Fact]
    public void TheTooltipsOfTheButtons_AreInTheLanguage_AndFollowASwitchOfItWhileTheBoxIsShown()
    {
        LockableTextBox lockable = _host.Show(UIStyles.TextBoxes.CreateLockable("abc"));
        TextBox password = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.Equal("Bearbeiten", Tip(lockable));
            Assert.Equal("Passwort anzeigen", Tip(password));

            lockable.BeginEdit();
            Assert.Equal("Speichern", Tip(lockable));

            ClickEye(password);
            Assert.Equal("Passwort verbergen", Tip(password));
        });

        Assert.Equal("Save", Tip(lockable));
        Assert.Equal("Hide password", Tip(password));
    }

    [Fact]
    public void ABoxThatIsMadeInAnotherLanguage_HasThatLanguageAtOnce()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            LockableTextBox lockable = _host.Show(UIStyles.TextBoxes.CreateLockable("abc"));
            TextBox password = _host.Show(UIStyles.TextBoxes.CreatePassword("abc"));

            Assert.Equal("Bearbeiten", Tip(lockable));
            Assert.Equal("Passwort anzeigen", Tip(password));
        });
    }

    [Fact]
    public void ADisposedBox_NoLongerListensForTheLanguage_AndTheSwitchDoesNotThrow()
    {
        int before = Listeners();
        LockableTextBox lockable = UIStyles.TextBoxes.CreateLockable("abc");
        TextBox password = UIStyles.TextBoxes.CreatePassword("abc");
        Assert.Equal(before + 2, Listeners());

        lockable.Dispose();
        password.Dispose();

        Assert.Equal(before, Listeners());
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () => { });
    }
}
