using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>How the table places the editors by their kind, the heights and widths of rows, empty and null arguments, and the editors that are put in again.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableEdgeCaseTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private PropertyTable Show(PropertyTable table)
    {
        Form form = OffscreenForm.Create(new Size(600, 500), UIColors.BackgroundMedium);
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

    [Fact]
    public void TheEditors_AreLeftAnchoredOrFilled_ByTheirKind()
    {
        PropertyTable table = Show(new PropertyTable());
        Button button = new Button();
        System.Windows.Forms.CheckBox check = new System.Windows.Forms.CheckBox();
        ToggleSwitch toggle = new ToggleSwitch();
        System.Windows.Forms.ComboBox combo = new System.Windows.Forms.ComboBox();
        System.Windows.Forms.TextBox text = new System.Windows.Forms.TextBox { Multiline = true };
        System.Windows.Forms.NumericUpDown number = new System.Windows.Forms.NumericUpDown();
        Control plain = new Control();

        table.AddRow("Row", button, check, toggle, combo, text, number, plain);

        foreach (Control left in new Control[] { button, check, toggle })
        {
            Assert.True(left.Dock == DockStyle.None, left.GetType().Name + " " + left.Dock);
            Assert.Equal(AnchorStyles.Left, left.Anchor);
        }

        Assert.Equal(DockStyle.Fill, combo.Dock);
        Assert.Equal(DockStyle.Fill, text.Dock);
        Assert.Equal(BorderStyle.None, text.BorderStyle);
        Assert.False(text.Multiline);
        Assert.Equal(DockStyle.Left, number.Dock);
        Assert.Equal(BorderStyle.None, number.BorderStyle);
        Assert.Equal(DockStyle.Fill, plain.Dock);
    }

    [Fact]
    public void ARowHasTheDefaultHeight_OrTheOneThatWasGiven()
    {
        PropertyTable table = Show(new PropertyTable());
        table.AddRow("Default", new Button());
        table.AddRow("Tall", 90, new Button());
        Relayout(table);

        Label standard = AllLabels(table).First(l => l.Text == "Default");
        Label tall = AllLabels(table).First(l => l.Text == "Tall");

        Assert.Equal(42, standard.Height);
        Assert.Equal(90, tall.Height);
    }

    [Fact]
    public void AnEditorHigherThanTheRow_MakesTheRowHigher_AndIsNotCutOff()
    {
        PropertyTable table = Show(new PropertyTable());
        System.Windows.Forms.Panel tall = new System.Windows.Forms.Panel { Height = 150 };
        System.Windows.Forms.Panel tallInFixedRow = new System.Windows.Forms.Panel { Height = 120 };
        table.AddRow("First", new Button());
        table.AddRow("Tall", tall);
        table.AddRow("Tall, row height given", 42, tallInFixedRow);
        table.AddRow("Last", new Button());
        Relayout(table);

        Assert.Equal(150, tall.Height);
        Assert.True(tall.Parent!.Parent!.Height >= 150, "the cell is lower than the editor");
        Assert.Equal(120, tallInFixedRow.Height);
        Assert.True(tallInFixedRow.Parent!.Parent!.Height >= 120);
        Assert.True(table.Height >= 42 + 156 + 126 + 42, "the rows were not made higher");
        Assert.True(AllLabels(table).First(l => l.Text == "Last").Top >= 42 + 156 + 126);
    }

    [Fact]
    public void ARowOfNormalEditors_KeepsTheDefaultHeight()
    {
        PropertyTable table = Show(new PropertyTable());
        table.AddRow("Row", new Button(), UIStyles.TextBoxes.CreateStandard("t"), new System.Windows.Forms.ComboBox());
        Relayout(table);

        Assert.Equal(42, AllLabels(table).First(l => l.Text == "Row").Height);
    }

    [Fact]
    public void TheColumnsOfARow_TakeTheirWidthsAsGiven()
    {
        PropertyTable table = Show(new PropertyTable());
        Button fixedWidth = new Button();
        Button rest = new Button();
        table.AddRow("Row", PropertyColumn.Absolute(fixedWidth, 150), PropertyColumn.Percent(rest, 100f));
        Relayout(table);

        // The cell of the first column is as wide as asked; the wrapper inside it is 2 px (cell margin) and 14 px (padding) narrower at most.
        Control cell = fixedWidth.Parent!.Parent!;

        Assert.Equal(150, cell.Width + 2);
        Assert.True(rest.Parent!.Parent!.Width > cell.Width);
    }

    [Fact]
    public void ARowWithoutEditors_OrWithNull_IsStillARow()
    {
        PropertyTable table = Show(new PropertyTable());

        PropertyRow none = table.AddRow("None", new Control[0]);
        PropertyRow nullArray = table.AddRow("Null", (Control[])null!);
        PropertyRow holes = table.AddRow("Holes", null!, new Button(), null!);
        PropertyRow columns = table.AddRow("Columns", (PropertyColumn[])null!);
        Relayout(table);

        Assert.NotNull(none);
        Assert.NotNull(nullArray);
        Assert.NotNull(holes);
        Assert.NotNull(columns);
        Assert.Equal(4, AllLabels(table).Count());
        Assert.Null(Record.Exception(() => none.Visible = false));
    }

    [Fact]
    public void NullTextsOfLabelsAndSections_AreEmptyTexts()
    {
        PropertyTable table = Show(new PropertyTable());

        table.AddSection(null!);
        table.AddRow(null!, new Button());
        Relayout(table);

        Assert.All(AllLabels(table), label => Assert.Equal("", label.Text));
        Assert.Equal(2, AllLabels(table).Count());
    }

    [Fact]
    public void TheEditorsOfClearedRows_CanBePutInAgain_WithTheirStateKept()
    {
        PropertyTable table = Show(new PropertyTable());
        System.Windows.Forms.TextBox box = new System.Windows.Forms.TextBox { Text = "typed" };
        table.AddRow("Old", box);
        Relayout(table);

        table.ClearRows();
        table.AddRow("New", box);
        Relayout(table);

        Assert.Equal("typed", box.Text);
        Assert.True(box.Visible);
        Assert.Contains(AllLabels(table), l => l.Text == "New");
        Assert.DoesNotContain(AllLabels(table), l => l.Text == "Old");
        Assert.True(box.Width > 0);
    }

    [Fact]
    public void ManyRebuilds_DoNotLeaveWrappersBehind()
    {
        PropertyTable table = Show(new PropertyTable());
        Button keep = new Button();

        for (int round = 0; round < 20; round++)
        {
            table.ClearRows();
            table.AddSection("Section");
            table.AddRow("Row", keep);
        }

        Relayout(table);

        Assert.Equal(2, AllLabels(table).Count());
        Assert.Equal(1, CountOf<System.Windows.Forms.TableLayoutPanel>(table, t => t.BackColor == UIColors.BackgroundLight));
    }

    [Fact]
    public void TheTable_FollowsAChangeOfTheTheme_InItsLabelsAndCells()
    {
        PropertyTable table = Show(new PropertyTable());
        table.AddSection("Section");
        table.AddRow("Row", new Button());
        Relayout(table);
        Label header = AllLabels(table).First(l => l.Text == "Section");
        Label label = AllLabels(table).First(l => l.Text == "Row");
        Color darkHeader = header.BackColor;
        Color darkLabel = label.BackColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.NotEqual(darkHeader.ToArgb(), header.BackColor.ToArgb());
        Assert.NotEqual(darkLabel.ToArgb(), label.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundDark.ToArgb(), header.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), label.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), table.BackColor.ToArgb());
    }

    [Fact]
    public void ADisposedTable_DoesNotKeepFollowingTheTheme()
    {
        PropertyTable table = new PropertyTable();
        table.AddSection("Section");
        table.AddRow("Row", new Button());

        table.Dispose();

        Assert.Null(Record.Exception(() => UIColors.ApplyTheme(UIThemes.Light)));
    }

    // How many controls of a kind below the parent match.
    private static int CountOf<T>(Control parent, Func<T, bool> match) where T : Control
    {
        int count = 0;

        foreach (Control child in parent.Controls)
        {
            if (child is T typed && match(typed))
                count++;

            count += CountOf(child, match);
        }

        return count;
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
}
