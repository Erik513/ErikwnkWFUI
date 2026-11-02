using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// What the tests of the property table use to show one: docked at the top of an off-screen form, laid out and with its messages handled. The
/// forms are disposed with the host. The static helpers look into a table (its labels, its buttons) and lay it out again after a change.
/// </summary>
internal sealed class PropertyTableHost : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Size _size;

    public PropertyTableHost(Size size)
    {
        _size = size;
    }

    /// <summary>Shows <paramref name="table"/> on a form of the size of the host and returns it.</summary>
    public PropertyTable Show(PropertyTable table)
    {
        Form form = OffscreenForm.Create(_size, UIColors.BackgroundMedium);
        table.Dock = DockStyle.Top;
        form.Controls.Add(table);
        form.Show();
        Relayout(table);
        _forms.Add(form);

        return table;
    }

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        _forms.Clear();
    }

    /// <summary>Lays the form of the table out again and handles the messages (after rows were added, hidden or changed).</summary>
    public static void Relayout(PropertyTable table)
    {
        table.Parent!.PerformLayout();
        Application.DoEvents();
    }

    /// <summary>Every label below <paramref name="parent"/>: the row labels and the section headers.</summary>
    public static IEnumerable<Label> AllLabels(Control parent)
    {
        return AllOf<Label>(parent);
    }

    public static IEnumerable<Button> AllButtons(Control parent)
    {
        return AllOf<Button>(parent);
    }

    /// <summary>The label with this text (the first one).</summary>
    public static Label LabelOf(PropertyTable table, string text)
    {
        return AllLabels(table).First(l => l.Text == text);
    }

    /// <summary>How many controls of a kind below <paramref name="parent"/> match.</summary>
    public static int CountOf<T>(Control parent, Func<T, bool> match) where T : Control
    {
        return AllOf<T>(parent).Count(match);
    }

    private static IEnumerable<T> AllOf<T>(Control parent) where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is T typed)
                yield return typed;

            foreach (T inner in AllOf<T>(child))
                yield return inner;
        }
    }
}
