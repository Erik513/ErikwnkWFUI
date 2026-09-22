using ErikwnkWFUI.Styles;
using WfuiListBoxControl = ErikwnkWFUI.Controls.ListBoxControl;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// ListBoxControl is a thin wrapper around ListBox (an optional header bar
/// plus a set of proxied members - see its own class remarks) - these tests
/// only cover that the proxying itself is correct and that CreatePrimary
/// reaches the same colors ListBoxConstructionTests locks in on the plain
/// ListBox; the underlying selection/reorder logic itself is ListBox's own
/// concern, already covered there.
/// </summary>
public class ListBoxControlTests
{
    [Fact]
    public void Items_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();

        control.Items.Add("A");
        control.Items.Add("B");

        Assert.Equal(2, control.InnerListBox.Items.Count);
        Assert.Equal("A", control.InnerListBox.Items[0]);
    }

    [Fact]
    public void SelectedIndex_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();
        control.Items.Add("A");
        control.Items.Add("B");

        control.SelectedIndex = 1;

        Assert.Equal(1, control.InnerListBox.SelectedIndex);
        Assert.Equal("B", control.SelectedItem);
    }

    [Fact]
    public void ClearSelected_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();
        control.Items.Add("A");
        control.SelectedIndex = 0;

        control.ClearSelected();

        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void MoveItem_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");

        control.MoveItem(0, 2);

        Assert.Equal("B", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
        Assert.Equal("A", control.Items[2]);
    }

    [Fact]
    public void SelectedIndexChanged_FiresWhenInnerListBoxSelectionChanges()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();
        control.Items.Add("A");
        bool raised = false;
        control.SelectedIndexChanged += (sender, e) => raised = true;

        control.SelectedIndex = 0;

        Assert.True(raised);
    }

    [Fact]
    public void ItemsReordered_FiresWhenInnerListBoxReorders()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");
        bool raised = false;
        control.ItemsReordered += (sender, e) => raised = true;

        control.MoveItem(0, 2);

        Assert.True(raised);
    }

    [Fact]
    public void SelectedBackColor_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl
        {
            SelectedBackColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.SelectedBackColor);
    }

    [Fact]
    public void DragIndicatorColor_ProxiesToInnerListBox()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl
        {
            DragIndicatorColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.DragIndicatorColor);
    }

    [Fact]
    public void Title_RoundTrips()
    {
        using WfuiListBoxControl control = new WfuiListBoxControl
        {
            Title = "Sample list"
        };

        Assert.Equal("Sample list", control.Title);
    }

    [Fact]
    public void CreatePrimary_SetsSelectedBackColorToAccent()
    {
        using WfuiListBoxControl control = UIStyles.ListBoxControls.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsDragIndicatorColorToAccent()
    {
        using WfuiListBoxControl control = UIStyles.ListBoxControls.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.DragIndicatorColor);
    }

    [Fact]
    public void CreateStandard_KeepsColorsGray()
    {
        using WfuiListBoxControl control = UIStyles.ListBoxControls.CreateStandard();

        Assert.Equal(UIColors.BorderLight, control.SelectedBackColor);
        Assert.Equal(UIColors.BorderLight, control.DragIndicatorColor);
    }
}
