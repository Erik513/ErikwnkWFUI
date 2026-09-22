using ErikwnkWFUI.Styles;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// Locks down CreateStandard's colors defaulting to a neutral gray (not the
/// live accent) and CreatePrimary explicitly overriding them back to the
/// accent - the exact split this control was missing before CreatePrimary
/// existed at all (see UIListBoxFactory/UIListBoxControlFactory).
/// </summary>
public class ListBoxConstructionTests
{
    [Fact]
    public void AllowReorder_DefaultsToTrue()
    {
        using WfuiListBox listBox = new WfuiListBox();

        Assert.True(listBox.AllowReorder);
    }

    [Fact]
    public void SelectedBackColor_DefaultsToBorderLight()
    {
        using WfuiListBox listBox = new WfuiListBox();

        Assert.Equal(UIColors.BorderLight, listBox.SelectedBackColor);
    }

    [Fact]
    public void DragIndicatorColor_DefaultsToBorderLight()
    {
        using WfuiListBox listBox = new WfuiListBox();

        Assert.Equal(UIColors.BorderLight, listBox.DragIndicatorColor);
    }

    [Fact]
    public void SelectedBackColor_CanBeSetExplicitly()
    {
        using WfuiListBox listBox = new WfuiListBox
        {
            SelectedBackColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, listBox.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsSelectedBackColorToAccent()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, listBox.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsDragIndicatorColorToAccent()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, listBox.DragIndicatorColor);
    }

    [Fact]
    public void CreateStandard_KeepsColorsGray()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();

        Assert.Equal(UIColors.BorderLight, listBox.SelectedBackColor);
        Assert.Equal(UIColors.BorderLight, listBox.DragIndicatorColor);
    }
}
