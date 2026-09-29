using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The scroll arrows a native tab control shows when its tabs overflow are
/// its own child window - TabScrollButtons takes over their drawing, so
/// these check that it actually attaches to that window, hit-tests the two
/// halves right, and paints them in the tab control's colors. Needs a
/// shown form: the arrows only exist once the control has a real size and
/// the tabs really don't fit. [Collection] (see AccentColorTestCollection):
/// the colors read UIColors.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabScrollButtonsTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    private const int WM_HSCROLL = 0x0114;
    private const int SB_THUMBPOSITION = 4;

    // Scrolls the tab strip the way the arrows do: the native up-down
    // reports its new position to the tab control.
    private static void ScrollTo(WfuiTabControl tabs, int position)
    {
        SendMessage(tabs.Handle, WM_HSCROLL, (System.IntPtr)(SB_THUMBPOSITION | (position << 16)), GetButtons(tabs).Handle);
        Application.DoEvents();
    }

    private static Form ShowOverflowingTabs(out WfuiTabControl tabs, TabAlignment alignment = TabAlignment.Top)
    {
        tabs = new WfuiTabControl { Dock = DockStyle.Fill, Alignment = alignment };

        for (int i = 0; i < 9; i++)
        {
            tabs.TabPages.Add("Tab number " + i);
        }

        Form host = new Form
        {
            ClientSize = new Size(300, 120),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, -3000),
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None
        };

        host.Controls.Add(tabs);
        host.Show();
        Application.DoEvents();
        return host;
    }

    private static TabScrollButtons GetButtons(WfuiTabControl tabs)
    {
        return tabs.GetPrivateField<TabScrollButtons>("_scrollButtons")!;
    }

    [Fact]
    public void OverflowingTabs_GetTheirScrollArrowsTakenOver()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);

            Assert.NotEqual(System.IntPtr.Zero, GetButtons(tabs).Handle);
        });
    }

    // expected: 0 = none, 1 = first half, 2 = second half.
    [Theory]
    [InlineData(2, 5, true, 1)]
    [InlineData(30, 5, true, 2)]
    [InlineData(20, 5, true, 2)]
    [InlineData(5, 3, false, 1)]
    [InlineData(5, 25, false, 2)]
    [InlineData(-1, 5, true, 0)]
    [InlineData(40, 5, true, 0)]
    [InlineData(5, 20, true, 0)]
    public void GetPartAt_PicksTheHalfUnderThePoint(int x, int y, bool horizontal, int expected)
    {
        Size size = horizontal ? new Size(40, 20) : new Size(20, 40);

        Assert.Equal((TabScrollButtons.Part)expected, TabScrollButtons.GetPartAt(new Point(x, y), size, horizontal));
    }

    [Fact]
    public void Render_PaintsTheArrowsInTheTabControlsColors()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.TabBackColor = Color.FromArgb(51, 52, 53);
            tabs.BorderColor = Color.FromArgb(61, 62, 63);
            Application.DoEvents();

            RECT rect;
            GetWindowRect(GetButtons(tabs).Handle, out rect);
            Point topLeft = host.PointToClient(new Point(rect.Left, rect.Top));

            using Bitmap bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
            host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.ClientSize));

            // Inside each half, clear of the arrow glyph and the outline.
            Color first = bitmap.GetPixel(topLeft.X + 3, topLeft.Y + 3);
            Color second = bitmap.GetPixel(topLeft.X + (rect.Right - rect.Left) - 4, topLeft.Y + 3);
            Color outline = bitmap.GetPixel(topLeft.X, topLeft.Y + 8);

            Assert.Equal(Color.FromArgb(51, 52, 53), Color.FromArgb(first.R, first.G, first.B));
            Assert.Equal(Color.FromArgb(51, 52, 53), Color.FromArgb(second.R, second.G, second.B));
            Assert.Equal(Color.FromArgb(61, 62, 63), Color.FromArgb(outline.R, outline.G, outline.B));
        });
    }

    [Fact]
    public void ArrowsSitFlushWithThePageBoxAndTheRowOfTabs_OnTop()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

            Rectangle arrows = GetButtons(tabs).GetBounds();

            // Right edge on the box's outer edge, bottom on its border line.
            Assert.Equal(area.Right, arrows.Right);
            Assert.Equal(area.Top + 1, arrows.Bottom);
            Assert.Equal(tabs.GetTabRect(0).Top, arrows.Top);
        });
    }

    [Fact]
    public void ArrowsSitFlushWithThePageBoxAndTheRowOfTabs_OnBottom()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs, TabAlignment.Bottom);
            Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

            Rectangle arrows = GetButtons(tabs).GetBounds();

            Assert.Equal(area.Right, arrows.Right);
            Assert.Equal(area.Bottom - 1, arrows.Top);
        });
    }

    [Fact]
    public void ArrowsStayInPlace_WhenTheControlIsResized()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);

            host.ClientSize = new Size(420, 120);
            Application.DoEvents();
            Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");
            Rectangle arrows = GetButtons(tabs).GetBounds();

            Assert.Equal(area.Right, arrows.Right);
        });
    }

    [Fact]
    public void ScrolledOutTabs_AreNotDrawnBeforeTheStripOrBehindTheArrows()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            host.BackColor = Color.FromArgb(71, 72, 73);
            ScrollTo(tabs, 2);
            Rectangle arrows = GetButtons(tabs).GetBounds();

            using Bitmap bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
            host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.ClientSize));

            Color background = Color.FromArgb(71, 72, 73);
            int y = tabs.GetTabRect(0).Top + 5;

            // Left of the strip's first pixel column, nothing of a hidden
            // tab may show; nor may anything right of the arrows.
            Assert.Equal(background, Color.FromArgb(bitmap.GetPixel(0, y).ToArgb() | unchecked((int)0xFF000000)));
            Assert.Equal(background, Color.FromArgb(bitmap.GetPixel(1, y).ToArgb() | unchecked((int)0xFF000000)));
            Assert.Equal(background, Color.FromArgb(bitmap.GetPixel(arrows.Right + 1, y).ToArgb() | unchecked((int)0xFF000000)));
        });
    }
}
