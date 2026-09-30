using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

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

    [DllImport("user32.dll")]
    private static extern bool GetUpdateRect(System.IntPtr hWnd, out RECT rect, bool erase);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr insertAfter, int x, int y, int cx, int cy, int flags);

    private static bool NeedsRepaint(WfuiTabControl tabs)
    {
        RECT rect;
        return GetUpdateRect(tabs.Handle, out rect, false);
    }

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

    // The tabs are drawn up to wherever the arrows are - so if the arrows
    // turn up (or change) after the tabs were already painted, the tabs have
    // to be painted again, or their text stays visible past the arrows.
    [Fact]
    public void ArrowsBeingAttached_TriggersARepaintOfTheTabs()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.Update();
            Assert.False(NeedsRepaint(tabs));

            GetButtons(tabs).ReleaseHandle();
            GetButtons(tabs).AttachTo(tabs.Handle);

            Assert.True(NeedsRepaint(tabs));
        });
    }

    [Fact]
    public void ArrowsBeingHidden_TriggersARepaintOfTheTabs()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.Update();
            Assert.False(NeedsRepaint(tabs));

            // SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_HIDEWINDOW.
            SetWindowPos(GetButtons(tabs).Handle, System.IntPtr.Zero, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0004 | 0x0010 | 0x0080);

            Assert.True(NeedsRepaint(tabs));
        });
    }

    // A tab reaching under the arrows stays visible, cut off right where
    // the arrows begin - so it's clear there are more tabs - and nothing of
    // it may show on the arrows' far side.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ScrollingStrip_ShowsACutOffTabUpToTheArrows_AndNotBeyond(int position)
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            host.BackColor = Color.FromArgb(71, 72, 73);
            tabs.TabBackColor = Color.FromArgb(81, 82, 83);
            ScrollTo(tabs, position);
            Rectangle arrows = GetButtons(tabs).GetBounds();

            using Bitmap bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
            host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.ClientSize));

            int cutOffTabs = 0;

            for (int i = 0; i < tabs.TabCount; i++)
            {
                Rectangle tab = tabs.GetTabRect(i);

                if (i == tabs.SelectedIndex || tab.Left >= arrows.Left || tab.Right <= arrows.Left || tab.Left < 2)
                    continue;

                cutOffTabs++;

                // Its shape is there right up to the arrows...
                Color pixel = bitmap.GetPixel(arrows.Left - 3, tab.Top + 4);
                Assert.Equal(Color.FromArgb(81, 82, 83), Color.FromArgb(pixel.R, pixel.G, pixel.B));
            }

            Assert.True(cutOffTabs > 0);

            // ...and past the arrows, only the strip's own background.
            int y = tabs.GetTabRect(0).Top + 5;
            Color beyond = bitmap.GetPixel(arrows.Right + 1, y);
            Assert.Equal(Color.FromArgb(71, 72, 73), Color.FromArgb(beyond.R, beyond.G, beyond.B));
        });
    }

    private static Rectangle GetInvalidArea(WfuiTabControl tabs)
    {
        RECT rect;
        GetUpdateRect(tabs.Handle, out rect, false);
        return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    // Scrolling shifts every tab; the native control only repaints what it
    // uncovers, so anything short of a full repaint leaves old (cut-off)
    // tab pixels behind.
    [Fact]
    public void ScrollingTheStrip_RepaintsTheWholeControl()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.Update();

            // Sent directly, without ScrollTo's DoEvents - that would run
            // the pending repaint before it can be looked at.
            SendMessage(tabs.Handle, WM_HSCROLL, (System.IntPtr)(SB_THUMBPOSITION | (2 << 16)), GetButtons(tabs).Handle);
            Rectangle invalid = GetInvalidArea(tabs);

            Assert.Equal(tabs.ClientRectangle, invalid);
        });
    }

    [Fact]
    public void SelectingACutOffTab_RepaintsTheWholeControl()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.Update();

            tabs.SelectedIndex = tabs.TabCount - 1;
            Rectangle invalid = GetInvalidArea(tabs);

            Assert.Equal(tabs.ClientRectangle, invalid);
        });
    }

    // Text was seen slipping past the graphics clip on screen, into the
    // margin beyond the arrows - so the strip outside the visible part is
    // painted over regardless. Junk is planted there first, then the cover
    // has to wipe it.
    [Fact]
    public void CoveringTheStrip_WipesWhateverWasDrawnBeyondTheArrows()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            host.BackColor = Color.FromArgb(71, 72, 73);
            Rectangle arrows = GetButtons(tabs).GetBounds();

            using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
                tabs.InvokePrivate("CoverOutsideVisibleStrip", graphics);
            }

            int y = arrows.Top + 8;

            // Beyond the arrows and before the strip's first pixel column:
            // wiped. Inside the arrows' own area it is left alone.
            Assert.Equal(Color.FromArgb(71, 72, 73), Color.FromArgb(bitmap.GetPixel(arrows.Right, y).ToArgb() | unchecked((int)0xFF000000)));
            Assert.Equal(Color.FromArgb(71, 72, 73), Color.FromArgb(bitmap.GetPixel(0, y).ToArgb() | unchecked((int)0xFF000000)));
            Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(arrows.Left + 5, y).ToArgb());
        });
    }

    // ---- the states of the arrows ----

    private static Color CenterOfHalf(Bitmap bitmap, Rectangle arrows, bool second)
    {
        int x = arrows.Left + (second ? arrows.Width * 3 / 4 : arrows.Width / 4);
        Color pixel = bitmap.GetPixel(x, arrows.Top + arrows.Height / 2);
        return Color.FromArgb(pixel.R, pixel.G, pixel.B);
    }

    private static Color CornerOfSecondHalf(Bitmap bitmap, Rectangle arrows)
    {
        Color pixel = bitmap.GetPixel(arrows.Right - 4, arrows.Top + 3);
        return Color.FromArgb(pixel.R, pixel.G, pixel.B);
    }

    private static Bitmap RenderHost(Form host)
    {
        Bitmap bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height);
        host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.ClientSize));
        return bitmap;
    }

    [Fact]
    public void TheArrowThatCannotScrollFurther_IsDrawnInTheDisabledColor()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.TabForeColor = Color.FromArgb(240, 240, 240);
            tabs.DisabledTabForeColor = Color.FromArgb(240, 0, 0);
            tabs.TabBackColor = Color.Black;
            Application.DoEvents();
            Rectangle arrows = GetButtons(tabs).GetBounds();

            using Bitmap bitmap = RenderHost(host);

            // At the start of the strip: back is spent, forward still works.
            Color back = CenterOfHalf(bitmap, arrows, second: false);
            Color forward = CenterOfHalf(bitmap, arrows, second: true);
            Assert.True(back.R > 150 && back.G < 80);
            Assert.True(forward.R > 150 && forward.G > 150);
        });
    }

    [Fact]
    public void AHoveredArrow_IsDrawnInTheHoverColor_APressedOneInTheSelectedColor()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.TabBackColor = Color.FromArgb(10, 11, 12);
            tabs.HoverTabBackColor = Color.FromArgb(20, 21, 22);
            tabs.SelectedTabBackColor = Color.FromArgb(30, 31, 32);
            TabScrollButtons buttons = GetButtons(tabs);
            Application.DoEvents();
            Rectangle arrows = buttons.GetBounds();

            using (Bitmap normal = RenderHost(host))
            {
                Assert.Equal(Color.FromArgb(10, 11, 12), CornerOfSecondHalf(normal, arrows));
            }

            buttons.SetPrivateField("_hotPart", TabScrollButtons.Part.Second);
            using (Bitmap hot = RenderHost(host))
            {
                Assert.Equal(Color.FromArgb(20, 21, 22), CornerOfSecondHalf(hot, arrows));
            }

            buttons.SetPrivateField("_pressedPart", TabScrollButtons.Part.Second);
            using (Bitmap pressed = RenderHost(host))
            {
                Assert.Equal(Color.FromArgb(30, 31, 32), CornerOfSecondHalf(pressed, arrows));
            }
        });
    }

    [Fact]
    public void TheArrowsOutline_UsesTheBorderColor()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            tabs.BorderColor = Color.FromArgb(91, 92, 93);
            Application.DoEvents();
            Rectangle arrows = GetButtons(tabs).GetBounds();

            using Bitmap bitmap = RenderHost(host);

            Color top = bitmap.GetPixel(arrows.Left + 8, arrows.Top);
            Color divider = bitmap.GetPixel(arrows.Left + arrows.Width / 2, arrows.Top + 5);
            Assert.Equal(Color.FromArgb(91, 92, 93), Color.FromArgb(top.R, top.G, top.B));
            Assert.Equal(Color.FromArgb(91, 92, 93), Color.FromArgb(divider.R, divider.G, divider.B));
        });
    }

    [Fact]
    public void Arrows_ReleasedAndAttachedAgain_KeepWorking()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowOverflowingTabs(out WfuiTabControl tabs);
            TabScrollButtons buttons = GetButtons(tabs);

            buttons.ReleaseHandle();
            buttons.AttachTo(tabs.Handle);
            Application.DoEvents();

            Assert.NotEqual(System.IntPtr.Zero, buttons.Handle);
            Assert.False(buttons.GetBounds().IsEmpty);
        });
    }

    [Fact]
    public void TheBounds_AreEmpty_BeforeTheArrowsAreAttached()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.True(tabs.GetPrivateField<TabScrollButtons>("_scrollButtons")!.GetBounds().IsEmpty);
    }
}
