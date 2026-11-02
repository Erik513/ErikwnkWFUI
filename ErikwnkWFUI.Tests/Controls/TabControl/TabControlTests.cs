using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control: the theme defaults, the pages, the behavior that stays standard, hot tracking, Appearance, the factories.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlTests : TabControlTestBase
{
    [Fact]
    public void WhatAPaintHandlerDraws_LiesOnTopOfTheTabs()
    {
        using WfuiTabControl tabs = CreateTabs();
        Rectangle marker = new Rectangle(tabs.GetTabRect(0).Left + 4, tabs.GetTabRect(0).Top + 4, 6, 6);
        tabs.Paint += (sender, e) =>
        {
            using SolidBrush brush = new SolidBrush(Color.Magenta);
            e.Graphics.FillRectangle(brush, marker);
        };

        using Bitmap rendered = Render(tabs);

        Assert.Equal(Color.FromArgb(255, 0, 255), Pixel(rendered, marker.Left + 2, marker.Top + 2));
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

        Assert.Equal(UIColors.TextTertiary, tabs.SelectedTabIndicatorColor);
    }

    [Fact]
    public void CreateReadOnlyStandard_KeepsTheAccentBarNeutral()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreateReadOnlyStandard();

        Assert.Equal(UIColors.TextTertiary, tabs.SelectedTabIndicatorColor);
    }

    [Fact]
    public void CreatePrimary_ColorsTheAccentBarInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreatePrimary();

        Assert.Equal(UIColors.PrimaryLight, tabs.SelectedTabIndicatorColor);
    }

    [Fact]
    public void CreateReadOnlyPrimary_ColorsTheAccentBarInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreateReadOnlyPrimary();

        Assert.Equal(UIColors.PrimaryLight, tabs.SelectedTabIndicatorColor);
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

        tabs.SelectedTabIndicatorColor = Color.Red;

        Assert.Equal(Color.Red, tabs.SelectedTabIndicatorColor);
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

    // ---- hot tracking (only when HotTrack is on, like the standard one) ----

    [Fact]
    public void MouseOverATab_IsIgnored_WhileHotTrackIsOff()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.HotTrack = false;
        Point over = Center(tabs.GetTabRect(1));

        tabs.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, over.X, over.Y, 0));

        Assert.Equal(-1, tabs.GetPrivateField<int>("_hoveredTabIndex"));
    }

    // The real message path, on a control as it comes out of the box: a mouse
    // move over a tab lights it up, leaving clears it again.
    [Fact]
    public void ADefaultControl_HighlightsTheHoveredTab_FromRealMouseMessages()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.HoverTabBackColor = Color.FromArgb(21, 22, 23);
        Rectangle tab = tabs.GetTabRect(1);

        SendMessage(tabs.Handle, 0x0200, System.IntPtr.Zero, (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16)));

        Assert.True(tabs.HotTrack);
        Assert.Equal(1, tabs.GetPrivateField<int>("_hoveredTabIndex"));

        using (Bitmap hovered = Render(tabs))
        {
            Assert.Equal(Color.FromArgb(21, 22, 23), Pixel(hovered, tab.Left + 4, tab.Top + 4));
        }

        SendMessage(tabs.Handle, 0x02A3, System.IntPtr.Zero, System.IntPtr.Zero);

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

    // ---- Appearance: only Normal ----

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

    // ---- the read-only control has none of the editing API ----

    [Fact]
    public void TheReadOnlyControl_HasNoEditingProperty_TheEditableOneHasThemAll()
    {
        string[] editing =
        {
            "AllowUserToAddTabs", "AllowUserToCloseTabs", "AllowUserToRenameTabs", "SelectTabOnRightClick",
            "MaxTabNameLength", "ShowLockIcon", "ContextMenuSelectionColor", "IsRenamingTab"
        };

        string[] onTheReadOnlyOne = editing.Where(name => typeof(ErikwnkWFUI.Controls.ReadOnlyTabControl).GetProperty(name) != null).ToArray();
        string[] missingOnTheEditableOne = editing.Where(name => typeof(ErikwnkWFUI.Controls.TabControl).GetProperty(name) == null).ToArray();

        Assert.True(onTheReadOnlyOne.Length == 0, "On the read-only control: " + string.Join(", ", onTheReadOnlyOne));
        Assert.True(missingOnTheEditableOne.Length == 0, "Missing on the editable control: " + string.Join(", ", missingOnTheEditableOne));
    }

    // ---- the factories and UIStyles ----

    [Fact]
    public void UIStyles_HandsOutTheRightTypes()
    {
        using var standard = ErikwnkWFUI.UIStyles.TabControls.CreateStandard();
        using var readOnlyStandard = ErikwnkWFUI.UIStyles.TabControls.CreateReadOnlyStandard();
        using var primary = ErikwnkWFUI.UIStyles.TabControls.CreatePrimary();
        using var readOnlyPrimary = ErikwnkWFUI.UIStyles.TabControls.CreateReadOnlyPrimary();

        Assert.IsType<ErikwnkWFUI.Controls.TabControl>(standard);
        Assert.IsType<ErikwnkWFUI.Controls.ReadOnlyTabControl>(readOnlyStandard);
        Assert.IsType<ErikwnkWFUI.Controls.TabControl>(primary);
        Assert.IsType<ErikwnkWFUI.Controls.ReadOnlyTabControl>(readOnlyPrimary);
    }

    [Fact]
    public void ThePrimaryVariants_ColorTheIndicatorInTheAccent_TheStandardOnesNeutral()
    {
        using var standard = ErikwnkWFUI.UIStyles.TabControls.CreateStandard();
        using var readOnlyStandard = ErikwnkWFUI.UIStyles.TabControls.CreateReadOnlyStandard();
        using var primary = ErikwnkWFUI.UIStyles.TabControls.CreatePrimary();
        using var readOnlyPrimary = ErikwnkWFUI.UIStyles.TabControls.CreateReadOnlyPrimary();

        Assert.Equal(UIColors.TextTertiary, standard.SelectedTabIndicatorColor);
        Assert.Equal(UIColors.TextTertiary, readOnlyStandard.SelectedTabIndicatorColor);
        Assert.Equal(UIColors.PrimaryLight, primary.SelectedTabIndicatorColor);
        Assert.Equal(UIColors.PrimaryLight, readOnlyPrimary.SelectedTabIndicatorColor);
    }
}
