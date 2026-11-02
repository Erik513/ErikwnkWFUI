using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// GetBackColor/GetTextColor - the priority order OnDrawItem picks a row's
/// colors from (selected, then hovered, then disabled, then plain
/// even/odd). Invoked directly via reflection: these two methods are pure
/// functions of their own parameters plus this control's own color
/// properties, so calling them exercises the exact same logic OnDrawItem
/// would (the disabled list, which also depends on Enabled, and a few
/// real paints are in ListBoxStandardBehaviorTests and
/// ListBoxEdgeCaseTests).
/// </summary>
public class ListBoxRenderingTests
{
    [Fact]
    public void GetBackColor_SelectedTakesPriorityOverEverythingElse()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.SelectedBackColor = Color.Red;

        Color result = listBox.InvokePrivate<Color>("GetBackColor", 0, true, true, true);

        // The selection is a tint of its color over the row (a color: 120 of 255 of it).
        Color expected = ColorContrastHelper.Composite(Color.FromArgb(120, Color.Red), listBox.ItemBackColor);
        Assert.Equal(expected.ToArgb(), result.ToArgb());
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
    public void GetTextColor_OfTheSelection_IsTheTextOfTheItem_UnlessOneWasSet()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox();
        listBox.SelectedBackColor = Color.White;

        Assert.Equal(listBox.ItemForeColor, listBox.InvokePrivate<Color>("GetTextColor", true, true));

        listBox.SelectedForeColor = Color.Black;
        Assert.Equal(Color.Black, listBox.InvokePrivate<Color>("GetTextColor", true, true));
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
