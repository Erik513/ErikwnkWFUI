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

        foreach (System.Windows.Forms.Timer timer in Scheduled().ToArray())
        {
            timer.Stop();
            timer.Dispose();
        }

        Scheduled().Clear();
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
            .Select(item => ((InfoPopupSection)item.GetType().GetField("Content")!.GetValue(item)!).Text!)
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

        ToastForm.ShowToast("Opaque", host, transparency: PopupTransparency.Opaque);
        Assert.Equal(1.0, CurrentToast()!.Opacity, 2);

        ToastForm.ShowToast("High", host, transparency: PopupTransparency.High);
        Assert.True(CurrentToast()!.Opacity < 0.80);

        ToastForm.ShowToast("Low", host, transparency: PopupTransparency.Low);
        Assert.True(CurrentToast()!.Opacity > 0.80 && CurrentToast()!.Opacity < 1.0);
    }

    private static List<System.Windows.Forms.Timer> Scheduled()
    {
        FieldInfo field = typeof(ToastForm).GetField("_scheduled", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (List<System.Windows.Forms.Timer>)field.GetValue(null)!;
    }

    [Fact]
    public void WithoutADelay_TheToastShowsAtOnce_ByDefault()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("Now", host);

        Assert.NotNull(CurrentToast());
        Assert.Empty(Scheduled());
    }

    [Fact]
    public void WithADelay_TheToastWaitsForItsTime_ThenShows()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("Later", host, delay: PopupDelay.Medium);

        Assert.Null(CurrentToast());
        Assert.Single(Scheduled());
        Assert.Equal(400, Scheduled()[0].Interval);

        Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty);

        Assert.Equal("Later", ShownMessage());
        Assert.Empty(Scheduled());
    }

    [Fact]
    public void TheDelayLevels_AreOrdered()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("a", host, delay: PopupDelay.Short);
        ToastForm.ShowToast("b", host, delay: PopupDelay.Long);

        Assert.Equal(2, Scheduled().Count);
        Assert.True(Scheduled()[0].Interval < 400 && 400 < Scheduled()[1].Interval, "the levels are not ordered");

        foreach (System.Windows.Forms.Timer timer in Scheduled().ToArray())
            timer.InvokePrivate("OnTick", EventArgs.Empty);
    }

    [Fact]
    public void AToastWhoseWindowIsNotInFrontWhenTheDelayEnds_Waits_LikeAnyOther()
    {
        Form host = CreateHost();
        ToastForm.ShowToast("Late", host, delay: PopupDelay.Short);

        PopupSupport.OwnerIsActive = _ => false;
        Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty);

        Assert.Null(CurrentToast());
        Assert.Equal(new[] { "Late" }, WaitingFor(host));
    }

    [Fact]
    public void AClosedWindow_IsNotAProblem_WhenTheDelayEnds()
    {
        Form host = CreateHost();
        ToastForm.ShowToast("Gone", host, delay: PopupDelay.Short);
        host.Dispose();

        Exception? thrown = Record.Exception(() => Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty));

        Assert.Null(thrown);
        Assert.Null(CurrentToast());
    }

    private static int Interval(ToastForm toast) => toast.GetPrivateField<System.Windows.Forms.Timer>("_closeTimer")!.Interval;

    [Fact]
    public void TheDuration_IsMediumByDefault_AndTheThreeLevelsAreOrdered()
    {
        Form host = CreateHost();

        ToastForm.ShowToast("Default", host);
        Assert.Equal(PopupDuration.Medium, CurrentToast()!.Duration);
        Assert.Equal(2500, Interval(CurrentToast()!));

        ToastForm.ShowToast("Short", host, duration: PopupDuration.Short);
        int shortTime = Interval(CurrentToast()!);

        ToastForm.ShowToast("Long", host, duration: PopupDuration.Long);
        int longTime = Interval(CurrentToast()!);

        Assert.True(shortTime < 2500 && 2500 < longTime, "the levels are not ordered");
        Assert.Equal(PopupDuration.Long, CurrentToast()!.Duration);
    }

    [Fact]
    public void ADurationChangedOnTheToast_ChangesItsTimer()
    {
        Form host = CreateHost();
        ToastForm.ShowToast("x", host);
        ToastForm toast = CurrentToast()!;

        toast.Duration = PopupDuration.Short;

        Assert.Equal(1500, Interval(toast));
    }

    [Fact]
    public void AWaitingToast_KeepsItsDuration_AndItsTimeRunsFromWhenItIsShown()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Waits", host, duration: PopupDuration.Long);

        PopupSupport.OwnerIsActive = _ => true;
        Activate(host);

        Assert.Equal(PopupDuration.Long, CurrentToast()!.Duration);
        Assert.Equal(4000, Interval(CurrentToast()!));
        Assert.True(CurrentToast()!.GetPrivateField<System.Windows.Forms.Timer>("_closeTimer")!.Enabled, "the time did not start with the show");
    }

    [Fact]
    public void AWaitingToast_KeepsItsTransparency()
    {
        Form host = CreateHost();
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Waits", host, transparency: PopupTransparency.Opaque);

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
