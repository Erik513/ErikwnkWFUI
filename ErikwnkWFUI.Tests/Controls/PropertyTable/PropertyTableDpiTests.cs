using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>The sizes of the table (label column, rows, section headers) are given for 96 dpi and follow the screen.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableDpiTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private PropertyTable Show(PropertyTable table)
    {
        Form form = OffscreenForm.Create(new Size(900, 600), UIColors.BackgroundMedium);
        table.Dock = DockStyle.Top;
        form.Controls.Add(table);
        form.Show();
        Relayout(table);
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
    public void RowsSectionsAndTheLabelColumn_AreScaledToTheScreen()
    {
        PropertyTable table = Show(new PropertyTable { DpiOverride = 144 });
        table.AddSection("Section");
        table.AddRow("Row", new Button());
        table.AddRow("Higher", 60, new Button());
        Relayout(table);

        Assert.Equal(48, LabelOf(table, "Section").Height);
        Assert.Equal(63, LabelOf(table, "Row").Height);
        Assert.Equal(90, LabelOf(table, "Higher").Height);
        Assert.Equal(195, LabelOf(table, "Row").Width);
    }

    [Fact]
    public void ChangingTheScreen_ScalesTheRowsThatAreThere_AlsoAHiddenOne()
    {
        PropertyTable table = Show(new PropertyTable());
        table.AddSection("Section");
        PropertyRow row = table.AddRow("Row", new Button());
        PropertyRow hidden = table.AddRow("Hidden", new Button());
        hidden.Visible = false;
        Relayout(table);
        Assert.Equal(42, LabelOf(table, "Row").Height);

        table.DpiOverride = 192;
        Relayout(table);

        Assert.Equal(64, LabelOf(table, "Section").Height);
        Assert.Equal(84, LabelOf(table, "Row").Height);

        hidden.Visible = true;
        Relayout(table);

        Assert.Equal(84, LabelOf(table, "Hidden").Height);
        Assert.True(row.Visible);
    }

    [Fact]
    public void ATallEditor_StillFitsItsCell_OnAHigherScreen()
    {
        PropertyTable table = Show(new PropertyTable { DpiOverride = 144 });
        System.Windows.Forms.Panel tall = new System.Windows.Forms.Panel { Height = 150 };
        table.AddRow("Tall", tall);
        Relayout(table);

        Assert.Equal(150, tall.Height);
        Assert.True(tall.Parent!.Parent!.Height >= 150 + 4, "the cell is lower than the editor and its frame");
    }
}
