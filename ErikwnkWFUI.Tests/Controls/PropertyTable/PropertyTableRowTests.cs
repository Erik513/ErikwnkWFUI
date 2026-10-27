using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>What a row handle can do (hide, disable), the width of the label column and the tooltip of a label that is cut.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableRowTests : IDisposable
{
    private const string LongText = "A very long name of a setting that needs more than the label column has room for in two lines";

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private PropertyTable Show(PropertyTable table)
    {
        Form form = OffscreenForm.Create(new Size(1200, 400), UIColors.BackgroundMedium);
        table.Dock = DockStyle.Top;
        form.Controls.Add(table);
        form.Show();
        form.PerformLayout();
        Application.DoEvents();
        _forms.Add(form);

        return table;
    }

    private static void Relayout(PropertyTable table)
    {
        table.Parent!.PerformLayout();
        Application.DoEvents();
    }

    private static Label LabelOf(PropertyTable table, string text)
    {
        return AllLabels(table).First(l => l.Text == text);
    }

    private static IEnumerable<Label> AllLabels(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Label label)
                yield return label;

            foreach (Label inner in AllLabels(child))
                yield return inner;
        }
    }

    [Fact]
    public void AHiddenRow_TakesNoRoom_TheRowsBelowMoveUp_AndShowingItBringsItBack()
    {
        PropertyTable table = Show(new PropertyTable());
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
        PropertyTable table = Show(new PropertyTable());
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
        PropertyTable table = Show(new PropertyTable());
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
        PropertyTable table = Show(new PropertyTable());
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
        PropertyTable table = Show(new PropertyTable());
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
    public void ThePointerOverALabel_DoesNotBreakWhenTheLabelIsCutOrNot()
    {
        PropertyTable table = Show(new PropertyTable());
        table.AddRow("Short", new Button());
        table.AddRow(LongText, new Button());
        Relayout(table);

        foreach (string text in new[] { "Short", LongText })
            Assert.Null(Record.Exception(() => LabelOf(table, text).InvokePrivate("OnMouseEnter", EventArgs.Empty)));
    }
}
