using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The content of the toast: an optional title above the message, own colors for each part, the
/// height that grows with both, and the content of a toast that has to wait.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastContentTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastContentTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();
        _host?.Dispose();
        ((System.Collections.IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
    }

    private Form Host()
    {
        _host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            ShowInTaskbar = false,
            ClientSize = new Size(600, 400)
        };
        _host.Show();

        return _host;
    }

    private static ToastForm? Current()
    {
        return (ToastForm?)typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
    }

    private static Label Part(string name) => Current()!.GetPrivateField<Label>(name)!;

    [Fact]
    public void AMessageOnly_ShowsNoTitle()
    {
        ToastForm.ShowToast("Just a message", Host());

        Assert.False(Part("_titleLabel").Visible);
        Assert.Equal("Just a message", Part("_messageLabel").Text);
    }

    [Fact]
    public void ATitleAndAMessage_AreBothShown_TheTitleAbove()
    {
        ToastForm.ShowToast(new InfoPopupSection("Saved", "The file is on disk."), Host());

        Assert.True(Part("_titleLabel").Visible);
        Assert.Equal("Saved", Part("_titleLabel").Text);
        Assert.Equal("The file is on disk.", Part("_messageLabel").Text);
        Assert.Equal(DockStyle.Top, Part("_titleLabel").Dock);

        // The title sits above the message; neither covers the other.
        Assert.True(Part("_titleLabel").Bottom <= Part("_messageLabel").Top, "title " + Part("_titleLabel").Bounds + ", message " + Part("_messageLabel").Bounds);
        Assert.True(Part("_titleLabel").Height > 0 && Part("_messageLabel").Height > 0);
    }

    [Fact]
    public void ATitleOnly_ShowsAToastWithoutAMessage()
    {
        ToastForm.ShowToast(new InfoPopupSection("Done", null), Host());

        Assert.NotNull(Current());
        Assert.True(Part("_titleLabel").Visible);
        Assert.Equal("", Part("_messageLabel").Text);

        // The title is in the middle of the height, as the only text.
        Label title = Part("_titleLabel");
        Assert.True(Math.Abs(title.Top + title.Height / 2 - Current()!.Height / 2) <= 1, "title at " + title.Bounds + " in " + Current()!.Height);
    }

    [Fact]
    public void ATitle_MakesTheToastNoLowerThanWithoutOne_AndALongMessageHigherStill()
    {
        Form host = Host();

        ToastForm.ShowToast("A message", host);
        int plain = Current()!.Height;
        ToastForm.ShowToast(new InfoPopupSection("A title", "A message"), host);
        int titled = Current()!.Height;
        ToastForm.ShowToast(new InfoPopupSection("A title", string.Join(" ", Enumerable.Repeat("a long message of many words", 8))), host);
        int tall = Current()!.Height;

        Assert.True(titled >= plain);
        Assert.True(tall > titled);
        Assert.Equal(Current()!.Width, 350 * Current()!.DeviceDpi / 96);
    }

    [Fact]
    public void TheTitleAndTheMessage_FitInsideTheToast()
    {
        Form host = Host();

        foreach (string text in new[] { "Short", string.Join(" ", Enumerable.Repeat("a long message of many words", 60)) })
        {
            ToastForm.ShowToast(new InfoPopupSection("A title", text), host);
            Rectangle inside = Current()!.ClientRectangle;

            Assert.True(inside.Contains(Part("_titleLabel").Bounds) || Part("_titleLabel").Bounds.Top >= 0);
            Assert.True(Part("_titleLabel").Bottom <= Current()!.ClientSize.Height);
            Assert.True(Part("_messageLabel").Bottom <= Current()!.ClientSize.Height);
        }
    }

    [Fact]
    public void WithoutOwnColors_TheTextIsWhiteOrBlackForTheBackground()
    {
        ToastForm.ShowToast(new InfoPopupSection("Title", "Message"), Host());

        Color expected = PopupSupport.GetTextColor(ErikwnkWFUI.UIStyles.Colors.Sets.Gray);

        Assert.Equal(expected, Part("_titleLabel").ForeColor);
        Assert.Equal(expected, Part("_messageLabel").ForeColor);
    }

    [Fact]
    public void OwnColors_AreUsedForTheTitleAndTheMessageSeparately()
    {
        Color red = Color.FromArgb(255, 110, 110);
        Color green = Color.FromArgb(150, 255, 150);

        ToastForm.ShowToast(new InfoPopupSection("Title", "Message") { HeaderColor = red }, Host());

        Color plain = PopupSupport.GetTextColor(ErikwnkWFUI.UIStyles.Colors.Sets.Gray);
        Assert.Equal(red, Part("_titleLabel").ForeColor);
        Assert.Equal(plain, Part("_messageLabel").ForeColor);

        ToastForm.ShowToast(new InfoPopupSection("Title", "Message") { TextColor = green }, Host());

        Assert.Equal(plain, Part("_titleLabel").ForeColor);
        Assert.Equal(green, Part("_messageLabel").ForeColor);
    }

    [Fact]
    public void ChangingTheColorsOrTheTransparency_KeepsTheOwnColors()
    {
        Color red = Color.FromArgb(255, 110, 110);
        ToastForm.ShowToast(new InfoPopupSection("Title", "Message") { HeaderColor = red }, Host());

        Current()!.Colors = ErikwnkWFUI.UIStyles.Colors.Sets.Green;
        Current()!.Transparency = PopupTransparency.High;

        Assert.Equal(red, Part("_titleLabel").ForeColor);
    }

    [Fact]
    public void TheCallerChangingTheSection_AfterTheCall_DoesNotChangeTheToast()
    {
        InfoPopupSection section = new InfoPopupSection("Title", "Message");

        ToastForm.ShowToast(section, Host());
        section.Header = "Changed";
        section.Text = "Changed";

        Assert.Equal("Title", Part("_titleLabel").Text);
        Assert.Equal("Message", Part("_messageLabel").Text);
    }

    [Fact]
    public void AToastThatWaits_KeepsItsTitleAndColors()
    {
        Form host = Host();
        Color red = Color.FromArgb(255, 110, 110);
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast(new InfoPopupSection("Waiting", "Message") { HeaderColor = red }, host);

        Assert.Null(Current());

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

        Assert.Equal("Waiting", Part("_titleLabel").Text);
        Assert.Equal(red, Part("_titleLabel").ForeColor);
    }

    [Fact]
    public void TwoWaitingToasts_WithTheSameTextButAnotherTitle_AreNotTheSame()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast(new InfoPopupSection("One", "Same"), host);
        ToastForm.ShowToast(new InfoPopupSection("Two", "Same"), host);
        ToastForm.ShowToast(new InfoPopupSection("Two", "Same"), host);

        var queue = (System.Collections.IEnumerable)((System.Collections.IDictionary)typeof(ToastForm)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)[host]!;

        Assert.Equal(2, queue.Cast<object>().Count());
    }

    [Fact]
    public void TheFactory_ShowsATitledToast_AlsoInThePrimaryColor()
    {
        Form host = Host();

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard(new InfoPopupSection("Std", "Message"), host);
        Assert.Equal("Std", Part("_titleLabel").Text);

        ErikwnkWFUI.UIStyles.Toasts.ShowPrimary(new InfoPopupSection("Prim", "Message"), host);
        Assert.Equal("Prim", Part("_titleLabel").Text);
        Assert.Equal(ErikwnkWFUI.UIStyles.Colors.Sets.Primary.Basic, Current()!.Colors!.Basic);
    }
}
