using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Rows: leaving the placeholder row, inserting a blank row, cutting and deleting rows.
    public partial class DataGridView : ReadOnlyDataGridView
    {
        // The "type here to add a row" placeholder stays as selectable as in the standard grid (a drag or Ctrl+A that
        // reaches it selects it). Forcing it out of the selection on every change dropped the anchor cell of a drag; copy,
        // paste, clear and delete treat the placeholder individually instead (GetClipboardContent, PasteFromClipboard,
        // ClearSelectedCellValues, DeleteRows).
        //
        // What is worth cleaning up here: merely passing through the placeholder (drag, Ctrl+A, arrow keys) makes WinForms
        // call IBindingList.AddNew(). If nothing is typed, a blank item stays in the list - invisible, but a rebuild of the
        // grid from the list (a paste) shows it as a real blank row. Leaving the row is the point to settle that:
        // RowValidating and CellEndEdit commit a real edit before RowLeave, so CancelNew is then a no-op for a row that was
        // typed into. Deferred with BeginInvoke, since CancelNew changes the list and would re-enter this handling.
        /// <inheritdoc/>
        protected override void OnRowLeave(DataGridViewCellEventArgs e)
        {
            base.OnRowLeave(e);

            if (e.RowIndex != NewRowIndex || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                if (!(DataSource is ICancelAddNew cancelAddNew) || !(DataSource is IList list) || list.Count == 0)
                {
                    return;
                }

                int countBeforeCancel = list.Count;
                cancelAddNew.CancelNew(list.Count - 1);

                if (list.Count < countBeforeCancel)
                {
                    // A pending add was cancelled (when a real row was typed, the count is unchanged and this is skipped).
                    // The shrinking list makes WinForms pick a current cell of its own (the first cell of the last real
                    // row), which nobody asked for: it is cleared here.
                    ClearSelection();
                    CurrentCell = null;
                }
            }));
        }

        // Insert above/below need the top and bottom row of the whole selection, not the right-clicked row: a right-click
        // inside a larger selection keeps it (OnCellMouseDown). False when nothing is selected; callers then use
        // _contextMenuRowIndex.
        private bool TryGetSelectedRowIndexRange(out int minRowIndex, out int maxRowIndex)
        {
            minRowIndex = int.MaxValue;
            maxRowIndex = -1;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                // The placeholder is left fully selectable like any other
                // row (see the remarks on OnRowLeave) - a drag-select
                // reaching down that far could otherwise make this treat
                // it as the selection's bottommost row, and "insert row
                // below" would then try inserting past it.
                if (IsExcludedFromDataSelection(cell))
                {
                    continue;
                }

                if (cell.RowIndex < minRowIndex)
                {
                    minRowIndex = cell.RowIndex;
                }

                if (cell.RowIndex > maxRowIndex)
                {
                    maxRowIndex = cell.RowIndex;
                }
            }

            return maxRowIndex >= 0;
        }

        // Inserts a blank row above or below rowIndex through the batch that PasteFromClipboard uses (same reasons: re-
        // entrancy in the handler, and an AddNew()'d row left cancellable). The menu offers neither for the placeholder
        // (typing there adds the row); the guard below covers other callers.
        private void InsertBlankRow(int rowIndex, bool above)
        {
            if (!(DataSource is IList list) || rowIndex < 0 || rowIndex > list.Count)
            {
                return;
            }

            if (!(list is IBindingList bindingList) || !bindingList.AllowNew)
            {
                return;
            }

            // Clicking the placeholder already makes WinForms call AddNew() (see PasteFromClipboard); if that grew the list
            // past rowIndex, another row would make two. Only for the row that was right-clicked: rowIndex can be a real
            // row of a selection that includes the placeholder, which the click did not grow.
            if (_contextMenuRowWasPlaceholder && rowIndex == _contextMenuRowIndex && rowIndex < list.Count)
            {
                return;
            }

            int insertAtIndex = above ? rowIndex : rowIndex + 1;

            ApplyBatchedDataSourceChange(() =>
            {
                List<object> originalItems = SnapshotItems(list);
                object newItem = bindingList.AddNew();
                InsertItemsAt(list, originalItems, insertAtIndex, new[] { newItem });
            });

            ShowActionConfirmation(UIStrings.Get("DataGridView.RowInserted"));
        }

        // Cut for whole rows: every selected row with all its cells goes to the clipboard, then the rows are removed. As
        // for cells, one "cut" message replaces the confirmations of the two steps.
        private void CutSelectedRows()
        {
            HashSet<int> rowIndexes = new HashSet<int>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex >= 0 && !IsPlaceholderRowIndex(cell.RowIndex))
                {
                    rowIndexes.Add(cell.RowIndex);
                }
            }

            if (rowIndexes.Count == 0)
            {
                return;
            }

            bool copied = true;

            RunWithSuppressedActionConfirmation(() =>
            {
                // Widen the selection to the full rows first so the
                // clipboard text is the whole row, whatever cells the
                // user had actually picked.
                ClearSelection();

                foreach (int rowIndex in rowIndexes)
                {
                    foreach (DataGridViewCell cell in Rows[rowIndex].Cells)
                    {
                        if (cell.Visible)
                        {
                            cell.Selected = true;
                        }
                    }
                }

                DataObject clipboardContent = GetClipboardContent();

                // (A cut whose copy did not work removes nothing: the rows would be gone.)
                if (clipboardContent != null && !ClipboardAccess.TrySetDataObject(clipboardContent))
                {
                    copied = false;

                    return;
                }

                DeleteRows(rowIndexes);
            });

            if (!copied)
            {
                return;
            }

            string message = rowIndexes.Count == 1
                ? UIStrings.Get("DataGridView.RowCut")
                : string.Format(UIStrings.Get("DataGridView.RowsCut"), rowIndexes.Count);
            ShowActionConfirmation(message);
        }

        private void DeleteSelectedRows()
        {
            HashSet<int> rowIndexes = new HashSet<int>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex >= 0)
                {
                    rowIndexes.Add(cell.RowIndex);
                }
            }

            DeleteRows(rowIndexes);
        }

        private void DeleteItem(object item)
        {
            if (item == null || !(DataSource is IList list))
            {
                return;
            }

            ApplyBatchedDataSourceChange(() => list.Remove(item));
            ShowActionConfirmation(UIStrings.Get("DataGridView.RowDeleted"));
        }

        private void DeleteRows(IEnumerable<int> rowIndexes)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            HashSet<int> indexesToRemove = new HashSet<int>();
            HashSet<int> placeholderIndexesToCancel = new HashSet<int>();

            foreach (int rowIndex in rowIndexes)
            {
                // The placeholder has nothing to remove, but selecting it made WinForms call AddNew(); the pending empty
                // item would stay as a stray row, so it is cancelled (CancelNew, as WinForms does for an abandoned add).
                if (IsPlaceholderRowIndex(rowIndex))
                {
                    placeholderIndexesToCancel.Add(rowIndex);
                    continue;
                }

                indexesToRemove.Add(rowIndex);
            }

            if (indexesToRemove.Count == 0 && placeholderIndexesToCancel.Count == 0)
            {
                return;
            }

            ApplyBatchedDataSourceChange(() =>
            {
                // Cancelled first, not folded into the rebuild loop below -
                // the placeholder is always the LAST row, so discarding its
                // still-pending item (if any) never shifts the position of
                // any real row indexesToRemove refers to.
                if (list is ICancelAddNew cancelAddNew)
                {
                    foreach (int placeholderIndex in placeholderIndexesToCancel)
                    {
                        cancelAddNew.CancelNew(placeholderIndex);
                    }
                }

                // One pass that filters by position, not one Remove per row (O(n*m)) and not by item: two rows can be bound
                // to the same object, and removing by item would remove all of them.
                List<object> remainingItems = new List<object>(list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    if (!indexesToRemove.Contains(i))
                    {
                        remainingItems.Add(list[i]);
                    }
                }

                list.Clear();

                foreach (object item in remainingItems)
                {
                    list.Add(item);
                }
            });

            if (indexesToRemove.Count > 0)
            {
                string message = indexesToRemove.Count == 1
                    ? UIStrings.Get("DataGridView.RowDeleted")
                    : string.Format(UIStrings.Get("DataGridView.RowsDeleted"), indexesToRemove.Count);
                ShowActionConfirmation(message);
            }
        }
    }
}
