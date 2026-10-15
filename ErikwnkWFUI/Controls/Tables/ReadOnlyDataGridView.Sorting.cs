using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // Sorting by a click on a header: the three states, the comparers and the order of the cells.
    public partial class ReadOnlyDataGridView
    {

        /// <summary>
        /// Registers a custom comparer for a specific column, used by
        /// column-header sorting (see <see cref="SortingEnabled"/>/
        /// <see cref="CycleSort"/>) instead of the default comparison
        /// (<see cref="IComparable"/>, falling back to case-insensitive
        /// string comparison) - useful for a column whose cell values
        /// don't sort correctly as plain strings/IComparables on their
        /// own, e.g. dates stored in a non-lexicographic text format, or
        /// numbers stored as strings. The comparer receives the two
        /// cells' raw <see cref="DataGridViewCell.Value"/> objects and is
        /// expected to compare them in plain ascending order - this
        /// control still applies ascending/descending itself, the same
        /// way it already does for the default comparison, so a
        /// registered comparer never needs to know which direction is
        /// currently active. Pass <c>null</c> as <paramref name="comparer"/>
        /// to remove one and revert that column to the default. Set any
        /// time after the column exists; not tied to binding order the
        /// way <see cref="SortingEnabled"/> is.
        /// </summary>
        public void SetSortComparer(DataGridViewColumn column, IComparer comparer)
        {
            SortComparerRegistry.Set(_columnSortComparers, column, comparer);
        }


        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnColumnHeaderMouseClick(e);

            // base's own header-click handling can move CurrentCell to the
            // column's first row as a side effect - SortMode.Programmatic
            // only suppresses its automatic sort, not that. Clearing again
            // here (in addition to HandleMouseDown's clear on the way in)
            // is what stops that from ever reaching the screen.
            ClearSelection();
            CurrentCell = null;

            // Regression test: resizing (or even just pressing down right
            // on top of) a column border still counts as a "click" on
            // whichever header cell that border belongs to as far as
            // DataGridView's own click detection is concerned - without
            // this, finishing a resize drag by releasing the mouse also
            // silently cycled that column's sort, since the resize itself
            // is hand-rolled (OnCellMouseDown/OnCellMouseMove) and never
            // told DataGridView's own click machinery to stay out of it.
            if (_suppressNextHeaderClickSort)
            {
                _suppressNextHeaderClickSort = false;
                return;
            }

            bool isSortableColumn =
                e.ColumnIndex >= 0 && Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable;

            if (SortingEnabled && e.Button == MouseButtons.Left && isSortableColumn)
            {
                CycleSort(e.ColumnIndex);
            }
        }


        /// <summary>
        /// Three-state header click: ascending, then descending, then back
        /// to the order rows were in when last bound - clicking a different
        /// column starts that column fresh at ascending.
        /// </summary>
        private void CycleSort(int columnIndex)
        {
            SortCycle.Advance(columnIndex, ref _sortedColumnIndex, ref _sortOrder);

            foreach (DataGridViewColumn column in Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ReorderDataSource(BuildOriginalOrder());
            }
            else
            {
                // The native SortGlyphDirection is NOT set: it paints a glyph of its own on top of
                // ColumnHeaderPainting.DrawSortGlyph's (two triangles overlapping into one "bowtie"
                // blob), on every column, not just the enumeration column with its empty HeaderText.
                // The arrow on screen is drawn from _sortedColumnIndex/_sortOrder alone.
                ReorderDataSource(BuildSortedOrder(columnIndex, _sortOrder));
            }

            UpdateEnumerationHeaderText();

            // Redundant with Invalidate() already happening as a side
            // effect of ClearSelection()/CurrentCell = null below (via
            // OnSelectionChanged) in the case where selection actually
            // changes - but a sort that doesn't move CurrentCell off of
            // wherever it already was (e.g. nothing was selected to begin
            // with) wouldn't otherwise repaint the header row at all,
            // leaving the custom-drawn glyph below stale. -1 addresses the
            // header row, same as everywhere else in this file that reads
            // RowIndex against it.
            InvalidateCell(columnIndex, -1);

            // Whole-header repaint, forced synchronous rather than left for
            // the next WM_PAINT - the enumeration column's HeaderText just
            // changed (see UpdateEnumerationHeaderText) and every header
            // cell's own SortGlyphDirection/border can change too, so a
            // single-cell InvalidateCell isn't always enough to guarantee
            // the whole row is actually repainted before this method
            // returns.
            Invalidate();
            Update();

            ClearSelection();
            CurrentCell = null;
        }


        private List<object> BuildSortedOrder(int columnIndex, SortOrder sortOrder)
        {
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            List<KeyValuePair<object, object>> itemsWithValues = new List<KeyValuePair<object, object>>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem != null)
                {
                    itemsWithValues.Add(new KeyValuePair<object, object>(
                        row.DataBoundItem,
                        row.Cells[columnIndex].Value));
                }
            }

            // A custom comparer (see SetSortComparer) always compares in
            // plain ascending order - direction is applied the same way
            // regardless of whether the default comparison or a
            // registered one is doing the actual comparing, so a
            // consumer's comparer never has to care which one is active.
            _columnSortComparers.TryGetValue(Columns[columnIndex], out IComparer customComparer);

            itemsWithValues.Sort((a, b) => customComparer != null
                ? customComparer.Compare(a.Value, b.Value) * direction
                : CompareCellValues(a.Value, b.Value, direction));

            List<object> ordered = new List<object>(itemsWithValues.Count);

            foreach (KeyValuePair<object, object> pair in itemsWithValues)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }


        // Rows added after the last bind/reset (e.g. a new row the user just
        // typed into) sort after every row that was present at that
        // snapshot, rather than colliding at index -1 (see
        // ItemOrderMath.BuildOriginalOrder). Mirrors ListView's own
        // BuildOriginalOrder.
        private List<object> BuildOriginalOrder()
        {
            List<object> dataBoundItems = new List<object>();
            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem != null)
                {
                    dataBoundItems.Add(row.DataBoundItem);
                }
            }

            return ItemOrderMath.BuildOriginalOrder(dataBoundItems, _originalOrder);
        }


        private static int CompareCellValues(object valueX, object valueY, int direction)
        {
            if (valueX == null && valueY == null)
            {
                return 0;
            }

            if (valueX == null)
            {
                return -1 * direction;
            }

            if (valueY == null)
            {
                return 1 * direction;
            }

            if (valueX is IComparable comparable)
            {
                try
                {
                    return comparable.CompareTo(valueY) * direction;
                }
                catch (ArgumentException)
                {
                    // Falls through to the string comparison below when the
                    // two values aren't directly comparable to each other
                    // (e.g. different underlying cell types).
                }
            }

            return string.Compare(
                Convert.ToString(valueX),
                Convert.ToString(valueY),
                StringComparison.CurrentCultureIgnoreCase) * direction;
        }
    }
}
