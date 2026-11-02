using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // Sorting by a click on a header: the three states, the comparers and the order of the items.
    public partial class ListView : System.Windows.Forms.ListView
    {

        /// <summary>
        /// Whether clicking a column header cycles it through ascending,
        /// descending, and original item order - mirrors
        /// <see cref="Controls.ReadOnlyDataGridView.SortingEnabled"/>. Defaults
        /// to true. Turn a single column off instead via
        /// <see cref="SetColumnSortable"/> if only that one shouldn't sort.
        /// </summary>
        [DefaultValue(true)]
        public bool SortingEnabled { get; set; } = true;

        /// <summary>
        /// Configurable per column, independent of <see cref="SortingEnabled"/> -
        /// a column can be excluded from sorting (e.g. one showing icons or
        /// actions rather than comparable data) while the rest stay sortable.
        /// </summary>
        public void SetColumnSortable(int columnIndex, bool sortable)
        {
            if (sortable)
            {
                _nonSortableColumns.Remove(columnIndex);
            }
            else
            {
                _nonSortableColumns.Add(columnIndex);
            }
        }

        /// <summary>Whether a click on the header of this column sorts the list by it (see <see cref="SortingEnabled"/> and <see cref="SetColumnSortable"/>).</summary>
        public bool IsColumnSortable(int columnIndex)
        {
            return SortingEnabled && !_nonSortableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Registers a custom comparer for a column's header-click sort,
        /// replacing the default (the sorted column's own text, compared
        /// the same "natural" way Windows Explorer's own file listing does -
        /// see CompareItemText/StrCmpLogicalW - so e.g. "Row 2" sorts before
        /// "Row 10"). The comparer receives the two
        /// <see cref="ListViewItem"/>s being compared, so it can read other
        /// SubItems or <see cref="ListViewItem.Tag"/> instead of just the
        /// sorted column's text. Compare in plain ascending order - this
        /// control applies descending itself. Pass <c>null</c> to remove a
        /// comparer and revert that column to the default.
        /// </summary>
        public void SetSortComparer(ColumnHeader column, IComparer comparer)
        {
            SortComparerRegistry.Set(_columnSortComparers, column, comparer);
        }

        /// <inheritdoc/>
        protected override void OnColumnClick(ColumnClickEventArgs e)
        {
            base.OnColumnClick(e);

            if (_suppressNextColumnClickSort)
            {
                _suppressNextColumnClickSort = false;
                return;
            }

            if (!IsColumnSortable(e.Column))
            {
                return;
            }

            CycleSort(e.Column);
        }

        // Three states on a header click: ascending, descending, and back to the order the items were in when the first
        // sort began. Another column starts at ascending.
        private void CycleSort(int columnIndex)
        {
            if (_sortedColumnIndex < 0)
            {
                // The order before the first sort is taken now (there is no data source event to take it from).
                _originalOrder = new List<ListViewItem>(Items.Cast<ListViewItem>());
            }

            SortCycle.Advance(columnIndex, ref _sortedColumnIndex, ref _sortOrder);

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ApplyItemOrder(BuildOriginalOrder());
            }
            else
            {
                ApplyItemOrder(BuildSortedOrder(columnIndex, _sortOrder));
            }

            // The rows moved from under the selection, which would now point at other rows: it is cleared (as in the grid).
            foreach (ListViewItem item in Items)
            {
                item.Selected = false;
            }

            InvalidateHeader();
        }

        // All items out and in again in the new order, in one update: painted once, not once per item.
        private void ApplyItemOrder(List<ListViewItem> desiredOrder)
        {
            BeginUpdate();
            try
            {
                Items.Clear();
                Items.AddRange(desiredOrder.ToArray());
            }
            finally
            {
                EndUpdate();
            }
        }

        // Sorted by the text of the column, or by the comparer set for it (SetSortComparer).
        private List<ListViewItem> BuildSortedOrder(int columnIndex, SortOrder sortOrder)
        {
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            List<ListViewItem> items = new List<ListViewItem>(Items.Cast<ListViewItem>());

            _columnSortComparers.TryGetValue(Columns[columnIndex], out IComparer customComparer);

            items.Sort((a, b) => customComparer != null
                ? customComparer.Compare(a, b) * direction
                : CompareItemText(GetSubItemText(a, columnIndex), GetSubItemText(b, columnIndex), direction));

            return items;
        }

        // Items that were added since the first sort come last (ItemOrderMath).
        private List<ListViewItem> BuildOriginalOrder()
        {
            return ItemOrderMath.BuildOriginalOrder(Items.Cast<ListViewItem>(), _originalOrder);
        }

        private static string GetSubItemText(ListViewItem item, int columnIndex)
        {
            return columnIndex < item.SubItems.Count ? item.SubItems[columnIndex].Text : string.Empty;
        }

        // "Row 2" before "Row 10": numbers inside the text compare as numbers (StrCmpLogicalW, as Explorer sorts files).
        private static int CompareItemText(string textX, string textY, int direction)
        {
            return StrCmpLogicalW(textX ?? string.Empty, textY ?? string.Empty) * direction;
        }
    }
}
