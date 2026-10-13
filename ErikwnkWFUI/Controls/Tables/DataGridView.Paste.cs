using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Pasting from the clipboard: into the selection, into new rows, converting the text into the values of the columns.
    public partial class DataGridView : ReadOnlyDataGridView
    {

        /// <summary>
        /// Pastes tab-separated, newline-separated text (what Excel, Word -
        /// copying a table - and this grid's own Ctrl+C all put on the
        /// clipboard as plain text) starting at the top-left of the
        /// current selection. Only the selected rows are overwritten in
        /// place; a pasted block taller than the selection gets the extra
        /// rows inserted right after it, via the bound list's own
        /// <see cref="IBindingList.AddNew"/>, rather than overwriting
        /// whatever real rows happened to already be sitting there.
        /// </summary>
        private void PasteFromClipboard()
        {
            if (!ClipboardAccess.ContainsText())
            {
                return;
            }

            string text = ClipboardAccess.GetText();

            if (string.IsNullOrEmpty(text) || !(DataSource is IList list))
            {
                return;
            }

            string[] pastedRows = text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n').Split('\n');

            if (pastedRows.Length == 0)
            {
                return;
            }

            List<DataGridViewColumn> targetColumns = GetPasteTargetColumns();

            if (targetColumns.Count == 0)
            {
                return;
            }

            if (TryFillSingleValueIntoSelection(list, pastedRows))
            {
                return;
            }

            (int startColumnIndex, int minRowIndex, HashSet<int> selectedRealRowIndexes, bool placeholderWasTouched) =
                ResolvePasteAnchor(targetColumns);

            // E.g. pasting 10 rows onto a 4-row selection in the middle of
            // a 20-row list must not clobber rows 5-10 of someone's
            // existing data - it should push them down by 6 instead.
            // Sorted so the write loop below can target the actual
            // selected rows in order, not just walk sequentially from
            // startDataRowIndex - a non-contiguous selection (e.g. rows 0
            // and 5 only) must overwrite exactly those rows, not rows 0
            // and 1.
            List<int> sortedSelectedRowIndexes = new List<int>(selectedRealRowIndexes);
            sortedSelectedRowIndexes.Sort();

            IBindingList bindingList = list as IBindingList;

            // Growing the list AND writing the pasted values both happen
            // inside this one suppress-events-then-ResetBindings-once
            // helper (CycleSort/DeleteRows already use it) rather than as
            // two separate steps:
            //
            // - Reentrancy: with change notifications live, each
            //   individual mutation reenters the grid's own
            //   binding-complete handling while still inside this
            //   key-press handler (the same kind of reentrancy that
            //   caused real crashes for sort/delete before they went
            //   through this helper).
            //
            // - New rows vanishing: a first version of this wrote pasted
            //   values afterward, through DataGridViewCell.Value, once
            //   the grid had already rebound and displayed the freshly
            //   added blank rows. BindingList&lt;T&gt; treats whatever
            //   AddNew() just added as still cancellable until something
            //   commits it - normally a side effect of the grid's own
            //   edit-commit handling once a user types into a cell - and
            //   setting Value programmatically doesn't trigger that same
            //   commit. The row silently vanished the next time focus
            //   moved elsewhere, as if the add had been cancelled - even
            //   calling BindingList&lt;T&gt;.EndNew() by hand right after
            //   AddNew() didn't stop it, so something in the grid's own
            //   side of this (separate from BindingList's own pending-add
            //   tracking) was still involved. Setting the pasted values
            //   directly on the bound objects here instead - before
            //   ResetBindings ever shows the grid a "freshly added, still
            //   empty" row at all - sidesteps that whole lifecycle: the
            //   row only ever appears already complete.
            ApplyBatchedDataSourceChange(() =>
            {
                // Discards a pending-but-abandoned placeholder add before
                // anything below reads list.Count - the placeholder is
                // purely a visual way to add rows by typing, and pasting
                // instead of typing means that pending item was never
                // really wanted (same reasoning as DeleteRows' own
                // placeholderIndexesToCancel). Must run first, before
                // anything below reads list.Count - startDataRowIndex's own
                // fallback (nothing selected -> append at the end) and the
                // overflow/trim math right after it both need to see the
                // post-cancel count, not the stale one from before this
                // batch started.
                if (placeholderWasTouched && list is ICancelAddNew cancelAddNew)
                {
                    cancelAddNew.CancelNew(list.Count - 1);
                }

                // Every index in selectedRealRowIndexes/minRowIndex is
                // already guaranteed to be a real row (the placeholder was
                // excluded above, before either was ever populated) -
                // nothing selected at all (a plain append at the end of
                // the list) is the only remaining case minRowIndex's
                // sentinel has to cover.
                int startDataRowIndex = minRowIndex != int.MaxValue ? minRowIndex : list.Count;

                // Where overflow rows land once InsertItemsAt grows the
                // list - computed here (not just inside the overflow
                // branch below) so the write loop can use the same value
                // regardless of whether growth actually happened.
                int insertAtIndex = startDataRowIndex + selectedRealRowIndexes.Count;

                int overflowRowCount = pastedRows.Length - selectedRealRowIndexes.Count;

                if (overflowRowCount > 0 && (bindingList == null || !bindingList.AllowNew))
                {
                    // Can't grow the list - still worth filling whatever
                    // existing rows the paste does reach below.
                    pastedRows = Trim(pastedRows, list.Count - startDataRowIndex);
                    overflowRowCount = 0;
                }

                if (overflowRowCount > 0)
                {
                    InsertOverflowRows(list, bindingList, insertAtIndex, overflowRowCount);
                }

                WritePastedValues(list, pastedRows, sortedSelectedRowIndexes, insertAtIndex, targetColumns, startColumnIndex);
            });

            string message = pastedRows.Length == 1
                ? UIStrings.Get("DataGridView.RowPasted")
                : string.Format(UIStrings.Get("DataGridView.RowsPasted"), pastedRows.Length);
            ShowActionConfirmation(message);
        }

        // Copying exactly one cell, then pasting into a selection of more
        // than one, fills every selected cell with that same value
        // (standard spreadsheet "fill" behavior) instead of anchoring at
        // the top-left and only ever writing the one value there. Copying
        // more than one cell never fills/tiles a larger selection this
        // way, regardless of its size - it's always just anchored at the
        // top-left and extends from there (same as always), matching
        // Excel's own paste behavior. Returns true when it actually
        // handled the paste this way, so PasteFromClipboard knows to skip
        // its own normal anchor-based paste entirely.
        private bool TryFillSingleValueIntoSelection(IList list, string[] pastedRows)
        {
            if (pastedRows.Length != 1 || pastedRows[0].Split('\t').Length != 1)
            {
                return false;
            }

            // Captured as plain (row, column) index pairs, not the
            // DataGridViewCell references themselves - FillCellsWithValue
            // cancels a pending placeholder add before writing, and a
            // DataGridViewCell held onto across that mutation turned out
            // to no longer resolve to the row it was read from.
            List<(int RowIndex, int ColumnIndex)> fillTargetCells = new List<(int, int)>();
            bool fillTouchedPlaceholder = false;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsSystemColumn(cell.ColumnIndex))
                {
                    continue;
                }

                if (IsPlaceholderRowIndex(cell.RowIndex))
                {
                    fillTouchedPlaceholder = true;
                    continue;
                }

                fillTargetCells.Add((cell.RowIndex, cell.ColumnIndex));
            }

            if (fillTargetCells.Count <= 1)
            {
                return false;
            }

            FillCellsWithValue(list, fillTargetCells, pastedRows[0], fillTouchedPlaceholder);
            return true;
        }

        // Where a paste anchors: the top-left of the current SELECTION,
        // not just CurrentCell - CurrentCell is whichever cell was clicked
        // or navigated to LAST within a multi-cell selection (e.g. the far
        // corner of a drag-select or a Shift+Right extension), not
        // necessarily where the selection starts, so using it alone put
        // paste in the wrong column/row whenever more than one cell was
        // selected. The delete column is never a valid anchor (nothing to
        // paste into it), same as it's never a paste target at all (see
        // GetPasteTargetColumns). PlaceholderWasTouched is set when the
        // placeholder was part of what got interacted with (selected, or
        // left as CurrentCell) - merely that, with nothing typed, already
        // made WinForms call IBindingList.AddNew() on the bound list on
        // its own (see InsertBlankRow's own comment on this); the
        // placeholder itself is still correctly excluded from
        // SelectedRealRowIndexes either way, this only tracks whether that
        // now-pending, still-empty item needs cancelling so it doesn't
        // linger as a permanent stray row once the paste is done.
        private (int StartColumnIndex, int MinRowIndex, HashSet<int> SelectedRealRowIndexes, bool PlaceholderWasTouched)
            ResolvePasteAnchor(List<DataGridViewColumn> targetColumns)
        {
            int minRowIndex = int.MaxValue;
            DataGridViewColumn minColumn = null;
            HashSet<int> selectedRealRowIndexes = new HashSet<int>();
            bool placeholderWasTouched = false;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsSystemColumn(cell.ColumnIndex))
                {
                    continue;
                }

                // The COLUMN anchor is tracked regardless of whether this
                // cell's row is the placeholder - selecting column 2 of
                // the placeholder and pasting must still start at column
                // 2 for whatever real row the paste ends up writing/
                // inserting, not silently fall back to column 0 just
                // because that particular cell's row got excluded below.
                DataGridViewColumn column = Columns[cell.ColumnIndex];

                if (minColumn == null || column.DisplayIndex < minColumn.DisplayIndex)
                {
                    minColumn = column;
                }

                // The placeholder is never a paste target itself, same as
                // the delete column - a selection that includes it (e.g. a
                // drag-select or Ctrl+A reaching down that far) must only
                // ever affect the real rows above it.
                if (IsPlaceholderRowIndex(cell.RowIndex))
                {
                    placeholderWasTouched = true;
                    continue;
                }

                if (cell.RowIndex < minRowIndex)
                {
                    minRowIndex = cell.RowIndex;
                }

                selectedRealRowIndexes.Add(cell.RowIndex);
            }

            if (minColumn == null && CurrentCell != null)
            {
                minColumn = CurrentCell.OwningColumn;

                if (IsPlaceholderRowIndex(CurrentCell.RowIndex))
                {
                    placeholderWasTouched = true;
                }
                else
                {
                    minRowIndex = CurrentCell.RowIndex;
                    selectedRealRowIndexes.Add(CurrentCell.RowIndex);
                }
            }

            int startColumnIndex = minColumn != null ? targetColumns.IndexOf(minColumn) : -1;

            if (startColumnIndex < 0)
            {
                startColumnIndex = 0;
            }

            return (startColumnIndex, minRowIndex, selectedRealRowIndexes, placeholderWasTouched);
        }

        // See InsertItemsAt for why this rebuilds via Clear() + re-Add()
        // rather than list.Insert() at a hand-picked index, and why
        // originalItems has to be snapshotted before AddNew() below, not
        // derived from list after.
        private void InsertOverflowRows(IList list, IBindingList bindingList, int insertAtIndex, int overflowRowCount)
        {
            List<object> originalItems = SnapshotItems(list);
            List<object> newItems = new List<object>(overflowRowCount);

            for (int i = 0; i < overflowRowCount; i++)
            {
                newItems.Add(bindingList.AddNew());
            }

            InsertItemsAt(list, originalItems, insertAtIndex, newItems);
        }

        private void WritePastedValues(
            IList list, string[] pastedRows, List<int> sortedSelectedRowIndexes,
            int insertAtIndex, List<DataGridViewColumn> targetColumns, int startColumnIndex)
        {
            // Looked up once per (item type, property name) rather than
            // once per pasted cell - GetProperty for the same column
            // resolves to the same PropertyInfo on every row for the
            // common case of a homogeneous bound list, so repeating that
            // lookup per row was pure waste on a paste of any real size.
            // Keyed by the item's own runtime type (not just the property
            // name) so a list that genuinely mixes item types - unusual,
            // but IList doesn't rule it out - still resolves correctly per
            // item.
            Dictionary<(Type ItemType, string PropertyName), PropertyInfo> propertyCache =
                new Dictionary<(Type, string), PropertyInfo>();

            for (int rowOffset = 0; rowOffset < pastedRows.Length; rowOffset++)
            {
                // Write into the actually-selected rows first, in
                // ascending order (not sequentially from
                // startDataRowIndex), then into the newly-inserted
                // overflow rows once the selection is exhausted.
                int dataRowIndex = rowOffset < sortedSelectedRowIndexes.Count
                    ? sortedSelectedRowIndexes[rowOffset]
                    : insertAtIndex + (rowOffset - sortedSelectedRowIndexes.Count);

                if (dataRowIndex >= list.Count)
                {
                    break;
                }

                object targetItem = list[dataRowIndex];
                string[] cellValues = pastedRows[rowOffset].Split('\t');

                for (int columnOffset = 0;
                    columnOffset < cellValues.Length && startColumnIndex + columnOffset < targetColumns.Count;
                    columnOffset++)
                {
                    DataGridViewColumn column = targetColumns[startColumnIndex + columnOffset];
                    TrySetCellValueFromText(targetItem, column, cellValues[columnOffset], propertyCache);
                }
            }
        }

        private static bool TryConvertPastedValue(string text, Type targetType, out object convertedValue)
        {
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (string.IsNullOrEmpty(text))
            {
                // Blank pasted into a nullable/reference column clears it;
                // a non-nullable value type has no such thing as "blank",
                // so that cell is left alone instead of guessing a default.
                bool canBeNull = targetType != underlyingType || !underlyingType.IsValueType;
                convertedValue = null;
                return canBeNull;
            }

            if (underlyingType == typeof(string))
            {
                convertedValue = text;
                return true;
            }

            try
            {
                convertedValue = underlyingType.IsEnum
                    ? Enum.Parse(underlyingType, text, ignoreCase: true)
                    : Convert.ChangeType(text, underlyingType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is ArgumentException)
            {
                convertedValue = null;
                return false;
            }
        }

        // Shared by PasteFromClipboard's own WritePastedValues and
        // FillCellsWithValue below, which used to each carry an identical
        // copy of this same cache-lookup-convert-set sequence. ReadOnly/
        // unbound columns are silently skipped, same as a cell that fails
        // to convert (e.g. "abc" into a number column) - best effort,
        // never aborts the whole paste/fill over one bad cell.
        private static void TrySetCellValueFromText(
            object targetItem, DataGridViewColumn column, string text,
            Dictionary<(Type ItemType, string PropertyName), PropertyInfo> propertyCache)
        {
            if (column.ReadOnly || string.IsNullOrEmpty(column.DataPropertyName))
            {
                return;
            }

            Type targetItemType = targetItem.GetType();
            (Type, string) propertyCacheKey = (targetItemType, column.DataPropertyName);

            if (!propertyCache.TryGetValue(propertyCacheKey, out PropertyInfo property))
            {
                property = targetItemType.GetProperty(column.DataPropertyName);
                propertyCache[propertyCacheKey] = property;
            }

            if (property == null)
            {
                return;
            }

            if (TryConvertPastedValue(text, property.PropertyType, out object convertedValue))
            {
                property.SetValue(targetItem, convertedValue);
            }
        }

        // Writes the same value into every one of targetCells (the "fill"
        // path PasteFromClipboard uses when exactly one cell was copied
        // into a selection of more than one) - never grows the list, since
        // every cell here is already a real, existing row.
        private void FillCellsWithValue(IList list, List<(int RowIndex, int ColumnIndex)> targetCells, string value, bool cancelPendingPlaceholder)
        {
            ApplyBatchedDataSourceChange(() =>
            {
                // Same reasoning as PasteFromClipboard's own placeholder
                // cancellation - a pending add from merely including the
                // placeholder in the selection was never really wanted
                // once the user pastes instead of types.
                if (cancelPendingPlaceholder && list is ICancelAddNew cancelAddNew)
                {
                    cancelAddNew.CancelNew(list.Count - 1);
                }

                Dictionary<(Type ItemType, string PropertyName), PropertyInfo> propertyCache =
                    new Dictionary<(Type, string), PropertyInfo>();

                foreach ((int rowIndex, int columnIndex) in targetCells)
                {
                    if (rowIndex >= list.Count)
                    {
                        continue;
                    }

                    TrySetCellValueFromText(list[rowIndex], Columns[columnIndex], value, propertyCache);
                }
            });

            ShowActionConfirmation(string.Format(UIStrings.Get("DataGridView.CellsPasted"), targetCells.Count));
        }

        private static string[] Trim(string[] values, int maxLength)
        {
            if (maxLength >= values.Length)
            {
                return values;
            }

            if (maxLength <= 0)
            {
                return Array.Empty<string>();
            }

            string[] trimmed = new string[maxLength];
            Array.Copy(values, trimmed, maxLength);
            return trimmed;
        }

        // Every data column in display order - the optional delete column
        // (see ShowDeleteRowColumn) is never a paste target, there's
        // nothing meaningful to paste into it.
        private List<DataGridViewColumn> GetPasteTargetColumns()
        {
            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();

            foreach (DataGridViewColumn column in Columns)
            {
                if (!IsSystemColumn(column.Index))
                {
                    columns.Add(column);
                }
            }

            return ColumnLayoutMath.OrderByDisplayIndex(columns, column => column.DisplayIndex);
        }
    }
}
