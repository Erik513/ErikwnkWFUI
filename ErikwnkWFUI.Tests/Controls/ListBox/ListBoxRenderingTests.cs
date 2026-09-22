using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// GetBackColor/GetTextColor - the priority order OnDrawItem picks a row's
/// colors from (selected, then hovered, then disabled, then plain
/// even/odd). Invoked directly via reflection rather than through a real
/// paint, which would need actual Graphics/rendering infrastructure this
/// doesn't have; these two methods are pure functions of their own
/// parameters plus this control's own color properties, so calling them
/// directly exercises the exact same logic OnDrawItem would.
/// </summary>
public class ListBoxRenderingTests
{
    [Fact]
    public void GetBackColor_SelectedTakesPriorityOverEverythingElse()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.SelectedBackColor = Color.Red;

        Color result = listBox.InvokePrivate<Color>("GetBackColor", 0, true, true, true);

        Assert.Equal(Color.Red, result);
    }

    [Fact]
    public void GetBackColor_HoveredTakesPriorityOverDisabled()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.HoverBackColor = Color.Blue;

        Color result = listBox.InvokePrivate<Color>("GetBackColor", 0, false, true, true);

        Assert.Equal(Color.Blue, result);
    }

    [Fact]
    public void GetBackColor_ReturnsDisabledColor_WhenNeitherSelectedNorHovered()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.DisabledBackColor = Color.Green;

        Color result = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, true);

        Assert.Equal(Color.Green, result);
    }

    [Fact]
    public void GetBackColor_AlternatesEvenAndOddRows_WhenPlain()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.ItemBackColor = Color.FromArgb(10, 10, 10);

        Color evenRow = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, false);
        Color oddRow = listBox.InvokePrivate<Color>("GetBackColor", 1, false, false, false);

        Assert.Equal(Color.FromArgb(10, 10, 10), evenRow);
        Assert.NotEqual(evenRow, oddRow);
    }

    [Fact]
    public void GetTextColor_ReturnsContrastingColorAgainstSelectedBackground()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.SelectedBackColor = Color.White;

        Color result = listBox.InvokePrivate<Color>("GetTextColor", true, true);

        Assert.Equal(UIColors.GetContrastingForeColor(Color.White), result);
    }

    [Fact]
    public void GetTextColor_ReturnsDisabledColor_WhenDisabledButNotSelected()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.DisabledForeColor = Color.Gray;

        Color result = listBox.InvokePrivate<Color>("GetTextColor", false, true);

        Assert.Equal(Color.Gray, result);
    }

    [Fact]
    public void GetTextColor_ReturnsItemForeColor_WhenPlain()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.ItemForeColor = Color.Yellow;

        Color result = listBox.InvokePrivate<Color>("GetTextColor", false, false);

        Assert.Equal(Color.Yellow, result);
    }
}
