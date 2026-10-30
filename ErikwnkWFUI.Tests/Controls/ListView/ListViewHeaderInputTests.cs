using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// The input of the header, which is a window of its own: a press at a divider resizes the column by the distance the mouse moved, one at a
/// divider of a column that may not be resized does nothing, another drag starts the reordering. The messages are sent to the header window itself,
/// the mouse is put where the test wants it (<c>CursorPosition</c>, <c>PressedButtons</c>).
/// </summary>
public class ListViewHeaderInputTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private const int LVM_GETHEADER = 0x101F;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_CANCELMODE = 0x001F;
    private const int WM_SETCURSOR = 0x0020;

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private (WfuiListView List, IntPtr Header) Show()
    {
        WfuiListView list = new WfuiListView { View = View.Details, FullRowSelect = true };
        list.Columns.Add("A", 100);
        list.Columns.Add("B", 100);
        list.Columns.Add("C", 100);
        list.Items.Add(new ListViewItem(new[] { "1", "2", "3" }));
        list.Dock = DockStyle.Fill;
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 200)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return (list, SendMessage(list.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero));
    }

    private static IntPtr At(int x, int y = 5) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static void Send(IntPtr header, int message, int x, int y = 5) => SendMessage(header, message, IntPtr.Zero, At(x, y));

    // The widths that the system wants a column to have (the one of a column header that is dragged is what the screen distance makes of it).
    [Fact]
    public void APressAtADivider_ResizesTheColumnByTheDistanceTheMouseMoved_AndTheReleaseEndsIt()
    {
        (WfuiListView list, IntPtr header) = Show();
        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => new Point(500, 50);

        Send(header, WM_LBUTTONDOWN, 99);
        list.CursorPosition = () => new Point(540, 50);
        Send(header, WM_MOUSEMOVE, 139);

        Assert.Equal(140, list.Columns[0].Width);

        list.CursorPosition = () => new Point(530, 50);
        Send(header, WM_LBUTTONUP, 129);

        Assert.Equal(130, list.Columns[0].Width);
        Assert.Null(list.GetPrivateField<object?>("_headerInputSubclass")!.GetPrivateField<object?>("_resizingColumn"));
    }

    [Fact]
    public void TheMouseMoveWithoutTheButton_EndsTheResize_AndACancelLeavesItToo()
    {
        (WfuiListView list, IntPtr header) = Show();
        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => new Point(500, 50);
        Send(header, WM_LBUTTONDOWN, 99);

        list.PressedButtons = () => MouseButtons.None;
        Send(header, WM_MOUSEMOVE, 120);
        Assert.Equal(100, list.Columns[0].Width);

        list.PressedButtons = () => MouseButtons.Left;
        Send(header, WM_LBUTTONDOWN, 99);
        SendMessage(header, WM_CANCELMODE, IntPtr.Zero, IntPtr.Zero);
        Assert.Null(list.GetPrivateField<object?>("_headerInputSubclass")!.GetPrivateField<object?>("_resizingColumn"));
    }

    [Fact]
    public void ADragNarrowerThanTheMinimum_StopsAtTheMinimumWidth()
    {
        (WfuiListView list, IntPtr header) = Show();
        list.MinimumColumnWidth = 60;
        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => new Point(500, 50);
        Send(header, WM_LBUTTONDOWN, 99);

        list.CursorPosition = () => new Point(400, 50);
        Send(header, WM_MOUSEMOVE, 0);

        Assert.Equal(60, list.Columns[0].Width);
        Send(header, WM_LBUTTONUP, 0);
    }

    [Fact]
    public void ADividerOfAColumnThatMayNotBeResized_IsLeftAlone_AndTheCursorStaysTheArrow()
    {
        (WfuiListView list, IntPtr header) = Show();
        list.SetColumnResizable(1, false);
        list.PressedButtons = () => MouseButtons.None;
        list.CursorPosition = () => new Point(100, 50);

        Send(header, WM_MOUSEMOVE, 199);
        SendMessage(header, WM_SETCURSOR, header, (IntPtr)((WM_MOUSEMOVE << 16) | 1));

        list.PressedButtons = () => MouseButtons.Left;
        Send(header, WM_LBUTTONDOWN, 199);
        list.CursorPosition = () => new Point(160, 50);
        Send(header, WM_MOUSEMOVE, 259);

        Assert.Equal(100, list.Columns[1].Width);
        Send(header, WM_LBUTTONUP, 259);
    }

    [Fact]
    public void APressInTheMiddleOfAColumn_WaitsForADragWideEnough_ThenReordersAndAReleaseCancelsIt()
    {
        // (AllowDrop registers the window as a drop target, which needs an STA thread.)
        StaThread.Run(() =>
        {
        (WfuiListView list, IntPtr header) = Show();
        list.AllowDrop = true;
        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => new Point(500, 50);
        object subclass = list.GetPrivateField<object>("_headerInputSubclass")!;

        Send(header, WM_LBUTTONDOWN, 50);
        Assert.Equal(0, subclass.GetPrivateField<int>("_pendingColumnIndex"));

        // (A move that does not reach the width of a drag starts nothing.)
        list.CursorPosition = () => new Point(502, 50);
        Send(header, WM_MOUSEMOVE, 52);
        Assert.Equal(0, subclass.GetPrivateField<int>("_pendingColumnIndex"));

        list.PressedButtons = () => MouseButtons.None;
        Send(header, WM_MOUSEMOVE, 52);
        Assert.Equal(-1, subclass.GetPrivateField<int>("_pendingColumnIndex"));

        list.PressedButtons = () => MouseButtons.Left;
        Send(header, WM_LBUTTONDOWN, 50);
        Send(header, WM_LBUTTONUP, 50);
        Assert.Equal(-1, subclass.GetPrivateField<int>("_pendingColumnIndex"));
        Dispose();
        });
    }

    [Fact]
    public void AColumnThatMayNotBeReordered_AndAPressWithoutDrop_StartNoDrag()
    {
        StaThread.Run(() =>
        {
        (WfuiListView list, IntPtr header) = Show();
        object subclass = list.GetPrivateField<object>("_headerInputSubclass")!;
        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => new Point(500, 50);

        list.AllowDrop = false;
        Send(header, WM_LBUTTONDOWN, 50);
        Assert.Equal(-1, subclass.GetPrivateField<int>("_pendingColumnIndex"));
        Send(header, WM_LBUTTONUP, 50);

        list.AllowDrop = true;
        list.SetColumnReorderable(0, false);
        Send(header, WM_LBUTTONDOWN, 50);
        Assert.Equal(-1, subclass.GetPrivateField<int>("_pendingColumnIndex"));
        Send(header, WM_LBUTTONUP, 50);
        Dispose();
        });
    }

    [Fact]
    public void TheHeader_IsPaintedBeyondTheLastColumn_InTheColorOfTheHeader()
    {
        (WfuiListView list, IntPtr header) = Show();

        SendMessage(header, 0x000F, IntPtr.Zero, IntPtr.Zero);
        list.Columns[0].Width = 120;

        Assert.Equal(3, list.Columns.Count);
    }
}
