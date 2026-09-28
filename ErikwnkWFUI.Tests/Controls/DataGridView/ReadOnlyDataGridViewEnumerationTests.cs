using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// ShowEnumeration - the optional "#" row-number column, the mirror image
/// of DataGridView's own rightmost delete-row column but pinned leftmost,
/// sortable, and always showing each row's current position. CycleSort
/// itself is invoked directly via reflection (see PrivateReflection), same
/// as DataGridViewColumnReorderTests - the header click that reaches it in
/// real use isn't practical to drive headlessly.
/// </summary>
public class ReadOnlyDataGridViewEnumerationTests
{
    [Fact]
    public void ShowEnumeration_DefaultsToFalse()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

        Assert.False(grid.ShowEnumeration);
    }

    [Fact]
    public void ShowEnumeration_True_AddsAPinnedLeftmostColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.ShowEnumeration = true;

        Assert.Equal(0, grid.Columns["__enumeration"]!.DisplayIndex);
    }

    [Fact]
    public void ShowEnumeration_StaysLeftmost_AfterAnotherColumnIsAdded()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;

        grid.Columns.Add("Extra", "Extra");

        Assert.Equal(0, grid.Columns["__enumeration"]!.DisplayIndex);
    }

    [Fact]
    public void ShowEnumeration_False_RemovesTheColumn()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;

        grid.ShowEnumeration = false;

        Assert.Null(grid.Columns["__enumeration"]);
    }

    [Fact]
    public void ShowEnumeration_NumbersRowsOneBased_TopToBottom()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        Assert.Equal(1, grid.Rows[0].Cells[columnIndex].Value);
        Assert.Equal(2, grid.Rows[1].Cells[columnIndex].Value);
        Assert.Equal(3, grid.Rows[2].Cells[columnIndex].Value);
    }

    [Fact]
    public void ShowEnumeration_IsSortable_UnlikeDeleteColumn()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;

        Assert.NotEqual(DataGridViewColumnSortMode.NotSortable, grid.Columns["__enumeration"]!.SortMode);
    }

    // Confirmed live before this existed: clicking the enumeration column's
    // own header genuinely reorders the OTHER columns too, via the exact
    // same CycleSort/ReorderDataSource mechanism as any other column -
    // ascending is a no-op on the very first click (rows are already in
    // that order, since they're numbered live), descending is where the
    // reversal actually becomes visible.
    [Fact]
    public void CycleSort_OnEnumerationColumn_Descending_ReversesRowOrder()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        grid.InvokePrivate("CycleSort", columnIndex); // ascending
        grid.InvokePrivate("CycleSort", columnIndex); // descending

        Assert.Equal("C", grid.Rows[0].Cells["Name"].Value);
        Assert.Equal("B", grid.Rows[1].Cells["Name"].Value);
        Assert.Equal("A", grid.Rows[2].Cells["Name"].Value);
    }

    // The fix for the confusing case above: without this, the numbers
    // always renumbered to 1..N regardless of direction, so a descending
    // sort visibly reordered every OTHER column but left the enumeration
    // column itself looking completely unchanged.
    [Fact]
    public void CycleSort_OnEnumerationColumn_Descending_CountsDownInstead()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        grid.InvokePrivate("CycleSort", columnIndex); // ascending
        grid.InvokePrivate("CycleSort", columnIndex); // descending

        Assert.Equal(3, grid.Rows[0].Cells[columnIndex].Value);
        Assert.Equal(2, grid.Rows[1].Cells[columnIndex].Value);
        Assert.Equal(1, grid.Rows[2].Cells[columnIndex].Value);
    }

    [Fact]
    public void CycleSort_OnEnumerationColumn_ThirdClick_RestoresAscendingNumbering()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        grid.InvokePrivate("CycleSort", columnIndex); // ascending
        grid.InvokePrivate("CycleSort", columnIndex); // descending
        grid.InvokePrivate("CycleSort", columnIndex); // back to original order

        Assert.Equal(1, grid.Rows[0].Cells[columnIndex].Value);
        Assert.Equal(2, grid.Rows[1].Cells[columnIndex].Value);
        Assert.Equal(3, grid.Rows[2].Cells[columnIndex].Value);
    }

    // Sorting by a DIFFERENT column still reorders rows, but the
    // enumeration column's own job there is just "which visual row is
    // this" - it stays plain ascending regardless of what drove the
    // reorder, unlike when it's the active sort column itself.
    [Fact]
    public void CycleSort_OnAnotherColumn_EnumerationStaysAscending()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int enumIndex = grid.Columns["__enumeration"]!.Index;
        int nameIndex = grid.Columns["Name"]!.Index;

        grid.InvokePrivate("CycleSort", nameIndex); // ascending
        grid.InvokePrivate("CycleSort", nameIndex); // descending - reverses Name order

        Assert.Equal(1, grid.Rows[0].Cells[enumIndex].Value);
        Assert.Equal(2, grid.Rows[1].Cells[enumIndex].Value);
        Assert.Equal(3, grid.Rows[2].Cells[enumIndex].Value);
    }

    [Fact]
    public void ShowEnumeration_HeaderTextDefaultsToHash()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;

        Assert.Equal("#", grid.Columns["__enumeration"]!.HeaderText);
    }

    // "#" doubles as the active-sort arrow's own header once this column
    // is being sorted - showing both looked like the "#" itself was
    // somehow changing, so it blanks out while a sort on this exact
    // column is active. The sort glyph itself (ColumnHeaderPainting.DrawSortGlyph)
    // is drawn independently of this and keeps showing regardless.
    [Fact]
    public void CycleSort_OnEnumerationColumn_HeaderTextBlanksWhileActive()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;
        DataGridViewColumn column = grid.Columns["__enumeration"]!;

        grid.InvokePrivate("CycleSort", column.Index); // ascending
        Assert.Equal(string.Empty, column.HeaderText);

        grid.InvokePrivate("CycleSort", column.Index); // descending
        Assert.Equal(string.Empty, column.HeaderText);
    }

    // Confirmed live: setting the NATIVE SortGlyphDirection alongside an
    // EMPTY HeaderText makes WinForms' own default header painting draw a
    // second, native glyph on top of ColumnHeaderPainting.DrawSortGlyph's
    // own - the two overlapping into a single "bowtie" blob instead of one
    // clean triangle. CycleSort skips the native property for this column
    // specifically to avoid it; this locks that in.
    [Fact]
    public void CycleSort_OnEnumerationColumn_NeverSetsNativeSortGlyphDirection()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;
        DataGridViewColumn column = grid.Columns["__enumeration"]!;

        grid.InvokePrivate("CycleSort", column.Index); // ascending
        Assert.Equal(SortOrder.None, column.HeaderCell.SortGlyphDirection);

        grid.InvokePrivate("CycleSort", column.Index); // descending
        Assert.Equal(SortOrder.None, column.HeaderCell.SortGlyphDirection);
    }

    [Fact]
    public void CycleSort_ThirdClick_RestoresHeaderText()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;
        DataGridViewColumn column = grid.Columns["__enumeration"]!;

        grid.InvokePrivate("CycleSort", column.Index);
        grid.InvokePrivate("CycleSort", column.Index);
        grid.InvokePrivate("CycleSort", column.Index); // back to original order

        Assert.Equal("#", column.HeaderText);
    }

    // Sorting a DIFFERENT column must never blank this column's own header -
    // only being the active sort column itself does that.
    [Fact]
    public void CycleSort_OnAnotherColumn_EnumerationHeaderStaysHash()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int nameIndex = grid.Columns["Name"]!.Index;

        grid.InvokePrivate("CycleSort", nameIndex);

        Assert.Equal("#", grid.Columns["__enumeration"]!.HeaderText);
    }

    [Fact]
    public void ShowEnumeration_ColumnWidthGrows_WhenRowCountReachesMoreDigits()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        DataGridViewColumn column = grid.Columns["__enumeration"]!;
        int narrowWidth = column.Width;

        for (int i = 0; i < 100; i++)
        {
            items.Add(new TestItem("X", i));
        }

        Assert.True(column.Width > narrowWidth);
    }

    // Confirmed live: MERELY reading InheritedStyle doesn't invoke
    // CellFormatting at all (that's a separate, formatting-specific event) -
    // OnCellFormatting is invoked directly here instead, the only way to
    // exercise the actual code path a real paint would use.
    [Fact]
    public void EnumerationCell_OnCellFormatting_SelectionColorsMatchRestingColors()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        DataGridViewCellStyle style = new DataGridViewCellStyle
        {
            BackColor = Color.Blue,
            ForeColor = Color.White
        };
        DataGridViewCellFormattingEventArgs args = new DataGridViewCellFormattingEventArgs(columnIndex, 0, 1, typeof(int), style);

        grid.InvokePrivate("OnCellFormatting", args);

        Assert.Equal(Color.Blue, style.SelectionBackColor);
        Assert.Equal(Color.White, style.SelectionForeColor);
    }

    // The "type here to add a row" placeholder committing to a real row
    // goes through WinForms' own IBindingList.AddNew()/commit flow, never
    // through ApplyBatchedDataSourceChange/ResetBindings() -
    // OnDataBindingComplete's own renumber never sees it, so OnUserAddedRow
    // is the dedicated hook for this case. Not practical to simulate the
    // actual placeholder-typing flow headlessly, so this instead
    // deliberately desyncs a stored number first (the same way that flow
    // would leave it) and confirms invoking the hook corrects it.
    [Fact]
    public void OnUserAddedRow_RenumbersTheEnumerationColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowEnumeration = true;
        int columnIndex = grid.Columns["__enumeration"]!.Index;

        grid.Rows[0].Cells[columnIndex].Value = 99;

        grid.InvokePrivate("OnUserAddedRow", new DataGridViewRowEventArgs(grid.Rows[0]));

        Assert.Equal(1, grid.Rows[0].Cells[columnIndex].Value);
    }

    // ReadOnly alone (already set - see ShowEnumeration) only blocks
    // editing, not copying - GetClipboardContent used to have no idea the
    // enumeration column even existed, so a selection spanning it and a
    // real column copied both. IsSystemColumn (shared with the delete
    // column's own exclusion) fixes that.
    [Fact]
    public void GetClipboardContent_ExcludesTheEnumerationColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("Apple", 42));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
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
    public void GetPasteTargetColumns_ExcludesTheEnumerationColumn()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        grid.Columns.Add("Name", "Name");
        grid.ShowEnumeration = true;

        List<DataGridViewColumn> targetColumns = grid.InvokePrivate<List<DataGridViewColumn>>("GetPasteTargetColumns")!;

        Assert.DoesNotContain(targetColumns, column => column.Name == "__enumeration");
    }

    // The context menu never even opens over the delete column (see
    // DataGridView's own BuildContextMenu/IsSystemColumn) - same now for
    // the enumeration column, invoked directly via the menu's own
    // protected OnOpening rather than a real right-click, which isn't
    // practical to drive headlessly.
    [Fact]
    public void ContextMenu_Opening_IsCancelled_WhenRightClickedOnTheEnumerationColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowEnumeration = true;
        int enumIndex = grid.Columns["__enumeration"]!.Index;

        grid.SetPrivateField("_contextMenuRowIndex", 0);
        grid.SetPrivateField("_contextMenuColumnIndex", enumIndex);

        CancelEventArgs args = new CancelEventArgs();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)grid.ContextMenuStrip!;
        menu.InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
    }

    // A real column being dragged must never land ahead of the pinned
    // leftmost enumeration column or past the pinned rightmost delete
    // column - both are already excluded from being the column that
    // STARTS a drag, but the drop TARGET is a separate computation
    // (GetColumnDropInsertionIndex, invoked directly via reflection since
    // an actual drag isn't practical to drive headlessly) that used to
    // have no idea either column existed.
    [Fact]
    public void GetColumnDropInsertionIndex_NeverInsertsBeforeTheEnumerationColumn()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.ShowEnumeration = true;
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Value", "Value");

        // x=0 - as far left as a drop target can be, which would normally
        // mean "insert as the very first column" (display index 0), i.e.
        // ahead of the enumeration column.
        int insertionIndex = grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 0);

        Assert.Equal(1, insertionIndex);
    }

    [Fact]
    public void GetColumnDropInsertionIndex_NeverInsertsPastTheDeleteColumn()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Value", "Value");
        grid.ShowDeleteRowColumn = true;

        // Far past every column's cumulative width - would normally mean
        // "insert as the very last column" (display index 3, past the
        // delete column at the end).
        int insertionIndex = grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 100_000);

        Assert.Equal(2, insertionIndex);
    }

    [Fact]
    public void GetColumnDropInsertionIndex_ClampsBothEnds_WhenBothPinnedColumnsArePresent()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        grid.Columns.Add("Name", "Name");
        grid.ShowEnumeration = true;
        grid.ShowDeleteRowColumn = true;

        // Columns end up as: #, Name, delete (display indexes 0, 1, 2).
        Assert.Equal(1, grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 0));
        Assert.Equal(2, grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 100_000));
    }

    // Confirms the clamp is a no-op (not just coincidentally landing on
    // the same numbers) when neither pinned column exists - the ordinary,
    // unrestricted reorder behavior every other DataGridView already had.
    [Fact]
    public void GetColumnDropInsertionIndex_IsUnclamped_WhenNoPinnedColumnsArePresent()
    {
        using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Value", "Value");

        Assert.Equal(0, grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 0));
        Assert.Equal(2, grid.InvokePrivate<int>("GetColumnDropInsertionIndex", 100_000));
    }
}
