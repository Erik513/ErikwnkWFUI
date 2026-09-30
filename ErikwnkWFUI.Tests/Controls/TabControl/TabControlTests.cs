using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

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
    public void CreateReadOnlyStandard_KeepsTheAccentBarNeutral()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreateReadOnlyStandard();

        Assert.Equal(UIColors.BorderLight, tabs.AccentColor);
    }

    [Fact]
    public void CreatePrimary_ColorsTheAccentBarInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreatePrimary();

        Assert.Equal(UIColors.Primary, tabs.AccentColor);
    }

    [Fact]
    public void CreateReadOnlyPrimary_ColorsTheAccentBarInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreateReadOnlyPrimary();

        Assert.Equal(UIColors.Primary, tabs.AccentColor);
    }

    [Fact]
    public void TheReadOnlyVariants_HaveNoEditingMenu_TheEditableOnesDo()
    {
        using WfuiTabControl readOnlyStandard = UITabControlFactory.CreateReadOnlyStandard();
        using WfuiTabControl readOnlyPrimary = UITabControlFactory.CreateReadOnlyPrimary();
        using WfuiTabControl standard = UITabControlFactory.CreateStandard();
        using WfuiTabControl primary = UITabControlFactory.CreatePrimary();

        Assert.Null(readOnlyStandard.ContextMenuStrip);
        Assert.Null(readOnlyPrimary.ContextMenuStrip);
        Assert.NotNull(standard.ContextMenuStrip);
        Assert.NotNull(primary.ContextMenuStrip);
    }

    // ---- right-click on a tab (the read-only base raises it too) ----

    [Fact]
    public void TabRightClick_CarriesThePageItsIndexAndTheLocation()
    {
        using WfuiTabControl tabs = CreateTabs();
        System.Collections.Generic.List<ErikwnkWFUI.Controls.TabRightClickEventArgs> clicks =
            new System.Collections.Generic.List<ErikwnkWFUI.Controls.TabRightClickEventArgs>();
        tabs.TabRightClick += (s, e) => clicks.Add(e);
        Rectangle rect = tabs.GetTabRect(1);

        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, rect.Left + 8, rect.Top + 8, 0));

        Assert.Single(clicks);
        Assert.Equal(1, clicks[0].TabIndex);
        Assert.Same(tabs.TabPages[1], clicks[0].TabPage);
        Assert.Equal(new Point(rect.Left + 8, rect.Top + 8), clicks[0].Location);
    }

    [Fact]
    public void TabRightClick_IsNotRaised_ForALeftClickOrAClickOffTheTabs()
    {
        using WfuiTabControl tabs = CreateTabs();
        int raised = 0;
        tabs.TabRightClick += (s, e) => raised++;
        Rectangle rect = tabs.GetTabRect(1);

        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, rect.Left + 8, rect.Top + 8, 0));
        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 290, rect.Top + 8, 0));

        Assert.Equal(0, raised);
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

    [Fact]
    public void Render_DrawsTheTabImageFromTheImageList()
    {
        using WfuiTabControl tabs = CreateTabs();
        using Bitmap icon = new Bitmap(16, 16);
        using (Graphics graphics = Graphics.FromImage(icon))
        {
            graphics.Clear(Color.FromArgb(255, 128, 0));
        }

        using ImageList images = new ImageList();
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

    // ---- layouts the standard control offers ----

    [Fact]
    public void Multiline_StacksTheTabRows_AboveThePageArea()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 12);
        tabs.Width = 200;
        tabs.Multiline = true;

        Assert.True(tabs.RowCount > 1);

        int lowestTabEdge = 0;

        for (int i = 0; i < tabs.TabCount; i++)
        {
            lowestTabEdge = System.Math.Max(lowestTabEdge, tabs.GetTabRect(i).Bottom);
        }

        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");
        Assert.True(area.Top <= lowestTabEdge);
        Assert.True(tabs.DisplayRectangle.Top >= lowestTabEdge);
    }

    [Fact]
    public void FillToRight_StretchesEveryTabRowAcrossTheControl_AndStillRenders()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 12);
        tabs.Width = 200;
        tabs.Multiline = true;
        tabs.SizeMode = TabSizeMode.FillToRight;

        using Bitmap bitmap = Render(tabs);

        System.Collections.Generic.Dictionary<int, int> rightEdgeByRow = new System.Collections.Generic.Dictionary<int, int>();

        for (int i = 0; i < tabs.TabCount; i++)
        {
            Rectangle rect = tabs.GetTabRect(i);
            rightEdgeByRow[rect.Top] = System.Math.Max(rightEdgeByRow.ContainsKey(rect.Top) ? rightEdgeByRow[rect.Top] : 0, rect.Right);
        }

        Assert.All(rightEdgeByRow.Values, right => Assert.True(right >= tabs.Width - 6));
    }

    [Fact]
    public void RightToLeftLayout_StillCreatesAndRenders()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.RightToLeft = RightToLeft.Yes;
        tabs.RightToLeftLayout = true;

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(3, tabs.TabCount);
    }

    [Fact]
    public void ToolTips_KeepTheStandardDefault_AndThePagesToolTipText()
    {
        using WfuiTabControl tabs = CreateTabs();

        tabs.TabPages[0].ToolTipText = "Hint";

        Assert.True(tabs.ShowToolTips == false);
        Assert.Equal("Hint", tabs.TabPages[0].ToolTipText);
    }

    // ---- Appearance: only Normal ----

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(System.IntPtr hWnd, int index);

    private const int GWL_STYLE = -16;
    private const int TCS_BUTTONS = 0x0100;

    [Fact]
    public void Appearance_IsAlwaysNormal()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Equal(TabAppearance.Normal, tabs.Appearance);
    }

    [Theory]
    [InlineData(TabAppearance.Buttons)]
    [InlineData(TabAppearance.FlatButtons)]
    public void Appearance_SettingAButtonStyle_Throws(TabAppearance appearance)
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Throws<System.NotSupportedException>(() => tabs.Appearance = appearance);
        Assert.Equal(TabAppearance.Normal, tabs.Appearance);
    }

    [Fact]
    public void Appearance_SettingNormal_IsAccepted()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        tabs.Appearance = TabAppearance.Normal;

        Assert.Equal(TabAppearance.Normal, tabs.Appearance);
    }

    // Through a reference typed as the standard control the property can't
    // be intercepted - the window itself must still never become button tabs.
    [Theory]
    [InlineData(TabAppearance.Buttons)]
    [InlineData(TabAppearance.FlatButtons)]
    public void Appearance_SetThroughTheBaseType_NeverMakesTheWindowButtonTabs(TabAppearance appearance)
    {
        using WfuiTabControl tabs = CreateTabs();

        ((System.Windows.Forms.TabControl)tabs).Appearance = appearance;
        _ = tabs.Handle;

        Assert.Equal(0, GetWindowLong(tabs.Handle, GWL_STYLE) & TCS_BUTTONS);
    }

    // ---- a page's own BackColor stays changeable ----

    [Fact]
    public void PageWithItsOwnBackColor_KeepsItWhenAdded()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        TabPage page = new TabPage("A") { BackColor = Color.Red };

        tabs.TabPages.Add(page);

        Assert.Equal(Color.Red, page.BackColor);
    }

    [Fact]
    public void PageBackColorSetAfterAdding_Stays()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("A");

        tabs.TabPages[0].BackColor = Color.Red;

        Assert.Equal(Color.Red, tabs.TabPages[0].BackColor);
    }

    [Fact]
    public void ChangingPageBackColor_LeavesPagesWithTheirOwnColorAlone()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("themed");
        tabs.TabPages.Add(new TabPage("custom") { BackColor = Color.Red });

        tabs.PageBackColor = Color.Green;

        Assert.Equal(Color.Green, tabs.TabPages[0].BackColor);
        Assert.Equal(Color.Red, tabs.TabPages[1].BackColor);
    }

    [Fact]
    public void PageWithACustomColorSetLater_StaysWhenPageBackColorChanges()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("A");
        tabs.TabPages[0].BackColor = Color.Red;

        tabs.PageBackColor = Color.Green;

        Assert.Equal(Color.Red, tabs.TabPages[0].BackColor);
    }

    // ---- DrawMode = OwnerDrawFixed: DrawItem still works ----

    private sealed class DrawItemRecorder
    {
        public System.Collections.Generic.List<DrawItemEventArgs> Calls { get; } = new System.Collections.Generic.List<DrawItemEventArgs>();
    }

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
}
