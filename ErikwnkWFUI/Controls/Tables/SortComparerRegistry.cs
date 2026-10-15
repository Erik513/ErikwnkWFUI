using System;
using System.Collections;
using System.Collections.Generic;

namespace ErikwnkWFUI.Controls
{
    /// <summary>The comparers a <c>ListView</c> and a <c>ReadOnlyDataGridView</c> have for their columns (the same rule for both).</summary>
    internal static class SortComparerRegistry
    {
        /// <summary>Registers <paramref name="comparer"/> for <paramref name="column"/>; <c>null</c> takes the comparer of the column away.</summary>
        public static void Set<TColumn>(Dictionary<TColumn, IComparer> comparers, TColumn column, IComparer comparer) where TColumn : class
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (comparer == null)
                comparers.Remove(column);
            else
                comparers[column] = comparer;
        }
    }
}
