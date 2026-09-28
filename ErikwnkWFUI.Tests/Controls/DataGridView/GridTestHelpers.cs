using System.ComponentModel;
using System.Windows.Forms;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

internal static class GridTestHelpers
{
    public static WfuiDataGridView CreateGrid(BindingList<TestItem> dataSource)
    {
        WfuiDataGridView grid = new WfuiDataGridView
        {
            // A standalone control (no parent Form) has a null
            // BindingContext by default, so DataSource never actually gets
            // a CurrencyManager - Columns/Rows silently stay empty (no
            // exception, just nothing there) without this.
            BindingContext = new BindingContext(),
            DataSource = dataSource
        };

        // Forces the native window handle to be created - without it, Rows/
        // selection don't reliably reflect a bound DataSource the way they
        // do once a grid is actually shown.
        _ = grid.Handle;

        return grid;
    }

    public static WfuiReadOnlyDataGridView CreateReadOnlyGrid(BindingList<TestItem> dataSource)
    {
        WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = dataSource
        };
        _ = grid.Handle;
        return grid;
    }

    public static TestableDataGridView CreateTestableGrid(BindingList<TestItem> dataSource)
    {
        TestableDataGridView grid = new TestableDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = dataSource
        };
        _ = grid.Handle;
        return grid;
    }

    // CurrentCell is assigned FIRST, before any cell is selected below, not
    // after - a real bug (found and fixed the same way in DataGridView's
    // own SelectRow) that lived in this exact ordering: this control's
    // SelectionMode is CellSelect, where assigning CurrentCell to a DIFFERENT
    // cell than it currently is collapses SelectedCells down to just that
    // one cell as a side effect. Doing it last, as this used to, silently
    // undid whatever the loop below had just selected the moment
    // CurrentCell actually changed - which every one of these three helpers
    // happened to dodge only when the first row/cell passed in was already
    // whatever a freshly-created grid defaults its CurrentCell to
    // (Rows[0].Cells[0]).
    public static void SelectCells(WfuiDataGridView grid, params (int Row, int Column)[] cells)
    {
        grid.ClearSelection();

        if (cells.Length > 0)
        {
            grid.CurrentCell = grid.Rows[cells[0].Row].Cells[cells[0].Column];
        }

        foreach ((int row, int column) in cells)
        {
            grid.Rows[row].Cells[column].Selected = true;
        }
    }

    public static void SelectRow(WfuiDataGridView grid, int rowIndex)
    {
        grid.ClearSelection();
        grid.CurrentCell = grid.Rows[rowIndex].Cells[0];

        foreach (DataGridViewCell cell in grid.Rows[rowIndex].Cells)
        {
            cell.Selected = true;
        }
    }

    public static void SelectRows(WfuiDataGridView grid, params int[] rowIndexes)
    {
        grid.ClearSelection();

        if (rowIndexes.Length > 0)
        {
            grid.CurrentCell = grid.Rows[rowIndexes[0]].Cells[0];
        }

        foreach (int rowIndex in rowIndexes)
        {
            foreach (DataGridViewCell cell in grid.Rows[rowIndex].Cells)
            {
                cell.Selected = true;
            }
        }
    }

    public static BindingList<TestItem> CreateItems(params (string Name, int Value)[] items)
    {
        BindingList<TestItem> list = new BindingList<TestItem>();

        foreach ((string name, int value) in items)
        {
            list.Add(new TestItem(name, value));
        }

        return list;
    }
}
