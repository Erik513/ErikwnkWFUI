using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// The read-only grid's own (small) right-click menu: Copy and Select all -
/// the only two things that still make sense on a display-only grid.
/// Opened through the menu's own protected OnOpening, same as
/// DataGridViewContextMenuEnableStateTests, since a real right-click isn't
/// practical to drive headlessly. Carries the shared Clipboard collection
/// (Copy touches the real clipboard) and the accent-color collection (the
/// CreateReadOnlyPrimary test reads UIColors.Primary).
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class ReadOnlyDataGridViewContextMenuTests
{
    private static WfuiContextMenuStrip Menu(WfuiReadOnlyDataGridView grid) => (WfuiContextMenuStrip)grid.ContextMenuStrip!;

    // Flat menu, same shape as ListView's own: Copy selection, Copy
    // selection with header, Copy all, Copy all with header, Select all.
    private static ToolStripMenuItem Copy(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[0];

    private static ToolStripMenuItem CopyWithHeader(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[1];

    private static ToolStripMenuItem CopyAll(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[3];

    private static ToolStripMenuItem CopyAllWithHeader(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[4];

    private static ToolStripMenuItem SelectAll(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[6];

    // Simulates "row 0, column 0 was right-clicked" (what OnCellMouseDown
    // captures on a real right-click) - without it the menu correctly
    // cancels itself (see the NoRow/SystemColumn tests below).
    private static void Open(WfuiReadOnlyDataGridView grid, int rowIndex = 0, int columnIndex = 0)
    {
        grid.SetPrivateField("_contextMenuRowIndex", rowIndex);
        grid.SetPrivateField("_contextMenuColumnIndex", columnIndex);
        Menu(grid).InvokePrivate("OnOpening", new CancelEventArgs());
    }

    [Fact]
    public void ReadOnlyGrid_HasTheSameCopyMenuShapeAsListView()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        WfuiContextMenuStrip menu = Menu(grid);

        Assert.Equal(7, menu.Items.Count);
        Assert.Equal("Copy", Copy(menu).Text);
        Assert.Equal("Copy with header", CopyWithHeader(menu).Text);
        Assert.Equal("Copy all", CopyAll(menu).Text);
        Assert.Equal("Copy all with header", CopyAllWithHeader(menu).Text);
        Assert.Equal("Select all", SelectAll(menu).Text);
    }

    [Fact]
    public void NoSelection_DisablesCopy_ButNotSelectAll()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ClearSelection();
        WfuiContextMenuStrip menu = Menu(grid);

        Open(grid);

        Assert.False(Copy(menu).Enabled);
        Assert.False(CopyWithHeader(menu).Enabled);
        Assert.True(SelectAll(menu).Enabled);
    }

    [Fact]
    public void WithSelection_EnablesCopy()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ClearSelection();
        grid.Rows[0].Cells[0].Selected = true;
        WfuiContextMenuStrip menu = Menu(grid);

        Open(grid);

        Assert.True(Copy(menu).Enabled);
        Assert.True(CopyWithHeader(menu).Enabled);
    }

    [Fact]
    public void EmptyGrid_DisablesSelectAll()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems();
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        WfuiContextMenuStrip menu = Menu(grid);

        Open(grid);

        Assert.False(SelectAll(menu).Enabled);
        Assert.False(CopyAll(menu).Enabled);
        Assert.False(CopyAllWithHeader(menu).Enabled);
    }

    [Fact]
    public void NoRowClicked_CancelsTheMenuEntirely()
    {
        // RowIndex -1 covers both the header and the empty space below the
        // last row - same as DataGridView's own menu.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.SetPrivateField("_contextMenuRowIndex", -1);
        grid.SetPrivateField("_contextMenuColumnIndex", -1);
        CancelEventArgs args = new CancelEventArgs();

        Menu(grid).InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
    }

    [Fact]
    public void RightClickOnTheEnumerationColumn_CancelsTheMenuEntirely()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        grid.SetPrivateField("_contextMenuRowIndex", 0);
        grid.SetPrivateField("_contextMenuColumnIndex", grid.Columns["__enumeration"]!.Index);
        CancelEventArgs args = new CancelEventArgs();

        Menu(grid).InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
    }

    // Right-click row selection - the same behavior DataGridView's own
    // OnCellMouseDown always had, now shared via the base class so both
    // grids behave identically.
    [Fact]
    public void RightClickOnAnUnselectedRow_SelectsTheWholeRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableReadOnlyDataGridView grid = new TestableReadOnlyDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = items
        };
        _ = grid.Handle;
        grid.ClearSelection();

        grid.RaiseCellMouseDown(1, 1, MouseButtons.Right);

        Assert.True(grid.Rows[1].Cells[0].Selected);
        Assert.True(grid.Rows[1].Cells[1].Selected);
        Assert.False(grid.Rows[0].Cells[0].Selected);
        Assert.Equal(1, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(1, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
    }

    [Fact]
    public void RightClickWithinAnExistingMultiRowSelection_LeavesItIntact()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableReadOnlyDataGridView grid = new TestableReadOnlyDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = items
        };
        _ = grid.Handle;
        grid.ClearSelection();
        grid.CurrentCell = grid.Rows[0].Cells[0];
        foreach (int rowIndex in new[] { 0, 2 })
        {
            foreach (DataGridViewCell cell in grid.Rows[rowIndex].Cells)
            {
                cell.Selected = true;
            }
        }

        grid.RaiseCellMouseDown(0, 0, MouseButtons.Right);

        Assert.True(grid.Rows[0].Cells[1].Selected);
        Assert.True(grid.Rows[2].Cells[0].Selected);
        Assert.False(grid.Rows[1].Cells[0].Selected);
    }

    [Fact]
    public void RightClickOnTheEnumerationColumn_DoesNotSelectTheRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableReadOnlyDataGridView grid = new TestableReadOnlyDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = items
        };
        _ = grid.Handle;
        grid.ShowEnumeration = true;
        grid.ClearSelection();
        int enumerationIndex = grid.Columns["__enumeration"]!.Index;

        grid.RaiseCellMouseDown(enumerationIndex, 0, MouseButtons.Right);

        Assert.False(grid.Rows[0].Cells[grid.Columns["Name"]!.Index].Selected);
    }

    [Fact]
    public void LeftClick_DoesNotCaptureAContextMenuTarget()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableReadOnlyDataGridView grid = new TestableReadOnlyDataGridView
        {
            BindingContext = new BindingContext(),
            DataSource = items
        };
        _ = grid.Handle;

        grid.RaiseCellMouseDown(0, 0, MouseButtons.Left);

        Assert.Equal(-1, grid.GetPrivateField<int>("_contextMenuRowIndex"));
    }

    [Fact]
    public void Copy_PutsTheSelectedCellsOnTheClipboard()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ClearSelection();
            grid.Rows[0].Cells[grid.Columns["Name"]!.Index].Selected = true;

            Copy(Menu(grid)).PerformClick();

            Assert.Equal("Apple", Clipboard.GetText().Trim());
        });
    }

    private static string NormalizedClipboardText() =>
        Clipboard.GetText().Replace("\r\n", "\n").Trim();

    [Fact]
    public void CopyAsTable_PutsTheHeaderTextOnTopOfTheSelection()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ClearSelection();
            grid.Rows[0].Cells[grid.Columns["Name"]!.Index].Selected = true;
            grid.Rows[0].Cells[grid.Columns["Value"]!.Index].Selected = true;

            CopyWithHeader(Menu(grid)).PerformClick();

            Assert.Equal("Name\tValue\nApple\t42", NormalizedClipboardText());
        });
    }

    // Same wording as ListView's own "... (with header)" copy toast.
    [Theory]
    [InlineData(false, "2 cells copied")]
    [InlineData(true, "2 cells copied (with header)")]
    public void CopyConfirmation_MentionsTheHeaderOnlyWhenItWentAlong(bool withHeader, string expected)
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
            using TestableReadOnlyDataGridView grid = new TestableReadOnlyDataGridView
            {
                BindingContext = new BindingContext(),
                DataSource = items
            };
            _ = grid.Handle;
            grid.ClearSelection();
            grid.Rows[0].Cells[0].Selected = true;
            grid.Rows[0].Cells[1].Selected = true;

            (withHeader ? CopyWithHeader(Menu(grid)) : Copy(Menu(grid))).PerformClick();

            Assert.Equal(new[] { expected }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void CopyAsTable_RestoresTheNormalCopyModeAfterwards()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            System.Windows.Forms.DataGridViewClipboardCopyMode before = grid.ClipboardCopyMode;
            grid.ClearSelection();
            grid.Rows[0].Cells[0].Selected = true;

            CopyWithHeader(Menu(grid)).PerformClick();

            Assert.Equal(before, grid.ClipboardCopyMode);
        });
    }

    [Fact]
    public void CopyAsTable_LeavesTheEnumerationColumnAndItsHeaderOut()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ShowEnumeration = true;
            grid.ClearSelection();
            foreach (DataGridViewCell cell in grid.Rows[0].Cells)
            {
                cell.Selected = true;
            }

            CopyWithHeader(Menu(grid)).PerformClick();

            Assert.Equal("Name\tValue\nApple\t42", NormalizedClipboardText());
        });
    }

    [Fact]
    public void CopyAll_SelectsEverythingAndCopiesItWithoutTheHeader()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ClearSelection();

            CopyAll(Menu(grid)).PerformClick();

            Assert.Equal("A\t1\nB\t2", NormalizedClipboardText());
            Assert.Equal(grid.Rows.Count * grid.Columns.Count, grid.SelectedCells.Count);
        });
    }

    [Fact]
    public void CopyAll_AsTable_SelectsEverythingAndCopiesItWithTheHeader()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ClearSelection();

            CopyAllWithHeader(Menu(grid)).PerformClick();

            Assert.Equal("Name\tValue\nA\t1\nB\t2", NormalizedClipboardText());
            Assert.Equal(grid.Rows.Count * grid.Columns.Count, grid.SelectedCells.Count);
        });
    }

    [Fact]
    public void CopyAll_OnTheEditableGrid_LeavesOutThePlaceholderAndTheDeleteColumn()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using ErikwnkWFUI.Controls.DataGridView grid = new ErikwnkWFUI.Controls.DataGridView
            {
                BindingContext = new BindingContext(),
                DataSource = items
            };
            _ = grid.Handle;
            grid.ActionConfirmation = ErikwnkWFUI.Controls.CopyConfirmationStyle.None;
            grid.ShowDeleteRowColumn = true;
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)grid.ContextMenuStrip!;

            // Cut (0), Copy (1), with header (2), Paste (3), Delete (4), separator (5), Copy all (6), with header (7).
            ((ToolStripMenuItem)menu.Items[7]).PerformClick();

            Assert.Equal("Name\tValue\nA\t1", NormalizedClipboardText());
        });
    }

    [Fact]
    public void SelectAll_SelectsEveryCell()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ClearSelection();

        SelectAll(Menu(grid)).PerformClick();

        Assert.Equal(grid.Rows.Count * grid.Columns.Count, grid.SelectedCells.Count);
    }

    [Fact]
    public void ContextMenuSelectionColor_PropagatesToTheReadOnlyMenu()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        grid.ContextMenuSelectionColor = Color.Red;

        Assert.Equal(Color.Red, Menu(grid).SelectionBackColor);
    }

    [Fact]
    public void EditableGrid_ReplacesTheReadOnlyMenuWithItsOwnFullOne()
    {
        using ErikwnkWFUI.Controls.DataGridView grid = new ErikwnkWFUI.Controls.DataGridView();

        // The five shared entries (Copy, Copy all, each also with header, Select
        // all) plus Cut, Paste, Clear, DeleteRows, InsertAbove, InsertBelow and
        // four separators (Cut rows included).
        Assert.Equal(16, grid.ContextMenuStrip!.Items.Count);
    }
}

/// <summary>Reads UIColors.Primary (UIColorsTests changes it via SetAccent).</summary>
[Collection(AccentColorTestCollection.Name)]
public class ReadOnlyDataGridViewContextMenuColorTests
{
    [Fact]
    public void CreateReadOnlyPrimary_UsesTheAccentForTheContextMenuSelectionColor()
    {
        using WfuiReadOnlyDataGridView grid = (WfuiReadOnlyDataGridView)UIStyles.DataGridViews.CreateReadOnlyPrimary();

        Assert.Equal(UIColors.Primary, grid.ContextMenuSelectionColor);
        Assert.Equal(UIColors.Primary, ((WfuiContextMenuStrip)grid.ContextMenuStrip!).SelectionBackColor);
    }
}
