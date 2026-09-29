using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed TabControl keeps the standard control's behavior and only
/// replaces the drawing, so these cover the theme defaults, the page
/// theming, the behavior that must stay standard, and - by drawing into a
/// bitmap and reading pixels back - that the colors actually end up where
/// they are meant to. [Collection] (see AccentColorTestCollection): the
/// defaults read UIColors, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlTests
{
    private static WfuiTabControl CreateTabs(TabAlignment alignment = TabAlignment.Top, int pageCount = 3)
    {
        WfuiTabControl tabs = new WfuiTabControl
        {
            Size = new Size(300, 260),
            Alignment = alignment
        };

        for (int i = 0; i < pageCount; i++)
        {
            tabs.TabPages.Add("Tab " + i);
        }

        _ = tabs.Handle;
        return tabs;
    }

    private static Bitmap Render(WfuiTabControl tabs)
    {
        Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));
        return bitmap;
    }

    private static Point Center(Rectangle rectangle)
    {
        return new Point(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2);
    }

    private static Color Pixel(Bitmap bitmap, int x, int y)
    {
        Color color = bitmap.GetPixel(x, y);
        return Color.FromArgb(color.R, color.G, color.B);
    }

    [Fact]
    public void Defaults_FollowTheCurrentTheme()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Equal(Color.Transparent, tabs.HeaderBackColor);
        Assert.Equal(UIColors.BackgroundDark, tabs.TabBackColor);
        Assert.Equal(UIColors.BackgroundLight, tabs.HoverTabBackColor);
        Assert.Equal(UIColors.BackgroundMedium, tabs.SelectedTabBackColor);
        Assert.Equal(UIColors.BackgroundMedium, tabs.PageBackColor);
        Assert.Equal(UIColors.TextSecondary, tabs.TabForeColor);
        Assert.Equal(UIColors.TextPrimary, tabs.SelectedTabForeColor);
        Assert.Equal(UIColors.TextDisabled, tabs.DisabledTabForeColor);
        Assert.Equal(UIColors.BorderMedium, tabs.BorderColor);
        Assert.Equal(UIFonts.Normal, tabs.Font);
    }

    [Fact]
    public void CreateStandard_KeepsTheAccentBarNeutral()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreateStandard();

        Assert.Equal(UIColors.BorderLight, tabs.AccentColor);
    }

    [Fact]
    public void CreatePrimary_ColorsTheAccentBarInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreatePrimary();

        Assert.Equal(UIColors.Primary, tabs.AccentColor);
    }

    [Fact]
    public void ColorSetter_OverridesTheThemeDefault()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        tabs.AccentColor = Color.Red;

        Assert.Equal(Color.Red, tabs.AccentColor);
    }

    // ---- pages ----

    [Fact]
    public void AddedPage_GetsThePageBackColor_InsteadOfTheVisualStyleTexture()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        TabPage page = new TabPage("A");

        tabs.TabPages.Add(page);

        Assert.False(page.UseVisualStyleBackColor);
        Assert.Equal(tabs.PageBackColor, page.BackColor);
    }

    [Fact]
    public void PageBackColor_Set_UpdatesPagesThatAlreadyExist_AndLaterOnes()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("A");

        tabs.PageBackColor = Color.Green;
        tabs.TabPages.Add("B");

        Assert.Equal(Color.Green, tabs.TabPages[0].BackColor);
        Assert.Equal(Color.Green, tabs.TabPages[1].BackColor);
    }

    [Fact]
    public void PageContent_InheritsTheThemedTextColor()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        TabPage page = new TabPage("A");
        Label label = new Label();
        page.Controls.Add(label);

        tabs.TabPages.Add(page);

        Assert.Equal(UIColors.TextPrimary, label.ForeColor);
    }

    // ---- standard behavior stays standard ----

    [Fact]
    public void SelectedIndexChanged_FiresLikeTheStandardControl()
    {
        using WfuiTabControl tabs = CreateTabs();
        int fired = 0;
        tabs.SelectedIndexChanged += (s, e) => fired++;

        tabs.SelectedIndex = 2;

        Assert.Equal(1, fired);
        Assert.Same(tabs.TabPages[2], tabs.SelectedTab);
    }

    [Fact]
    public void TabPages_BehaveLikeTheStandardCollection()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 2);

        tabs.TabPages.RemoveAt(0);

        Assert.Equal(1, tabs.TabCount);
        Assert.Equal("Tab 1", tabs.TabPages[0].Text);
    }

    [Theory]
    [InlineData(TabAlignment.Top)]
    [InlineData(TabAlignment.Bottom)]
    [InlineData(TabAlignment.Left)]
    [InlineData(TabAlignment.Right)]
    public void EveryAlignment_KeepsThePagesInsideTheClientArea(TabAlignment alignment)
    {
        using WfuiTabControl tabs = CreateTabs(alignment);

        Assert.True(tabs.ClientRectangle.Contains(tabs.DisplayRectangle));
    }

    // ---- hot tracking (only when HotTrack is on, like the standard one) ----

    [Fact]
    public void MouseOverATab_IsIgnored_WhileHotTrackIsOff()
    {
        using WfuiTabControl tabs = CreateTabs();
        Point over = Center(tabs.GetTabRect(1));

        tabs.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, over.X, over.Y, 0));

        Assert.Equal(-1, tabs.GetPrivateField<int>("_hoveredTabIndex"));
    }

    [Fact]
    public void MouseOverATab_IsTracked_WhileHotTrackIsOn_AndClearedOnLeave()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.HotTrack = true;
        Point over = Center(tabs.GetTabRect(1));

        tabs.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, over.X, over.Y, 0));
        Assert.Equal(1, tabs.GetPrivateField<int>("_hoveredTabIndex"));

        tabs.InvokePrivate("OnMouseLeave", System.EventArgs.Empty);
        Assert.Equal(-1, tabs.GetPrivateField<int>("_hoveredTabIndex"));
    }

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
        tabs.AccentColor = Color.FromArgb(200, 100, 50);
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
}
