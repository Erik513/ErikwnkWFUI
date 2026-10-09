using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// What every toast test needs: windows to show toasts over (the test says the window is in
/// front), a look at the toast that is up and at the ones that wait, and a clean state after
/// each test - the toast, the windows, the queue, the delay timers and the seams the tests change
/// (window in front, screen scaling, mouse position, waiting time) are put back.
/// </summary>
public abstract class ToastTestBase : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private readonly Func<Form, int> _originalDpi = ToastForm.OwnerDpi;
    private readonly Func<Point> _originalCursor = ToastForm.CursorPosition;
    private readonly TimeSpan _originalLifetime = ToastForm.PendingLifetime;
    private readonly List<Form> _hosts = new List<Form>();

    protected ToastTestBase()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public virtual void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        ToastForm.OwnerDpi = _originalDpi;
        ToastForm.CursorPosition = _originalCursor;
        ToastForm.PendingLifetime = _originalLifetime;

        Current()?.Dispose();

        foreach (Form host in _hosts)
            host.Dispose();

        Pending().Clear();

        foreach (System.Windows.Forms.Timer timer in Scheduled().ToArray())
        {
            timer.Stop();
            timer.Dispose();
        }

        Scheduled().Clear();
    }

    /// <summary>A window to show toasts over; shown, and disposed after the test.</summary>
    protected Form Host(Point? location = null, Size? client = null)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = location ?? new Point(100, 100),
            ShowInTaskbar = false,
            ClientSize = client ?? new Size(800, 600)
        };
        host.Show();
        _hosts.Add(host);

        return host;
    }

    /// <summary>The toast that is up, or null.</summary>
    protected static ToastForm? Current()
    {
        return (ToastForm?)typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
    }

    /// <summary>A part of the toast that is up (<c>_messageLabel</c>, <c>_titleLabel</c>, <c>_iconLabel</c>).</summary>
    protected static Label Part(string name)
    {
        return Current()!.GetPrivateField<Label>(name)!;
    }

    /// <summary>The toasts that wait for their window: window -> list of waiting toasts.</summary>
    protected static IDictionary Pending()
    {
        return (IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
    }

    /// <summary>How many toasts wait for this window.</summary>
    protected static int WaitingCount(Form owner)
    {
        IDictionary pending = Pending();

        return pending.Contains(owner) ? ((IEnumerable)pending[owner]!).Cast<object>().Count() : 0;
    }

    /// <summary>The timers of the toasts that wait for their delay.</summary>
    protected static List<System.Windows.Forms.Timer> Scheduled()
    {
        return (List<System.Windows.Forms.Timer>)typeof(ToastForm).GetField("_scheduled", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
    }

    /// <summary>The window comes to the front: the toasts that wait for it are due.</summary>
    protected static void Activate(Form owner)
    {
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { owner, EventArgs.Empty });
    }

    /// <summary>The time of the toast is up.</summary>
    protected static void Tick(ToastForm toast)
    {
        toast.InvokePrivate("OnCloseTimerTick", toast, EventArgs.Empty);
    }

    /// <summary>The delay of the first toast that waits for it is over.</summary>
    protected static void EndFirstDelay()
    {
        Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty);
    }
}
