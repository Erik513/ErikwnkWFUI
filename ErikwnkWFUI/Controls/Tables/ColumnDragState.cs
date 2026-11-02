using System;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Where the drag of a column title stands, for <c>ListView</c> and <c>ReadOnlyDataGridView</c> (both move their columns the same
    /// way): which column is dragged and before which place it would land. One object instead of three loose fields in each control.
    /// </summary>
    internal sealed class ColumnDragState
    {
        /// <summary>Whether a column is being dragged.</summary>
        public bool IsDragging { get; set; }

        /// <summary>The column that is dragged; -1 while none is.</summary>
        public int ColumnIndex { get; set; } = -1;

        /// <summary>The display index before which the column would be dropped; -1 while there is no such place.</summary>
        public int InsertBeforeDisplayIndex { get; set; } = -1;

        /// <summary>Whether the drag has a column and a place to drop it.</summary>
        public bool HasTarget
        {
            get { return ColumnIndex >= 0 && InsertBeforeDisplayIndex >= 0; }
        }

        /// <summary>
        /// Runs a drag: <paramref name="dragDrop"/> returns when it is over, however it ended (a drop, a cancel with Escape, a drop
        /// where nothing takes it), and then everything is reset and <paramref name="afterwards"/> is called (to paint without the
        /// insertion line). The drop itself only moves the column; it does not reset this.
        /// </summary>
        public void Run(int columnIndex, Action dragDrop, Action afterwards)
        {
            ColumnIndex = columnIndex;
            IsDragging = true;
            InsertBeforeDisplayIndex = -1;

            try
            {
                dragDrop();
            }
            finally
            {
                IsDragging = false;
                ColumnIndex = -1;
                InsertBeforeDisplayIndex = -1;
                afterwards();
            }
        }

        /// <summary>Sets the place the column would land; returns whether it is another one than before (the picture has to change).</summary>
        public bool MoveInsertTo(int displayIndex)
        {
            if (displayIndex == InsertBeforeDisplayIndex)
                return false;

            InsertBeforeDisplayIndex = displayIndex;

            return true;
        }
    }
}
