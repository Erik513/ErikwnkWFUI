using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests;

/// <summary>
/// Locks down the two constructors' defaults - the editable/read-only split
/// silently dropped AllowUserToAddRows (never reset back to true on the
/// editable side) and AllowUserToDeleteRows (never reset to false on the
/// read-only side) once already; these exist so a future edit that
/// reintroduces either regresses a red test instead of just "feeling off"
/// the next time someone happens to try adding/deleting a row.
/// </summary>
public class DataGridViewConstructionTests
{
    [Fact]
    public void DataGridView_AllowsAddingRows()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        Assert.True(grid.AllowUserToAddRows);
    }

    [Fact]
    public void DataGridView_AllowsDeletingRows()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        Assert.True(grid.AllowUserToDeleteRows);
    }

    [Fact]
    public void DataGridView_IsNotReadOnly()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        Assert.False(grid.ReadOnly);
    }

    [Fact]
    public void DataGridView_HasAContextMenu()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        Assert.NotNull(grid.ContextMenuStrip);
    }

    [Fact]
    public void ReadOnlyDataGridView_DoesNotAllowAddingRows()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.False(grid.AllowUserToAddRows);
    }

    [Fact]
    public void ReadOnlyDataGridView_DoesNotAllowDeletingRows()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.False(grid.AllowUserToDeleteRows);
    }

    [Fact]
    public void ReadOnlyDataGridView_IsReadOnly()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.True(grid.ReadOnly);
    }
}
