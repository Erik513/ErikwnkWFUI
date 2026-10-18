using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control: what ends up on the pixels (selected, hovered, the page border, images, DrawItem, vertical tabs).
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlRenderTests : TabControlTestBase
{
    // ---- what actually gets drawn ----

    [Fact]
    public void Render_PaintsSelectedAndUnselectedTabsInTheirOwnColors()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedTabBackColor = Color.FromArgb(1, 2, 3);
        tabs.TabBackColor = Color.FromArgb(4, 5, 6);
        Rectangle selected = tabs.GetTabRect(0);
        Rectangle other = tabs.GetTabRect(1);

        using Bitmap bitmap = Render(tabs);

        // Just inside a tab's corner, clear of the text and the border.
        Assert.Equal(Color.FromArgb(1, 2, 3), Pixel(bitmap, selected.Left + 4, selected.Bottom - 4));
        Assert.Equal(Color.FromArgb(4, 5, 6), Pixel(bitmap, other.Left + 4, other.Top + 4));
    }

    [Fact]
    public void Render_PaintsTheHoveredTabInTheHoverColor()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.HoverTabBackColor = Color.FromArgb(7, 8, 9);
        tabs.SetPrivateField("_hoveredTabIndex", 1);
        Rectangle hovered = tabs.GetTabRect(1);

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(Color.FromArgb(7, 8, 9), Pixel(bitmap, hovered.Left + 4, hovered.Top + 4));
    }

    [Fact]
    public void Render_PaintsTheHeaderStripAndThePageBorder()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.HeaderBackColor = Color.FromArgb(11, 12, 13);
        tabs.BorderColor = Color.FromArgb(21, 22, 23);
        tabs.PageBackColor = Color.FromArgb(31, 32, 33);
        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

        using Bitmap bitmap = Render(tabs);

        // Right of the last tab, where only the strip background shows.
        Assert.Equal(Color.FromArgb(11, 12, 13), Pixel(bitmap, tabs.Width - 5, 5));
        Assert.Equal(Color.FromArgb(21, 22, 23), Pixel(bitmap, area.Right - 10, area.Top));
        Assert.Equal(Color.FromArgb(31, 32, 33), Pixel(bitmap, area.Left + 2, area.Top + area.Height / 2));
    }

    [Fact]
    public void Render_TransparentHeader_ShowsTheParentBackgroundBehindTheTabs()
    {
        using Form host = new Form { ClientSize = new Size(300, 260), BackColor = Color.FromArgb(41, 42, 43) };
        using WfuiTabControl tabs = CreateTabs();
        host.Controls.Add(tabs);
        tabs.Dock = DockStyle.Fill;
        _ = host.Handle;

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(Color.FromArgb(41, 42, 43), Pixel(bitmap, tabs.Width - 5, 5));
    }

    [Fact]
    public void Render_LetsTheSelectedTabFlowIntoThePage()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedTabBackColor = Color.FromArgb(1, 2, 3);
        tabs.BorderColor = Color.FromArgb(21, 22, 23);
        Rectangle selected = tabs.GetTabRect(0);
        Rectangle other = tabs.GetTabRect(1);
        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

        using Bitmap bitmap = Render(tabs);

        // Under the selected tab the page border is interrupted...
        Assert.Equal(Color.FromArgb(1, 2, 3), Pixel(bitmap, selected.Left + 4, area.Top));
        // ...under an unselected one it runs straight through.
        Assert.Equal(Color.FromArgb(21, 22, 23), Pixel(bitmap, other.Left + 4, area.Top));
    }

    [Fact]
    public void PageBox_LinesUpWithTheOuterEdgeOfTheFirstTab()
    {
        using WfuiTabControl tabs = CreateTabs();

        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

        Assert.Equal(tabs.GetTabRect(0).Left, area.Left);
    }

    [Fact]
    public void Render_SelectedTabOutline_EndsOnThePageBorder_WithoutStubsBelowIt()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.BorderColor = Color.FromArgb(21, 22, 23);
        tabs.PageBackColor = Color.FromArgb(31, 32, 33);
        tabs.SelectedTabBackColor = Color.FromArgb(31, 32, 33);
        Rectangle selected = tabs.GetTabRect(0);
        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

        using Bitmap bitmap = Render(tabs);

        // The tab's right outline is still there on the border line...
        Assert.Equal(Color.FromArgb(21, 22, 23), Pixel(bitmap, selected.Right - 1, area.Top));
        // ...but doesn't continue into the page below it.
        Assert.Equal(Color.FromArgb(31, 32, 33), Pixel(bitmap, selected.Right - 1, area.Top + 1));
    }

    [Theory]
    [InlineData(TabAlignment.Top)]
    [InlineData(TabAlignment.Bottom)]
    [InlineData(TabAlignment.Left)]
    [InlineData(TabAlignment.Right)]
    public void Render_PutsTheAccentBarOnTheOuterEdgeOfTheSelectedTab(TabAlignment alignment)
    {
        using WfuiTabControl tabs = CreateTabs(alignment);
        tabs.SelectedTabIndicatorColor = Color.FromArgb(200, 100, 50);
        Rectangle selected = tabs.GetTabRect(0);

        Point onBar;
        Point beside;

        switch (alignment)
        {
            case TabAlignment.Bottom:
                onBar = new Point(selected.Left + 6, selected.Bottom - 1);
                beside = new Point(selected.Left + 6, selected.Bottom - 4);
                break;

            case TabAlignment.Left:
                onBar = new Point(selected.Left, selected.Top + 6);
                beside = new Point(selected.Left + 4, selected.Top + 6);
                break;

            case TabAlignment.Right:
                onBar = new Point(selected.Right - 1, selected.Top + 6);
                beside = new Point(selected.Right - 4, selected.Top + 6);
                break;

            default:
                onBar = new Point(selected.Left + 6, selected.Top);
                beside = new Point(selected.Left + 6, selected.Top + 4);
                break;
        }

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(Color.FromArgb(200, 100, 50), Pixel(bitmap, onBar.X, onBar.Y));
        Assert.NotEqual(Color.FromArgb(200, 100, 50), Pixel(bitmap, beside.X, beside.Y));
    }

    [Fact]
    public void Render_DrawsTheTabImageFromTheImageList()
    {
        using WfuiTabControl tabs = CreateTabs();
        using Bitmap icon = new Bitmap(16, 16);
        using (Graphics graphics = Graphics.FromImage(icon))
        {
            graphics.Clear(Color.FromArgb(255, 128, 0));
        }

        using ImageList images = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
        images.Images.Add(icon);
        tabs.ImageList = images;
        tabs.TabPages[1].ImageIndex = 0;
        Rectangle tab = tabs.GetTabRect(1);

        using Bitmap bitmap = Render(tabs);

        bool found = false;

        for (int x = tab.Left; x < tab.Right && !found; x++)
        {
            found = Pixel(bitmap, x, tab.Top + tab.Height / 2) == Color.FromArgb(255, 128, 0);
        }

        Assert.True(found);
    }

    // ---- DrawMode = OwnerDrawFixed: DrawItem still works ----

    [Fact]
    public void DrawItem_IsNotRaised_InTheDefaultMode()
    {
        using WfuiTabControl tabs = CreateTabs();
        int raised = 0;
        tabs.DrawItem += (s, e) => raised++;

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void DrawItem_IsRaisedOncePerTab_WithOwnerDrawFixed()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        System.Collections.Generic.List<int> indexes = new System.Collections.Generic.List<int>();
        tabs.DrawItem += (s, e) => indexes.Add(e.Index);

        using Bitmap bitmap = Render(tabs);

        indexes.Sort();
        Assert.Equal(new[] { 0, 1, 2 }, indexes);
    }

    [Fact]
    public void DrawItem_CarriesTheTabRectangleAndItsState()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.TabPages[2].Enabled = false;
        System.Collections.Generic.Dictionary<int, DrawItemEventArgs> byIndex = new System.Collections.Generic.Dictionary<int, DrawItemEventArgs>();
        tabs.DrawItem += (s, e) => byIndex[e.Index] = new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index, e.State, e.ForeColor, e.BackColor);

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(tabs.GetTabRect(1), byIndex[1].Bounds);
        Assert.True((byIndex[0].State & DrawItemState.Selected) != 0);
        Assert.True((byIndex[1].State & DrawItemState.Selected) == 0);
        Assert.True((byIndex[2].State & DrawItemState.Disabled) != 0);
        // (DrawItemEventArgs itself swaps in the system highlight colors for
        // the selected state, same as with the native control.)
        Assert.Equal(tabs.TabBackColor, byIndex[1].BackColor);
        Assert.Equal(tabs.DisabledTabForeColor, byIndex[2].ForeColor);
    }

    [Fact]
    public void DrawItem_LetsTheHandlerDrawTheTab()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.DrawItem += (s, e) =>
        {
            using SolidBrush brush = new SolidBrush(Color.FromArgb(200, 10, 20));
            e.Graphics.FillRectangle(brush, e.Bounds);
        };
        Rectangle tab = tabs.GetTabRect(1);

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(Color.FromArgb(200, 10, 20), Pixel(bitmap, tab.Left + 4, tab.Top + 4));
    }

    // ---- drawing never falls over ----

    [Theory]
    [InlineData(TabAlignment.Top, 0)]
    [InlineData(TabAlignment.Bottom, 0)]
    [InlineData(TabAlignment.Left, 0)]
    [InlineData(TabAlignment.Right, 0)]
    [InlineData(TabAlignment.Top, 1)]
    [InlineData(TabAlignment.Left, 1)]
    public void DrawingAnEmptyOrSingleTabControl_DoesNotThrow_InAnyAlignment(TabAlignment alignment, int pageCount)
    {
        using WfuiTabControl tabs = CreateTabs(alignment, pageCount);

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(tabs.Width, bitmap.Width);
    }

    [Fact]
    public void DrawingATinyControl_DoesNotThrow()
    {
        foreach (Size size in new[] { new Size(1, 1), new Size(10, 10), new Size(40, 20) })
        {
            using WfuiTabControl tabs = CreateTabs();
            tabs.Size = size;

            using Bitmap bitmap = new Bitmap(System.Math.Max(1, tabs.Width), System.Math.Max(1, tabs.Height));
            tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

            Assert.True(bitmap.Width >= 1);
        }
    }

    [Fact]
    public void DrawingBeforeTheHandleExists_IsNotNeeded_ButTheBoundsStillWork()
    {
        using WfuiTabControl tabs = new WfuiTabControl { Size = new Size(200, 100) };
        tabs.TabPages.Add("a");

        Assert.Equal(1, tabs.TabCount);
        Assert.Equal(new Size(200, 100), tabs.Size);
    }

    // ---- vertical tabs ----

    [Theory]
    [InlineData(TabAlignment.Left)]
    [InlineData(TabAlignment.Right)]
    public void OnAVerticalStrip_TheNameIsDrawnAlongTheTab(TabAlignment alignment)
    {
        using WfuiTabControl tabs = CreateTabs(alignment);
        tabs.TabBackColor = Color.Black;
        tabs.SelectedTabBackColor = Color.Black;
        tabs.TabForeColor = Color.White;
        tabs.SelectedTabForeColor = Color.White;
        Rectangle tab = tabs.GetTabRect(1);

        using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));

        // Rotated text fills the tab's length, not just its middle: pixels
        // in the upper and in the lower half of the tab both.
        int upper = CountColored(bitmap, new Rectangle(tab.Left, tab.Top, tab.Width, tab.Height / 2), reddish: false);
        int lower = CountColored(bitmap, new Rectangle(tab.Left, tab.Top + tab.Height / 2, tab.Width, tab.Height / 2), reddish: false);
        Assert.True(upper > 3);
        Assert.True(lower > 3);
    }

    [Theory]
    [InlineData(TabAlignment.Left)]
    [InlineData(TabAlignment.Right)]
    public void OnAVerticalStrip_ATabImageIsDrawn(TabAlignment alignment)
    {
        using WfuiTabControl tabs = CreateTabs(alignment);
        using Bitmap icon = new Bitmap(16, 16);
        using (Graphics graphics = Graphics.FromImage(icon))
        {
            graphics.Clear(Color.FromArgb(255, 128, 0));
        }

        using ImageList images = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
        images.Images.Add(icon);
        tabs.ImageList = images;
        tabs.TabPages[1].ImageIndex = 0;
        Rectangle tab = tabs.GetTabRect(1);

        using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));

        bool found = false;

        for (int x = tab.Left; x < tab.Right && !found; x++)
        {
            for (int y = tab.Top; y < tab.Bottom && !found; y++)
            {
                found = Pixel(bitmap, x, y) == Color.FromArgb(255, 128, 0);
            }
        }

        Assert.True(found);
    }
}
