using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The toast hangs on its window: shown only while the application is in front, above its
/// window but not above other programs, moving with the window, never taking the activation.
/// Toasts are shown for real (briefly) over a host form.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastFormTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastFormTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        ToastForm.PendingLifetime = TimeSpan.FromSeconds(60);
        CurrentToast()?.Dispose();
        _host?.Dispose();
        Pending().Clear();
        _second?.Dispose();
    }

    private Form? _second;

    private static System.Collections.IDictionary Pending()
    {
        FieldInfo field = typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (System.Collections.IDictionary)field.GetValue(null)!;
    }

    private static string[] WaitingFor(Form owner)
    {
        System.Collections.IDictionary pending = Pending();

        if (!pending.Contains(owner))
            return new string[0];

        return ((System.Collections.IEnumerable)pending[owner]!)
            .Cast<object>()
            .Select(item => (string)item.GetType().GetField("Message")!.GetValue(item)!)
            .ToArray();
    }

    private static string ShownMessage()
    {
        Label label = CurrentToast()!.GetPrivateField<Label>("_messageLabel")!;

        return label.Text;
    }

    private static void Activate(Form form)
    {
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { form, EventArgs.Empty });
    }

    private static void CloseByTime(ToastForm toast)
    {
        toast.InvokePrivate("OnCloseTimerTick", toast, EventArgs.Empty);
    }

    private static ToastForm? CurrentToast()
    {
        FieldInfo field = typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (ToastForm?)field.GetValue(null);
    }

    private Form CreateHost()
    {
        _host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(500, 300)
        };
        _host.Show();

        return _host;
    }

    [Fact]
    public void AToast_IsShownOverTheWindow_NotTopmost_WithoutTakingTheActivation()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("Done", host);
        ToastForm toast = CurrentToast()!;

        Assert.True(toast.Visible);
        Assert.False(toast.TopMost);
        Assert.Same(host, toast.Owner);
        Assert.True((bool)typeof(ToastForm).GetProperty("ShowWithoutActivation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(toast)!);

        CreateParams parameters = (CreateParams)typeof(Form).GetProperty("CreateParams", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(toast)!;
        Assert.NotEqual(0, parameters.ExStyle & 0x08000000);
    }

    [Fact]
    public void TheToast_IsMediumTransparentByDefault_AndTakesAnotherLevel()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("Default", host);
        Assert.Equal(PopupTransparency.Medium, CurrentToast()!.Transparency);
        Assert.Equal(0.80, CurrentToast()!.Opacity, 2);

        ToastForm.ShowToast("Opaque", host, PopupTransparency.Opaque);
        Assert.Equal(1.0, CurrentToast()!.Opacity, 2);

        ToastForm.ShowToast("High", host, PopupTransparency.High);
        Assert.True(CurrentToast()!.Opacity < 0.80);

        ToastForm.ShowToast("Low", host, PopupTransparency.Low);
        Assert.True(CurrentToast()!.Opacity > 0.80 && CurrentToast()!.Opacity < 1.0);
    }

    [Fact]
    public void AWaitingToast_KeepsItsTransparency()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Waits", host, PopupTransparency.Opaque);

        PopupSupport.OwnerIsActive = _ => true;
        Activate(host);

        Assert.Equal(PopupTransparency.Opaque, CurrentToast()!.Transparency);
    }

    [Fact]
    public void WhileItsWindowIsNotInFront_TheToastIsNotShown_ButWaits()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast("Done", host);

        Assert.Null(CurrentToast());
        Assert.Equal(new[] { "Done" }, WaitingFor(host));
    }

    [Fact]
    public void WhenTheWindowComesToTheFront_TheWaitingToastIsShown()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Saved", host);

        Activate(host);
        Assert.Null(CurrentToast());

        PopupSupport.OwnerIsActive = _ => true;
        Activate(host);

        Assert.Equal("Saved", ShownMessage());
        Assert.Empty(WaitingFor(host));
    }

    [Fact]
    public void Waiting_KeepsTheNewestThree_AndTheSameTextTwiceIsOne()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;

        foreach (string message in new[] { "A", "B", "B", "C", "D", "E" })
            ToastForm.ShowToast(message, host);

        Assert.Equal(new[] { "C", "D", "E" }, WaitingFor(host));
    }

    [Fact]
    public void AToastThatWaitedTooLong_IsDropped()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Old", host);

        ToastForm.PendingLifetime = TimeSpan.Zero;
        PopupSupport.OwnerIsActive = _ => true;
        Activate(host);

        Assert.Null(CurrentToast());
        Assert.Empty(WaitingFor(host));
    }

    [Fact]
    public void SeveralWaitingToasts_AreShownOneAfterTheOther_EachWhenTheLastOneClosed()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("One", host);
        ToastForm.ShowToast("Two", host);

        PopupSupport.OwnerIsActive = _ => true;
        Activate(host);
        Assert.Equal("One", ShownMessage());

        CloseByTime(CurrentToast()!);
        Assert.Equal("Two", ShownMessage());
        Assert.Empty(WaitingFor(host));
    }

    [Fact]
    public void ANewToast_ReplacesTheOneThatIsShown_AsBefore()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("First", host);
        ToastForm.ShowToast("Second", host);

        Assert.Equal("Second", ShownMessage());
    }

    [Fact]
    public void AToast_WaitsForItsOwnWindow_NotForAnother()
    {
        Form host = CreateHost();
        _second = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, ClientSize = new Size(300, 200) };
        _second.Show();
        PopupSupport.OwnerIsActive = owner => owner == _second;

        ToastForm.ShowToast("For the first", host);
        Assert.Null(CurrentToast());
        Assert.Equal(new[] { "For the first" }, WaitingFor(host));

        ToastForm.ShowToast("For the second", _second);
        Assert.Equal("For the second", ShownMessage());
    }

    [Fact]
    public void AClosedWindow_TakesItsWaitingToastsWithIt()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Lost with the window", host);

        host.Close();

        Assert.Empty(WaitingFor(host));
    }

    [Fact]
    public void TheOwnerIsActive_WhenItOrAWindowOwnedByItIsInFront()
    {
        using Form owner = new Form();
        using Form other = new Form();
        using Form ownedDialog = new Form { Owner = owner };
        using Form foreignDialog = new Form { Owner = other };

        Assert.True(PopupSupport.IsActiveFor(owner, owner));
        Assert.True(PopupSupport.IsActiveFor(ownedDialog, owner));
        Assert.False(PopupSupport.IsActiveFor(foreignDialog, owner));
        Assert.False(PopupSupport.IsActiveFor(other, owner));
        Assert.False(PopupSupport.IsActiveFor(null!, owner));
        Assert.True(PopupSupport.IsActiveFor(other, null!));
    }

    [Fact]
    public void WhenTheWindowIsMoved_TheToastGoesWithIt()
    {
        Form host = CreateHost();
        ToastForm.ShowToast("Done", host);
        ToastForm toast = CurrentToast()!;
        Point before = toast.Location;

        host.Location = new Point(host.Left + 35, host.Top + 20);

        Assert.Equal(new Point(before.X + 35, before.Y + 20), toast.Location);
    }
}
