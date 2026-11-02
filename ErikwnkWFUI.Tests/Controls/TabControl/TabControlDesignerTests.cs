using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>What the designer writes of the tab controls (only what was set, never the colors of the theme), and the tab that is hovered after the control was disabled or hidden.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlDesignerTests : TabControlTestBase
{
    private static readonly string[] Colors =
    {
        "HeaderBackColor", "TabBackColor", "HoverTabBackColor", "SelectedTabBackColor", "TabForeColor", "SelectedTabForeColor",
        "DisabledTabForeColor", "SelectedTabIndicatorColor", "BorderColor", "PageBackColor"
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheDesigner_WritesAColorOnlyWhereItWasSet_AndResetGivesTheThemeBack(bool editable)
    {
        using WfuiTabControl tabs = editable ? new ErikwnkWFUI.Controls.TabControl() : new WfuiTabControl();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(tabs);

        foreach (string name in Colors)
        {
            PropertyDescriptor property = properties[name]!;

            Assert.False(property.ShouldSerializeValue(tabs), name + " writes the color of the theme");

            property.SetValue(tabs, Color.Magenta);
            Assert.True(property.ShouldSerializeValue(tabs), name);

            property.ResetValue(tabs);
            Assert.False(property.ShouldSerializeValue(tabs), name);
            Assert.NotEqual(Color.Magenta, (Color)property.GetValue(tabs)!);
        }
    }

    [Fact]
    public void TheMenuColor_FollowsTheSameRule()
    {
        using ErikwnkWFUI.Controls.TabControl tabs = new ErikwnkWFUI.Controls.TabControl();
        PropertyDescriptor property = TypeDescriptor.GetProperties(tabs)["ContextMenuSelectionColor"]!;

        Assert.False(property.ShouldSerializeValue(tabs));
        property.SetValue(tabs, Color.Magenta);
        Assert.True(property.ShouldSerializeValue(tabs));
        property.ResetValue(tabs);
        Assert.False(property.ShouldSerializeValue(tabs));
    }

    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheTabControlsHave_AndTheStateIsHidden()
    {
        using ErikwnkWFUI.Controls.TabControl tabs = new ErikwnkWFUI.Controls.TabControl();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(tabs);

        Assert.False(properties["IsRenamingTab"]!.IsBrowsable);

        // (Font, ForeColor, HotTrack and the menu are given by the library on purpose.)
        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute
                && property.Name is not ("Font" or "ForeColor" or "HotTrack" or "ContextMenuStrip"))
                Assert.True(Equals(attribute.Value, property.GetValue(tabs)), property.Name + " is not its default");
        }
    }

    [Fact]
    public void ATabThatWasUnderTheMouse_WhileTheControlWasDisabledOrHidden_IsLookedAtAgain()
    {
        using WfuiTabControl tabs = CreateTabs();
        using Form host = OffscreenForm.Show(tabs, new Size(320, 280), Color.Black, new Point(5, 5));
        Rectangle second = tabs.GetTabRect(1);
        Func<int> hovered = () => tabs.GetPrivateField<int>("_hoveredTabIndex");

        tabs.CursorPosition = () => tabs.PointToScreen(new Point(second.Left + 5, second.Top + 5));
        tabs.Enabled = false;
        Assert.Equal(-1, hovered());
        tabs.Enabled = true;
        Assert.Equal(1, hovered());

        // (The mouse went away while the control was hidden.)
        tabs.Visible = false;
        tabs.CursorPosition = () => new Point(-9000, -9000);
        tabs.Visible = true;
        Assert.Equal(-1, hovered());
    }
}
