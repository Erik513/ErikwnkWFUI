using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>A click somewhere else deselects, as in the ListView: the watch of the whole application runs only while the grid has a window.</summary>
public class DataGridViewOutsideClickTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private static bool Watching(WfuiReadOnlyDataGridView grid)
    {
        object? filter = grid.GetPrivateField<object>("_outsideClickDeselectFilter");

        return filter != null && filter.GetPrivateField<bool>("_started");
    }

    private (Form Form, T Grid) Show<T>(T grid) where T : WfuiReadOnlyDataGridView
    {
        grid.Dock = DockStyle.Top;
        grid.Height = 120;

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 400)
        };
        form.Controls.Add(grid);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return (form, grid);
    }

    private (Form Form, WfuiDataGridView Grid) ShowItems()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 1), ("Pear", 2));

        return Show(GridTestHelpers.CreateGrid(items));
    }

    [Fact]
    public void AGridWithoutAWindow_WatchesNothing()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.False(Watching(grid));
    }

    [Fact]
    public void TheWatch_RunsWhileTheGridHasAWindow_AndEndsWithIt()
    {
        (Form form, WfuiDataGridView grid) = ShowItems();
        Assert.True(Watching(grid));

        form.Controls.Remove(grid);
        grid.Dispose();

        Assert.False(Watching(grid));
    }

    [Fact]
    public void TheWatch_EndsWhenTheWindowIsDestroyed_AndStartsAgainWithTheNext_AlsoInTheReadOnlyGrid()
    {
        (Form form, WfuiDataGridView grid) = ShowItems();

        grid.InvokePrivate("DestroyHandle");
        Assert.False(Watching(grid));

        _ = grid.Handle;
        Assert.True(Watching(grid));

        (Form otherForm, WfuiReadOnlyDataGridView readOnly) = Show(GridTestHelpers.CreateReadOnlyGrid(GridTestHelpers.CreateItems(("Apple", 1))));
        Assert.True(Watching(readOnly));
    }

    [Fact]
    public void AClickOnABoxOrALabel_ClearsTheSelection_AClickOnACommand_OrInsideTheGrid_DoesNot()
    {
        (Form form, WfuiDataGridView grid) = ShowItems();
        Button button = new Button { Dock = DockStyle.Bottom };
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        ToolStrip toolBar = new ToolStrip { Dock = DockStyle.Bottom };
        MenuStrip menuBar = new MenuStrip { Dock = DockStyle.Bottom };
        TextBox editor = new TextBox();
        System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
        form.Controls.AddRange(new Control[] { button, panel, toolBar, menuBar });
        grid.Controls.Add(editor);
        _ = menu.Handle;
        _ = editor.Handle;
        grid.Rows[0].Cells[0].Selected = true;
        Application.DoEvents();

        bool Deselects(Control control) => grid.InvokePrivate<bool>("IsOutsideClickThatDeselects", control.Handle);

        Assert.True(Deselects(panel));
        Assert.True(Deselects(form));
        Assert.False(Deselects(button));
        Assert.False(Deselects(toolBar));
        Assert.False(Deselects(menuBar));
        Assert.False(Deselects(menu));
        Assert.False(Deselects(grid));

        // What belongs to the grid - the editor of a cell, its scroll bars - is inside.
        Assert.False(Deselects(editor));
        menu.Dispose();
    }

    [Fact]
    public void WithNothingSelected_OrWhileACellIsEdited_AClickElsewhereDoesNothing()
    {
        (Form form, WfuiDataGridView grid) = ShowItems();
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        form.Controls.Add(panel);
        Application.DoEvents();

        bool Deselects() => grid.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle);

        grid.ClearSelection();
        Assert.False(Deselects());

        grid.CurrentCell = grid.Rows[0].Cells[0];
        grid.Rows[0].Cells[0].Selected = true;
        Assert.True(Deselects());

        grid.BeginEdit(false);
        Assert.True(grid.IsCurrentCellInEditMode);
        Assert.False(Deselects());
        grid.CancelEdit();
    }

    [Fact]
    public void Deselecting_ClearsTheSelectionAndTheCurrentCell()
    {
        (Form form, WfuiDataGridView grid) = ShowItems();
        grid.CurrentCell = grid.Rows[1].Cells[0];
        grid.Rows[0].Cells[0].Selected = true;
        grid.Rows[1].Cells[0].Selected = true;
        Assert.Equal(2, grid.SelectedCells.Count);

        grid.InvokePrivate("DeselectAll");

        Assert.Empty(grid.SelectedCells);
        Assert.Null(grid.CurrentCell);
    }
}