using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// Locks down CreateStandard's colors defaulting to a neutral gray (not the
/// live accent) and CreatePrimary explicitly overriding them back to the
/// accent - the exact split this control was missing before CreatePrimary
/// existed at all (see UIListBoxFactory/UIListBoxControlFactory).
/// [Collection] (see AccentColorTestCollection) - these read UIColors.Primary,
/// which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
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

    // The native ItemHeight setter itself only rejects a negative value,
    // not zero - but EnsureItemVisible divides ClientSize.Height by this
    // during a drag, which a zero height would turn into a
    // DivideByZeroException the moment a user actually dragged an item.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ItemHeightCustom_ClampsToAtLeastOne(int requestedHeight)
    {
        using WfuiListBox listBox = new WfuiListBox
        {
            ItemHeightCustom = requestedHeight
        };

        Assert.Equal(1, listBox.ItemHeightCustom);
    }
}
