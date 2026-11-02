using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The three-state click on a column header, the same in the list view and the grid:
    // ascending, then descending, then back to the original order. Another column starts
    // fresh at ascending.
    internal static class SortCycle
    {
        internal static void Advance(int columnIndex, ref int sortedColumnIndex, ref SortOrder sortOrder)
        {
            if (columnIndex != sortedColumnIndex)
            {
                sortedColumnIndex = columnIndex;
                sortOrder = SortOrder.Ascending;
            }
            else if (sortOrder == SortOrder.Ascending)
            {
                sortOrder = SortOrder.Descending;
            }
            else if (sortOrder == SortOrder.Descending)
            {
                sortOrder = SortOrder.None;
            }
            else
            {
                sortOrder = SortOrder.Ascending;
            }
        }
    }
}
