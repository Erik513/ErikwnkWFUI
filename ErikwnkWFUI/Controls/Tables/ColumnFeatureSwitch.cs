using System.Collections.Generic;

namespace ErikwnkWFUI.Controls
{
    // Backs a "can this column do X" feature - reordering, resizing - that
    // both ReadOnlyDataGridView and ListView expose the exact same way: a
    // master switch (AllowColumnReordering/AllowColumnResizing) that governs
    // every column by default, plus a per-column exception set
    // (SetColumnReorderable/SetColumnResizable) for pinning specific ones
    // regardless of the master switch's own value. Each control used to
    // hand-write this pair (a bool field plus a HashSet&lt;int&gt;) twice
    // over - once for reordering, once for resizing.
    internal sealed class ColumnFeatureSwitch
    {
        private readonly HashSet<int> _disabledColumns = new HashSet<int>();

        public bool AllowedByDefault { get; set; } = true;

        public void SetAllowed(int columnIndex, bool allowed)
        {
            if (allowed)
            {
                _disabledColumns.Remove(columnIndex);
            }
            else
            {
                _disabledColumns.Add(columnIndex);
            }
        }

        public bool IsAllowed(int columnIndex)
        {
            return AllowedByDefault && !_disabledColumns.Contains(columnIndex);
        }
    }
}
