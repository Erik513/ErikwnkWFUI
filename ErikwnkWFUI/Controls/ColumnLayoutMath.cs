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

        // Where a column dropped at x would be inserted, expressed as
        // "insert before this display index" - the boundary flips at each
        // column's midpoint rather than its edges, so the insertion line
        // snaps to whichever side of the hovered column the cursor is
        // actually closer to. x and every column's own width must already
        // be in the same coordinate space - DataGridView additionally
        // folds in HorizontalScrollingOffset before calling this (see its
        // own caller), since it can scroll its columns independently of
        // where they're drawn; ListView's header can't scroll independently
        // like that, so it passes x as-is. columnsInDisplayOrder is
        // expected already ordered (see OrderByDisplayIndex above).
        public static int GetDropInsertionIndex<T>(List<T> columnsInDisplayOrder, Func<T, int> widthOf, int x)
        {
            int cumulativeWidth = 0;
            for (int displayIndex = 0; displayIndex < columnsInDisplayOrder.Count; displayIndex++)
            {
                int columnWidth = widthOf(columnsInDisplayOrder[displayIndex]);
                if (x < cumulativeWidth + columnWidth / 2)
                {
                    return displayIndex;
                }

                cumulativeWidth += columnWidth;
            }

            return columnsInDisplayOrder.Count;
        }

        // insertBeforeDisplayIndex is expressed in the ORIGINAL display
        // order (before the dragged column is removed from its old slot) -
        // the standard "move to before index P" -> "target index"
        // adjustment (subtract one if P is past the column's own current
        // position) is needed because DisplayIndex's setter moves the
        // column to an absolute position, and removing it from its old
        // slot first would shift everything after that slot left by one.
        // Returns null for a no-op move (already exactly there) - the
        // caller's own DisplayIndex assignment (and whatever it does
        // afterward) is a real property set with real side effects on
        // both controls, worth skipping entirely rather than reassigning
        // to the same value.
        public static int? GetMoveTargetDisplayIndex(int originalDisplayIndex, int insertBeforeDisplayIndex)
        {
            int targetDisplayIndex = insertBeforeDisplayIndex > originalDisplayIndex
                ? insertBeforeDisplayIndex - 1
                : insertBeforeDisplayIndex;

            return targetDisplayIndex == originalDisplayIndex ? (int?)null : targetDisplayIndex;
        }
    }
}
