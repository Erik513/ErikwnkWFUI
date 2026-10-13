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

            // Pasting 10 rows onto a 4-row selection in the middle of 20 rows pushes the rest down by 6; it does not
            // overwrite it. Sorted, so that a selection that is not contiguous (rows 0 and 5) overwrites exactly those
            // rows.
            List<int> sortedSelectedRowIndexes = new List<int>(selectedRealRowIndexes);
            sortedSelectedRowIndexes.Sort();

            IBindingList bindingList = list as IBindingList;

            // Growing the list and writing the pasted values are one batch (ApplyBatchedDataSourceChange, as for sort and
            // delete):
            // - with change notifications live, each mutation re-enters the binding handling of the grid while this key
            //   handler is still running;
            // - the values go straight onto the bound objects, before the grid shows the new rows. A row filled in
            //   afterwards through DataGridViewCell.Value is not committed (BindingList treats what AddNew added as
            //   cancellable until the grid's edit handling commits it) and vanished at the next change of focus; EndNew()
            //   by hand did not help.
            ApplyBatchedDataSourceChange(() =>
            {
                // Discards the pending add of the placeholder first (pasting instead of typing means it was never wanted,
                // as in DeleteRows): everything below reads list.Count, which has to be the count after this.
                if (placeholderWasTouched && list is ICancelAddNew cancelAddNew)
                {
                    cancelAddNew.CancelNew(list.Count - 1);
                }

                // All indexes are real rows (the placeholder was left out above); nothing selected at all (append at the
                // end) is the one case the sentinel of minRowIndex covers.
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

        // One copied cell pasted onto a selection of more cells fills all of them with that value (as a spreadsheet does).
        // More than one copied cell never fills: it is anchored at the top left and extends from there (as in Excel). True
        // when the paste was handled this way.
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

        // Where a paste anchors: the top left of the selection, not CurrentCell (that is the cell clicked or reached last,
        // e.g. the far corner of a drag, and put the paste in the wrong place). The delete column is never an anchor.
        // PlaceholderWasTouched: merely selecting the placeholder makes WinForms call IBindingList.AddNew() (see
        // InsertBlankRow). The placeholder is not in SelectedRealRowIndexes, but its pending empty item has to be
        // cancelled, or it stays as a stray row.
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

                // The column anchor counts also when the cell is in the placeholder row: a paste that starts in column 2 of
                // the placeholder starts in column 2 of the real row.
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
            // Looked up once per item type and property name, not per cell (the same for a list of one item type; the item
            // type is part of the key for lists that mix them).
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

        // Shared by WritePastedValues and FillCellsWithValue. ReadOnly and unbound columns, and values that do not convert
        // ("abc" into a number), are skipped: best effort, one bad cell does not stop the paste.
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
