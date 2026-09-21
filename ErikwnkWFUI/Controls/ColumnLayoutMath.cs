using System;
using System.Collections.Generic;

namespace ErikwnkWFUI.Controls
{
    // Shared column-layout math for ReadOnlyDataGridView and ListView's own
    // hand-rolled column reordering/resizing - both controls lay out their
    // columns the same way (left-to-right by DisplayIndex, cumulative
    // widths), but have no common column type to share an instance method
    // through (DataGridViewColumn and ColumnHeader are unrelated types, each
    // sealed into its own framework control). Generic over the column type
    // instead, taking a plain accessor delegate for whichever property is
    // needed - each caller already has its own columns as a
    // List&lt;T&gt; (built once per call from that control's own non-generic
    // column collection), so this only ever operates on that, never the
    // collection itself.
    internal static class ColumnLayoutMath
    {
        // Mirrors both controls' own former GetColumnsInDisplayOrder -
        // ordered.Sort's comparer capture of displayIndexOf is exactly as
        // cheap as the hand-written one each had before this existed.
        public static List<T> OrderByDisplayIndex<T>(List<T> columns, Func<T, int> displayIndexOf)
        {
            columns.Sort((first, second) => displayIndexOf(first).CompareTo(displayIndexOf(second)));
            return columns;
        }
    }
}
