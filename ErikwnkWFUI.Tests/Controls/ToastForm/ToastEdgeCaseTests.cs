using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// Odd uses of the toast: calls from another thread, an empty message, a toast that is disposed,
/// a window that is small or reaches beyond the screen, a long message, a window that closes
/// while a toast waits or is up. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastEdgeCaseTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private readonly List<Form> _forms = new List<Form>();

    public ToastEdgeCaseTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();

        foreach (Form form in _forms)
            form.Dispose();

        ((System.Collections.IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
    }

    private Form Host(Point location, Size client)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = location,
            ShowInTaskbar = false,
            ClientSize = client
        };
        host.Show();
        _forms.Add(host);

        return host;
    }

    private static ToastForm? Current()
    {
        FieldInfo field = typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (ToastForm?)field.GetValue(null);
    }

    private static Rectangle Area(Form host)
    {
        return Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea);
    }

    [Fact]
    public void AMessageWithoutText_ShowsNoToast()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));

        ToastForm.ShowToast("", host);
        ToastForm.ShowToast("   ", host);
        ToastForm.ShowToast(null!, host);

        Assert.Null(Current());
    }

    [Fact]
    public void ACallFromAnotherThread_ShowsTheToastOnTheWindowsThread()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));
        Exception? thrown = null;

        Thread worker = new Thread(() =>
        {
            try
            {
                ToastForm.ShowToast("From the background", host);
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
        });
        worker.Start();
        worker.Join();

        for (int i = 0; i < 20 && Current() == null; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }

        Assert.Null(thrown);
        Assert.NotNull(Current());
        Assert.True(Current()!.Visible);

        // It lives on this thread (the window's), not on the thread that asked for it.
        Assert.False(Current()!.InvokeRequired);
    }

    [Fact]
    public void ADisposedToast_TakesItsSettingsWithoutAnError()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));
        ToastForm.ShowToast("x", host);
        ToastForm toast = Current()!;
        toast.Dispose();

        Exception? thrown = Record.Exception(() =>
        {
            toast.Transparency = PopupTransparency.High;
            toast.Duration = PopupDuration.Long;
            toast.Colors = ErikwnkWFUI.UIStyles.Colors.Sets.Green;
            toast.Colors = null;
            toast.Dispose();
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void AToast_StaysInsideTheWindow_AsFarAsItFits()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));

        ToastForm.ShowToast("Inside", host);

        Assert.True(Area(host).Contains(Current()!.Bounds), "outside the window: " + Current()!.Bounds + " in " + Area(host));
    }

    [Fact]
    public void AWindowThatReachesBeyondTheScreen_GetsAToastInsideTheVisiblePart()
    {
        Rectangle screen = SystemInformation.VirtualScreen;
        Form host = Host(new Point(screen.Right - 420, 100), new Size(500, 300));

        ToastForm.ShowToast("Half there", host);

        Assert.True(Area(host).Right < host.Right, "the window is not beyond the screen");
        Assert.True(Area(host).Contains(Current()!.Bounds), "outside the visible part: " + Current()!.Bounds + " in " + Area(host));
    }

    [Fact]
    public void AWindowNarrowerThanTheToast_GetsItAtItsLeftEdge_WithoutAnError()
    {
        Form host = Host(new Point(100, 100), new Size(200, 120));

        Exception? thrown = Record.Exception(() => ToastForm.ShowToast("In a small window", host));

        // It cannot fit in the width: it starts at the window's edge; in the height it still fits.
        Assert.Null(thrown);
        Assert.Equal(Area(host).Left, Current()!.Left);
        Assert.True(Current()!.Top >= Area(host).Top && Current()!.Bottom <= Area(host).Bottom);
    }

    [Fact]
    public void ALongMessage_DoesNotChangeTheSizeOfTheToast()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));

        ToastForm.ShowToast("Short", host);
        Size shortSize = Current()!.Size;
        ToastForm.ShowToast(new string('x', 400) + " and a lot more words that do not fit on one line of the toast at all", host);

        Assert.Equal(shortSize, Current()!.Size);
    }

    [Fact]
    public void WhenTheWindowClosesWhileAToastIsUp_ThePopupGoesWithIt_AndLaterCallsAreHarmless()
    {
        Form host = Host(new Point(100, 100), new Size(500, 300));
        ToastForm.ShowToast("Up", host);
        ToastForm toast = Current()!;

        host.Close();

        Exception? thrown = Record.Exception(() =>
        {
            ToastForm.ShowToast("After", host);
            toast.Transparency = PopupTransparency.Low;
        });

        Assert.Null(thrown);
        Assert.True(toast.IsDisposed);
    }
}
