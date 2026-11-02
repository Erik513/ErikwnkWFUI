using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The numbered first column (ShowEnumeration).
    public partial class ReadOnlyDataGridView
    {

        /// <summary>
        /// Whether <paramref name="columnIndex"/> is the optional
        /// enumeration column (see <see cref="ShowEnumeration"/>) - exposed
        /// so <see cref="DataGridView"/> can exclude it from copy/paste/the
        /// right-click menu the same way it already excludes its own
        /// delete-row column, without the backing field itself needing to
        /// be any more visible than that.
        /// </summary>
        protected bool IsEnumerationColumn(int columnIndex)
        {
            return _enumerationColumn != null && columnIndex == _enumerationColumn.Index;
        }


        // The displayed number for each row is always its CURRENT position -
        // recomputed here from scratch rather than tracked incrementally,
        // since every operation that could change row order or count (a
        // fresh bind, a sort, DataGridView's own paste/insert/delete) already
        // funnels through either this property's own setter (toggling the
        // column on) or OnDataBindingComplete below exactly once. IsNewRow
        // (the "type here to add a row" placeholder, only ever present on a
        // DataGridView with AllowUserToAddRows enabled) is left blank, same
        // as the delete column shows nothing there - nothing to number yet.
        private void RenumberEnumeration()
        {
            if (_enumerationColumn == null)
            {
                return;
            }

            int columnIndex = _enumerationColumn.Index;

            int realRowCount = 0;
            foreach (DataGridViewRow row in Rows)
            {
                if (!row.IsNewRow)
                {
                    realRowCount++;
                }
            }

            // Counts down from the row count instead of up from 1 while
            // this column is itself the active DESCENDING sort column -
            // otherwise a click that reverses the row order (confirmed live
            // that it does - see CycleSort/ReorderDataSource) had no visible
            // effect on the numbers themselves, since renumbering to match
            // whatever the new top-to-bottom order is always produced the
            // same 1..N sequence regardless of which direction the rows
            // actually got reordered in. Every other case (ascending, no
            // active sort, or a DIFFERENT column being sorted) still just
            // counts up - this column's job there is "which visual row is
            // this", unrelated to what's being sorted.
            bool countDown = columnIndex == _sortedColumnIndex && _sortOrder == SortOrder.Descending;
            int number = countDown ? realRowCount : 1;

            foreach (DataGridViewRow row in Rows)
            {
                if (row.IsNewRow)
                {
                    row.Cells[columnIndex].Value = null;
                    continue;
                }

                row.Cells[columnIndex].Value = number;
                number += countDown ? -1 : 1;
            }

            _enumerationColumn.Width = GetEnumerationColumnWidth(Rows.Count);
        }


        // Rows.Count, not the real (possibly one-smaller, once the
        // placeholder is excluded) count of numbered rows - the difference
        // is at most one digit's worth of slack, and this avoids needing to
        // special-case AllowUserToAddRows here just to shave that off.
        private int GetEnumerationColumnWidth(int rowCount)
        {
            int digitCount = Math.Max(1, rowCount.ToString().Length);
            string widest = new string('9', digitCount);

            using (Font font = new Font(Font, FontStyle.Bold))
            {
                Size textSize = TextRenderer.MeasureText(widest, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                return textSize.Width + EnumerationColumnPadding;
            }
        }


        // "#" normally, but blank while this column is itself the active
        // sort column - ColumnHeaderPainting.DrawSortGlyph already draws
        // the ascending/descending arrow there regardless (the sort itself
        // creates that on its own, independent of this), and showing "#"
        // right next to a direction-flipping arrow read as if the "#"
        // itself was changing. Reverts back to "#" on the third click
        // (SortOrder.None, back to original bind order) same as any other
        // column losing the glyph.
        private void UpdateEnumerationHeaderText()
        {
            if (_enumerationColumn == null)
            {
                return;
            }

            bool isActiveSortColumn = _enumerationColumn.Index == _sortedColumnIndex && _sortOrder != SortOrder.None;
            _enumerationColumn.HeaderText = isActiveSortColumn ? string.Empty : UIStrings.Get("DataGridView.EnumerationHeader");
        }
    }
}
