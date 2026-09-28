using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// CopyConfirmation/GetClipboardContent - mirrors ListView's own copy
/// confirmation (CopyConfirmationDisplay is the shared logic both use).
/// GetCopyConfirmationMessage is invoked directly via reflection since it's
/// a pure function; GetClipboardContent itself is called directly (a
/// public override, no reflection needed) rather than through an actual
/// Ctrl+C, which isn't practical to drive headlessly. None of these tests
/// add the grid to a real Form, so CopyConfirmation's default (Toast) never
/// actually tries to show one - FindForm() returns null, same safety net
/// ListView's own copy tests already rely on - this also means these
/// tests double as confirmation that the new toast wiring itself doesn't
/// throw.
/// </summary>
public class ReadOnlyDataGridViewCopyConfirmationTests
{
    [Fact]
    public void ActionConfirmation_DefaultsToToast()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.Equal(CopyConfirmationStyle.Toast, grid.ActionConfirmation);
    }

    [Fact]
    public void GetCopyConfirmationMessage_SingleCell_ReturnsSingularText()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        string message = grid.InvokePrivate<string>("GetCopyConfirmationMessage", 1)!;

        Assert.Equal("Cell copied", message);
    }

    [Fact]
    public void GetCopyConfirmationMessage_MultipleCells_ReturnsFormattedPluralText()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        string message = grid.InvokePrivate<string>("GetCopyConfirmationMessage", 3)!;

        Assert.Equal("3 cells copied", message);
    }

    [Fact]
    public void GetClipboardContent_OnReadOnlyDataGridView_ExcludesTheEnumerationColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int enumIndex = grid.Columns["__enumeration"]!.Index;
        int nameIndex = grid.Columns["Name"]!.Index;

        grid.ClearSelection();
        grid.Rows[0].Cells[enumIndex].Selected = true;
        grid.Rows[0].Cells[nameIndex].Selected = true;

        DataObject clipboardContent = grid.GetClipboardContent();
        string text = (string)clipboardContent.GetData(DataFormats.Text)!;

        Assert.Equal("Apple", text.Trim());
    }

    [Fact]
    public void GetClipboardContent_WithoutShowEnumeration_ReturnsNormalContent()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        int nameIndex = grid.Columns["Name"]!.Index;

        grid.ClearSelection();
        grid.Rows[0].Cells[nameIndex].Selected = true;

        DataObject clipboardContent = grid.GetClipboardContent();
        string text = (string)clipboardContent.GetData(DataFormats.Text)!;

        Assert.Equal("Apple", text.Trim());
    }

    // Confirms the composed override chain (DataGridView's own delete-
    // column exclusion wraps ReadOnlyDataGridView's enumeration-column
    // exclusion, which wraps the native copy) still produces the exact
    // same result as before this feature existed.
    [Fact]
    public void GetClipboardContent_OnDataGridView_StillExcludesBothSystemColumns()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowEnumeration = true;
        grid.ShowDeleteRowColumn = true;
        int enumIndex = grid.Columns["__enumeration"]!.Index;
        int deleteIndex = grid.Columns["__deleteRow"]!.Index;
        int nameIndex = grid.Columns["Name"]!.Index;

        grid.ClearSelection();
        grid.Rows[0].Cells[enumIndex].Selected = true;
        grid.Rows[0].Cells[deleteIndex].Selected = true;
        grid.Rows[0].Cells[nameIndex].Selected = true;

        DataObject clipboardContent = grid.GetClipboardContent();
        string text = (string)clipboardContent.GetData(DataFormats.Text)!;

        Assert.Equal("Apple", text.Trim());
    }
}
