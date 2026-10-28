using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>What can be read and seen in the table: the texts of the labels and the headers, and the frame of the cells, in both themes.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableColorTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private PropertyTable Show()
    {
        PropertyTable table = new PropertyTable();
        table.AddSection("Section");
        table.AddRow("Row", new Button());
        Form form = OffscreenForm.Create(new Size(500, 200), UIColors.BackgroundMedium);
        table.Dock = DockStyle.Top;
        form.Controls.Add(table);
        form.Show();
        form.PerformLayout();
        Application.DoEvents();
        _forms.Add(form);

        return table;
    }

    private static IEnumerable<Button> AllButtons(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button)
                yield return button;

            foreach (Button inner in AllButtons(child))
                yield return inner;
        }
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

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTextsAndTheFrame_CanBeSeen_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            PropertyTable table = Show();
            Label header = AllLabels(table).First(l => l.Text == "Section");
            ScrollingLabel label = (ScrollingLabel)AllLabels(table).First(l => l.Text == "Row");

            Check(shortfalls, "section title", header.ForeColor, header.BackColor, 4.5);
            Check(shortfalls, "row label", label.ForeColor, label.BackColor, 4.5);
            label.Enabled = false;
            Check(shortfalls, "disabled row label", label.GetTextColor(), label.BackColor, 3.0);

            // The frame of a cell, as painted, is the quiet line of the library (1.5:1) against the table - the cell fill itself hardly
            // differs from the table, so the frame is what shows the cell.
            Control cell = AllButtons(table).First().Parent!.Parent!;
            Rectangle area = table.RectangleToClient(cell.RectangleToScreen(cell.ClientRectangle));

            using Bitmap picture = TestPictures.Draw(table);
            Check(shortfalls, "frame of a cell", picture.GetPixel(area.Left + area.Width / 2, area.Top), table.BackColor, 1.5);
            Check(shortfalls, "frame of a cell, left", picture.GetPixel(area.Left, area.Top + area.Height / 2), table.BackColor, 1.5);
            Check(shortfalls, "frame of a cell, bottom", picture.GetPixel(area.Left + area.Width / 2, area.Bottom - 1), table.BackColor, 1.5);
        });
    }
}
