using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>A click closes the toast; a toast the mouse is on stays until the mouse has left.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastInteractionTests : ToastTestBase
{
    private static void Click(Control control)
    {
        control.GetType().GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(control, new object[] { EventArgs.Empty });
    }

    [Fact]
    public void AClickOnTheToast_ClosesIt()
    {
        ToastForm.ShowToast("Click me", Host());
        ToastForm toast = Current()!;

        Click(toast);

        Assert.True(toast.IsDisposed);
        Assert.Null(Current());
    }

    [Fact]
    public void AClickOnAnyPartOfTheToast_ClosesIt()
    {
        foreach (string name in new[] { "_messageLabel", "_titleLabel", "_iconLabel", "_contentPanel" })
        {
            ToastForm.ShowToast(new InfoPopupSection("Title", "Text"), Host(), symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);
            ToastForm toast = Current()!;

            Click(toast.GetPrivateField<Control>(name)!);

            Assert.True(toast.IsDisposed, "a click on " + name + " did not close the toast");
        }
    }

    [Fact]
    public void AClick_ShowsTheNextToastThatWaits()
    {
        Form host = Host();
        ToastForm.ShowToast("First", host);
        ToastForm first = Current()!;

        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Second", host);
        PopupSupport.OwnerIsActive = _ => true;

        Click(first);

        Assert.True(first.IsDisposed);
        Assert.NotNull(Current());
        Assert.Equal("Second", Current()!.GetPrivateField<Label>("_messageLabel")!.Text);
    }

    [Fact]
    public void AClickOnAToastThatIsAlreadyGone_IsHarmless()
    {
        ToastForm.ShowToast("Gone", Host());
        ToastForm toast = Current()!;
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.InvokePrivate("OnToastClick", toast, EventArgs.Empty)));
    }

    [Fact]
    public void TheToast_ShowsAHandCursor()
    {
        ToastForm.ShowToast("Hand", Host());

        Assert.Equal(Cursors.Hand, Current()!.Cursor);
    }

    [Fact]
    public void WhenTheTimeIsUp_ATimeNotOnTheToast_ClosesIt()
    {
        ToastForm.ShowToast("Done", Host());
        ToastForm toast = Current()!;

        Tick(toast);

        Assert.True(toast.IsDisposed);
    }

    [Fact]
    public void WhenTheTimeIsUp_AMouseOnTheToast_KeepsItOpen_UntilTheMouseHasLeft()
    {
        ToastForm.ShowToast("Reading", Host());
        ToastForm toast = Current()!;
        ToastForm.CursorPosition = () => new Point(toast.Left + toast.Width / 2, toast.Top + toast.Height / 2);

        Tick(toast);
        Tick(toast);

        Assert.False(toast.IsDisposed, "the toast closed with the mouse on it");
        Assert.Same(toast, Current());

        ToastForm.CursorPosition = () => new Point(toast.Right + 50, toast.Bottom + 50);
        Tick(toast);

        Assert.True(toast.IsDisposed);
    }

    [Fact]
    public void AMouseOnTheToast_ChecksAgainSoon_NotAfterTheWholeDuration()
    {
        ToastForm.ShowToast("Reading", Host(), duration: PopupDuration.Long);
        ToastForm toast = Current()!;
        ToastForm.CursorPosition = () => toast.Location;

        Tick(toast);

        System.Windows.Forms.Timer timer = toast.GetPrivateField<System.Windows.Forms.Timer>("_closeTimer")!;
        Assert.True(timer.Enabled);
        Assert.True(timer.Interval <= 500, "the next look comes after " + timer.Interval + " ms");
    }

    [Fact]
    public void AMouseOnTheToast_KeepsAWaitingToastWaiting()
    {
        Form host = Host();
        ToastForm.ShowToast("First", host);
        ToastForm first = Current()!;
        PopupSupport.OwnerIsActive = _ => false;
        ToastForm.ShowToast("Second", host);
        PopupSupport.OwnerIsActive = _ => true;
        ToastForm.CursorPosition = () => first.Location;

        Tick(first);

        Assert.Same(first, Current());
        Assert.False(first.IsDisposed);
    }
}
