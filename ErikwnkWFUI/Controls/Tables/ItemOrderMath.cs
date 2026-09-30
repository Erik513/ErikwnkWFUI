using System;
using System.Collections.Generic;

namespace ErikwnkWFUI.Controls
{
    // Shared row/item-ordering math for ReadOnlyDataGridView and ListView's
    // own column-header sort cycling - both restore "original" order the
    // same way once a user cycles all the way back past Descending, just
    // over different item types (a bound object vs a ListViewItem) with no
    // common base to share an instance method through.
    internal static class ItemOrderMath
    {
        // Restores candidateItems to the order they were in at the last
        // snapshot (originalOrder) - an item added since that snapshot was
        // taken (not found in it at all) sorts after every item that WAS
        // present, rather than colliding at index -1 the way IndexOf's own
        // "not found" result would. List.Sort isn't a stable sort, but
        // every item added after the snapshot shares that same fallback
        // index (originalOrder.Count), so this is no less stable than
        // either control's own former hand-written version of this exact
        // same code.
        public static List<TItem> BuildOriginalOrder<TItem>(IEnumerable<TItem> candidateItems, List<TItem> originalOrder)
        {
            List<KeyValuePair<TItem, int>> itemsWithIndex = new List<KeyValuePair<TItem, int>>();

            foreach (TItem item in candidateItems)
            {
                int index = originalOrder.IndexOf(item);
                itemsWithIndex.Add(new KeyValuePair<TItem, int>(item, index >= 0 ? index : originalOrder.Count));
            }

            itemsWithIndex.Sort((first, second) => first.Value.CompareTo(second.Value));

            List<TItem> ordered = new List<TItem>(itemsWithIndex.Count);
            foreach (KeyValuePair<TItem, int> pair in itemsWithIndex)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }
    }
}
