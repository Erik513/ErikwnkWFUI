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

        // The "type here to add a row" placeholder is left exactly as
        // selectable as stock DataGridView already makes it - a drag-
        // select or Ctrl+A reaching down that far selects it like any
        // other row. An earlier version forced it back out of
        // SelectedCells on every selection change instead, but reading/
        // mutating selection state from OnSelectionChanged while WinForms'
        // own drag-selection loop was still running dropped the drag's own
        // anchor cell from the final selection entirely. The real
        // correctness problems this was trying to solve - copy/paste/
        // clear/delete treating the placeholder as real data - are handled
        // individually at each of those call sites instead
        // (GetClipboardContent, PasteFromClipboard, ClearSelectedCellValues,
        // DeleteRows), each already checking IsNewRow or list membership on
        // its own terms.
        //
        // The one thing genuinely worth cleaning up here: merely having
        // moved through the placeholder at all - a drag passing over it,
        // Ctrl+A, arrow-keying past it, not just clicking directly into
        // it - already makes WinForms call IBindingList.AddNew() on the
        // bound list (same cause as InsertBlankRow/PasteFromClipboard).
        // If nothing was actually typed there, that leaves a genuinely
        // blank item sitting in the list, invisible for now (that row
        // still reports IsNewRow == true, same as ever, until something
        // commits it) - but no longer just a harmless placeholder either:
        // anything that later rebuilds the grid from the list's current
        // contents (e.g. a paste) reveals it as a genuine extra blank
        // row. Leaving the row - rather than every keystroke within it -
        // is the natural, standard point to settle whether it should
        // stick around: RowValidating/CellEndEdit (which is what commits
        // a real edit) already runs before RowLeave, so by the time this
        // fires, CancelNew is a documented no-op for a row that was
        // actually typed into - it only undoes an add nobody meant to
        // make. Deferred via BeginInvoke since CancelNew's own list
        // mutation would otherwise reenter this same row-leave handling
        // while it's still on the call stack.
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
                    // A pending add really was cancelled (the no-op case,
                    // e.g. the user actually typed a new row and moved
                    // on, leaves list.Count unchanged and skips this) -
                    // CancelNew's own list-shrink is itself enough of a
                    // change to make WinForms pick some "current" cell of
                    // its own afterward (observed: the first cell of what
                    // was the last real row), which nobody asked for.
                    // Sweeping it up here, rather than leaving it for
                    // whatever happens to run next, keeps this contained
                    // to exactly the case it caused.
                    ClearSelection();
                    CurrentCell = null;
                }
            }));
        }

        // "Insert row above/below" (see BuildContextMenu) needs the top
        // and bottom of the WHOLE current selection, not just the single
        // row that happened to be right-clicked (_contextMenuRowIndex) -
        // right-clicking a row that's already part of a larger selection
        // leaves that selection intact (see OnCellMouseDown), so
        // right-clicking anywhere within a multi-row selection has to
        // insert above its topmost row / below its bottommost row, not
        // wherever the cursor happened to land inside it. Returns false
        // (leaving both out parameters unset) when nothing is selected -
        // shouldn't normally happen for a row-targeted right-click (see
        // OnCellMouseDown), but callers fall back to _contextMenuRowIndex
        // for that case regardless.
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

        // Inserts one blank row immediately above or below rowIndex,
        // through the same batched suppress-events-then-ResetBindings-once
        // rebuild PasteFromClipboard's own row-insertion uses, for the
        // same reasons (reentrancy while still inside this event handler,
        // and a freshly AddNew()'d row otherwise being left in a
        // still-cancellable state the grid can silently drop later). The
        // context menu itself never offers either direction for the "type
        // here to add a row" placeholder - typing into it is already how
        // a row gets added there - but the guard below still covers it
        // defensively in case this is ever called some other way.
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

            // Right-clicking (or even just clicking) the placeholder
            // itself already makes WinForms call IBindingList.AddNew() on
            // the bound list on its own - same cause as the paste bug
            // this same pattern fixed earlier (see PasteFromClipboard). If
            // that already grew the list past rowIndex, adding another row
            // here would insert two for one call. Only skips when rowIndex
            // is the SAME row that was actually right-clicked
            // (_contextMenuRowIndex) - rowIndex can also be a different,
            // real row resolved from a multi-row selection that happens to
            // include the placeholder (see TryGetSelectedRowIndexRange),
            // and that row was never auto-grown by the click at all.
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

        // Cut for whole rows: puts every selected row (all of its cells,
        // not just the ones that happened to be selected) on the clipboard,
        // then removes those rows - the row-level counterpart of the
        // cell-level Cut above. Like it, the copy and delete steps' own
        // confirmations are suppressed in favor of one dedicated "cut"
        // message.
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

                if (clipboardContent != null)
                {
                    ClipboardAccess.SetDataObject(clipboardContent);
                }

                DeleteRows(rowIndexes);
            });

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
                // The placeholder has nothing to remove from the data
                // source. Selecting it - even just landing CurrentCell on
                // it as part of a larger selection, no typing required -
                // already makes WinForms call IBindingList.AddNew() on the
                // bound list on its own, the same as a real click does
                // (see InsertBlankRow's own comment on this). Left alone,
                // that still-empty pending item would never get cleaned up
                // and would sit in the data source forever as a permanent
                // stray blank row - CancelNew below discards it instead,
                // same as WinForms' own native handling does when a
                // pending add is abandoned rather than committed.
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

                // Single filtering pass instead of one list.Remove(item)
                // call per row - Remove is an O(n) IndexOf-then-shift on
                // List<T>/BindingList<T>, so removing m rows that way costs
                // O(n*m) instead of the O(n) this rebuild does. Filtered by
                // POSITION, not by item identity/HashSet<object> - two
                // different rows can legitimately be bound to the exact
                // same object reference, and a set of items would then
                // remove every occurrence instead of just the one(s)
                // actually selected.
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
