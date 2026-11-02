using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>The width of a column of a row of the property table: the three ways to make one, and the old name that still works.</summary>
public class PropertyColumnTests
{
    [Fact]
    public void TheThreeWaysToMakeAColumn_GiveTheControlAndTheWidth()
    {
        using Control control = new Control();

        PropertyColumn auto = PropertyColumn.Auto(control);
        PropertyColumn fixedWidth = PropertyColumn.Absolute(control, 120);
        PropertyColumn percent = PropertyColumn.Percent(control, 30f);

        Assert.Same(control, auto.Control);
        Assert.Equal(SizeType.Percent, auto.Style.SizeType);
        Assert.Equal(100f, auto.Style.Width);
        Assert.Equal(SizeType.Absolute, fixedWidth.Style.SizeType);
        Assert.Equal(120f, fixedWidth.Style.Width);
        Assert.Equal(SizeType.Percent, percent.Style.SizeType);
        Assert.Equal(30f, percent.Style.Width);
    }

    [Fact]
    public void AColumnMayHoldNoControl_AsAPlaceholder()
    {
        PropertyColumn empty = PropertyColumn.Percent(null!, 50f);

        Assert.Null(empty.Control);
    }

    // The old name stays until the projects that use it have been changed: its factory methods give the same columns, and a row takes them.
#pragma warning disable CS0618
    [Fact]
    public void TheOldName_UIColumn_StillMakesTheSameColumns_AndARowTakesThem()
    {
        using Control first = new Control();
        using Control second = new Control();
        using PropertyTable table = new PropertyTable();

        PropertyColumn percent = UIColumn.Percent(first, 40f);
        PropertyColumn fixedWidth = UIColumn.Absolute(second, 90);

        Assert.Equal(40f, percent.Style.Width);
        Assert.Equal(SizeType.Absolute, fixedWidth.Style.SizeType);
        Assert.Null(Record.Exception(() => table.AddRow("Label", UIColumn.Percent(first, 50f), UIColumn.Percent(second, 50f))));
    }
#pragma warning restore CS0618
}
