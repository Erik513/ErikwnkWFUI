using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.PropertyTableHost;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>What a row handle can do (hide, disable), the width of the label column and the tooltip of a label that is cut.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableRowTests : IDisposable
{
    private const string LongText = "A very long name of a setting that needs more than the label column has room for in two lines";

    private readonly PropertyTableHost _host = new PropertyTableHost(new Size(1200, 400));

    public void Dispose()
    {
        _host.Dispose();
    }

    [Fact]
    public void AHiddenRow_TakesNoRoom_TheRowsBelowMoveUp_AndShowingItBringsItBack()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        Button first = new Button { Text = "a" };
        Button second = new Button { Text = "b" };
        PropertyRow row = table.AddRow("First", first);
        table.AddRow("Second", second);
        Relayout(table);
        int top = LabelOf(table, "Second").Top;
        int height = table.Height;

        row.Visible = false;
        Relayout(table);

        Assert.False(row.Visible);
        Assert.False(first.Visible);
        Assert.False(LabelOf(table, "First").Visible);
        Assert.True(LabelOf(table, "Second").Top < top);
        Assert.True(table.Height < height);

        row.Visible = true;
        Relayout(table);

        Assert.True(first.Visible);
        Assert.Equal(top, LabelOf(table, "Second").Top);
        Assert.Equal(height, table.Height);
    }

    [Fact]
    public void ADisabledRow_LocksTheEditors_ButTheyKeepTheirOwnEnabled()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        Button free = new Button();
        Button locked = new Button { Enabled = false };
        PropertyRow row = table.AddRow("Row", free, locked);
        Relayout(table);

        row.Enabled = false;

        Assert.False(row.Enabled);
        Assert.False(free.Enabled);
        Assert.False(LabelOf(table, "Row").Enabled);

        row.Enabled = true;

        Assert.True(row.Enabled);
        Assert.True(free.Enabled);
        Assert.False(locked.Enabled);
    }

    [Fact]
    public void ASectionCanBeHiddenToo_AndAHandleOfClearedRowsDoesNothing()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        PropertyRow section = table.AddSection("Section");
        PropertyRow row = table.AddRow("Row", new Button());
        Relayout(table);

        section.Visible = false;
        Assert.False(LabelOf(table, "Section").Visible);

        table.ClearRows();

        Assert.Null(Record.Exception(() => row.Visible = false));
        Assert.Null(Record.Exception(() => row.Enabled = false));
        Assert.Null(Record.Exception(() => section.Visible = true));
    }

    [Fact]
    public void TheLabelWidth_IsScaledToTheScreen_AndFollowedByTheRowsThatAreThere()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        table.AddRow("Row", new Button());
        Relayout(table);

        Assert.Equal(130, table.LabelWidth);
        Assert.Equal(Dpi.Scale(table, 130), LabelOf(table, "Row").Width);

        table.LabelWidth = 220;
        Relayout(table);

        Assert.Equal(Dpi.Scale(table, 220), LabelOf(table, "Row").Width);
        Assert.Throws<ArgumentOutOfRangeException>(() => table.LabelWidth = 0);
    }

    [Fact]
    public void ALabelIsCutOnlyWhenItsTextDoesNotFitTheRow_AndAWiderColumnHoldsIt()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        table.AddRow("Short", new Button());
        table.AddRow(LongText, new Button());
        Relayout(table);

        Assert.False(PropertyTable.IsCut((ScrollingLabel)LabelOf(table, "Short")));
        Assert.True(PropertyTable.IsCut((ScrollingLabel)LabelOf(table, LongText)));

        table.LabelWidth = 900;
        Relayout(table);

        Assert.False(PropertyTable.IsCut((ScrollingLabel)LabelOf(table, LongText)));
    }

    [Fact]
    public void ALongLabel_WrapsOntoMoreLines_AndIsNotCutWhileTheLinesFitTheRow()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        string twoLines = "Name of the folder for the files";
        table.AddRow(twoLines, new Button());
        Relayout(table);
        ScrollingLabel label = (ScrollingLabel)LabelOf(table, twoLines);

        int oneLine = TextRenderer.MeasureText("A", label.Font).Height;
        int needed = TextRenderer.MeasureText(twoLines, label.Font, new Size(label.ClientSize.Width - label.Padding.Horizontal, int.MaxValue), label.GetTextFormatFlags()).Height;

        Assert.False(label.AutoEllipsis);
        Assert.True(needed >= 2 * oneLine - 2, "the text did not wrap");
        Assert.False(PropertyTable.IsCut(label));
    }

    [Fact]
    public void ThePointerOverALabel_DoesNotBreakWhenTheLabelIsCutOrNot()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        table.AddRow("Short", new Button());
        table.AddRow(LongText, new Button());
        Relayout(table);

        foreach (string text in new[] { "Short", LongText })
            Assert.Null(Record.Exception(() => LabelOf(table, text).InvokePrivate("OnMouseEnter", EventArgs.Empty)));
    }

    [Fact]
    public void TheTextOfARow_CanBeReadAndChanged_AndFollowsASourceThatIsAskedForAgain()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        PropertyRow row = table.AddRow("First", new Button());
        Assert.Equal("First", row.Text);

        row.Text = "Other";
        Assert.Equal("Other", row.Text);
        row.Text = null!;
        Assert.Equal("", row.Text);

        string current = "one";
        int assigned = 0;
        row.TextSource = () => current;
        Assert.Equal("one", row.Text);

        current = "two";
        row.InvokePrivate("RefreshText");
        Assert.Equal("two", row.Text);
        Assert.NotNull(row.TextSource);

        row.TextSource = null!;
        current = "three";
        row.InvokePrivate("RefreshText");
        Assert.Equal("two", row.Text);
        Assert.Equal(0, assigned);
    }

    [Fact]
    public void ARowWithNoControlsAtAll_HasNoTextAndIsEnabled()
    {
        PropertyTable table = _host.Show(new PropertyTable());
        PropertyRow section = table.AddSection("Section");

        Assert.Equal("Section", section.Text);
        Assert.True(section.Enabled);
    }

    [Fact]
    public void TheDesigner_DoesNotSeeTheColorSet_AndKnowsTheLabelWidth()
    {
        using PropertyTable table = new PropertyTable();
        System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(table);

        Assert.False(properties["Colors"]!.IsBrowsable);
        Assert.False(properties["LabelWidth"]!.ShouldSerializeValue(table));
        properties["LabelWidth"]!.SetValue(table, 200);
        Assert.True(properties["LabelWidth"]!.ShouldSerializeValue(table));
    }
}
