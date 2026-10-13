using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Native;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The columns: resizing and reordering (the native drag is replaced), the least width, and the header handle.
    public partial class ListView : System.Windows.Forms.ListView
    {

        /// <summary>No column can be resized narrower than this.</summary>
        public int MinimumColumnWidth
        {
            get { return _minimumColumnWidth; }
            set { _minimumColumnWidth = Math.Max(1, value); }
        }

        /// <summary>
        /// Master switch for whether any column can be resized at all.
        /// <see cref="SetColumnResizable"/> still applies per column among
        /// whichever this allows; false overrides everything. Defaults to
        /// true.
        /// </summary>
        public bool AllowColumnResizing
        {
            get => _columnResizing.AllowedByDefault;
            set
            {
                _columnResizing.AllowedByDefault = value;
                ApplyHeaderSizingStyle();
            }
        }

        /// <summary>
        /// Master switch for whether any column can be dragged to reorder
        /// at all. <see cref="SetColumnReorderable"/> still applies per
        /// column among whichever this allows; false overrides everything.
        /// Defaults to true.
        /// </summary>
        public bool AllowColumnReordering
        {
            get => _columnReordering.AllowedByDefault;
            set => _columnReordering.AllowedByDefault = value;
        }

        /// <summary>
        /// Per-column override, independent of <see cref="MinimumColumnWidth"/> -
        /// locks a column at its current width (e.g. a narrow status
        /// column) while others stay resizable.
        /// </summary>
        public void SetColumnResizable(int columnIndex, bool resizable)
        {
            _columnResizing.SetAllowed(columnIndex, resizable);
        }

        public bool IsColumnResizable(int columnIndex)
        {
            return _columnResizing.IsAllowed(columnIndex);
        }

        /// <summary>
        /// Per-column override, independent of <see cref="AllowColumnReordering"/> -
        /// pins a column in place (e.g. a leading "Time" column) while the
        /// rest can still be dragged into a new order.
        /// </summary>
        public void SetColumnReorderable(int columnIndex, bool reorderable)
        {
            _columnReordering.SetAllowed(columnIndex, reorderable);
        }

        public bool IsColumnReorderable(int columnIndex)
        {
            return _columnReordering.IsAllowed(columnIndex);
        }

        /// <summary>
        /// Sizes every column to fit its current content and header text.
        /// Call this again after rebuilding the rows, since content
        /// driving the "right" width may have changed.
        /// </summary>
        public void AutoFitColumnsToContent()
        {
            if (!IsHandleCreated || Columns.Count == 0)
            {
                return;
            }

            for (var index = 0; index < Columns.Count; index++)
            {
                AutoResizeColumn(index, ColumnHeaderAutoResizeStyle.ColumnContent);
                var minimumWidth = GetEffectiveMinimumWidth(index);
                if (Columns[index].Width < minimumWidth)
                {
                    Columns[index].Width = minimumWidth;
                }
            }
        }

        private bool TryGetHeaderBounds(int columnIndex, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (!IsHandleCreated || columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return false;
            }

            var header = HeaderInputSubclass.GetHeaderHandle(Handle);
            var rectangle = new NativeRect();
            if (header == System.IntPtr.Zero ||
                SendMessage(header, HDM_GETITEMRECT, (System.IntPtr)columnIndex, ref rectangle) == System.IntPtr.Zero)
            {
                return false;
            }

            MapWindowPoints(header, Handle, ref rectangle, 2);
            bounds = Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            return true;
        }

        // Where no column can be resized, the header is told so (HDS_NOSIZING): it then never shows its resize
        // cursor at a divider, which no answer to WM_SETCURSOR could keep from flickering in between. A list where
        // only some columns are fixed keeps the header as it is; the cursor is corrected for those (see
        // HeaderInputSubclass).
        private void ApplyHeaderSizingStyle()
        {
            if (!IsHandleCreated)
                return;

            System.IntPtr header = HeaderInputSubclass.GetHeaderHandle(Handle);

            if (header == System.IntPtr.Zero)
                return;

            int style = GetWindowLong(header, GWL_STYLE);
            int wanted = AllowColumnResizing ? style & ~HDS_NOSIZING : style | HDS_NOSIZING;

            if (wanted != style)
                SetWindowLong(header, GWL_STYLE, wanted);
        }

        private void OnColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            _headerInputSubclass?.CancelPendingReorder();
            _suppressNextColumnClickSort = true;

            if (!IsColumnResizable(e.ColumnIndex))
            {
                e.NewWidth = Columns[e.ColumnIndex].Width;
                e.Cancel = true;
                return;
            }

            var minimumWidth = GetEffectiveMinimumWidth(e.ColumnIndex);
            if (e.NewWidth < minimumWidth)
            {
                e.NewWidth = minimumWidth;
                e.Cancel = true;
                return;
            }

            // (No upper limit: a column wider than the list makes a horizontal scroll bar, as in the standard list view.)
        }

        // MinimumColumnWidth is the caller's floor, but a column is never narrower than its caption needs: the larger wins.
        private int GetEffectiveMinimumWidth(int columnIndex)
        {
            return Math.Max(_minimumColumnWidth, MeasureHeaderTextWidth(columnIndex));
        }

        private int MeasureHeaderTextWidth(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return 0;
            }

            // (The font the caption is drawn in: HeaderFont.)
            var textSize = TextRenderer.MeasureText(
                Columns[columnIndex].Text,
                HeaderFont,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.Left | TextFormatFlags.NoPadding);

            // Matches the padding OnDrawColumnHeader draws the caption with.
            return textSize.Width + Px(HeaderTextLeft96 + HeaderTextRight96);
        }

        private List<ColumnHeader> GetColumnsInDisplayOrder()
        {
            var columns = new List<ColumnHeader>();
            foreach (ColumnHeader column in Columns)
            {
                columns.Add(column);
            }

            return ColumnLayoutMath.OrderByDisplayIndex(columns, column => column.DisplayIndex);
        }

        // Called by HeaderInputSubclass when a press on a header has moved far enough: DoDragDrop, OnDragOver and OnDragDrop
        // do the rest (as for the items of the ListBox).
        private void BeginColumnDragDrop(int columnIndex)
        {
            _dragColumnIndex = columnIndex;
            _isDraggingColumn = true;
            _dragInsertBeforeDisplayIndex = -1;

            // The drag ends with a mouse-up on a header, which would count as a click and sort: that click is ignored.
            _suppressNextColumnClickSort = true;

            try
            {
                DoDragDrop(columnIndex, DragDropEffects.Move);
            }
            finally
            {
                // Every way the drag can end, a cancel (Escape) and a drop without OnDragDrop too; OnDragDrop only moves.
                _isDraggingColumn = false;
                _dragColumnIndex = -1;
                _dragInsertBeforeDisplayIndex = -1;

                InvalidateHeader();
            }
        }

        // Invalidate() reaches the client area only; the header is a window of its own and has to be told too.
        private void InvalidateHeader()
        {
            Invalidate();
            _headerInputSubclass?.InvalidateHeaderNow();
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            var point = PointToClient(new Point(drgevent.X, drgevent.Y));
            var insertBefore = GetColumnDropInsertionIndex(point.X);

            drgevent.Effect = DragDropEffects.Move;

            if (insertBefore != _dragInsertBeforeDisplayIndex)
            {
                _dragInsertBeforeDisplayIndex = insertBefore;
                InvalidateHeader();
            }

            base.OnDragOver(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (_dragColumnIndex >= 0 && _dragInsertBeforeDisplayIndex >= 0)
            {
                MoveColumnToDisplayIndex(_dragColumnIndex, _dragInsertBeforeDisplayIndex);
            }

            base.OnDragDrop(drgevent);
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            gfbevent.UseDefaultCursors = false;
            Cursor.Current = Cursors.SizeWE;

            base.OnGiveFeedback(gfbevent);
        }

        private void MoveColumnToDisplayIndex(int columnIndex, int insertBeforeDisplayIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return;
            }

            var column = Columns[columnIndex];
            int? targetDisplayIndex = ColumnLayoutMath.GetMoveTargetDisplayIndex(column.DisplayIndex, insertBeforeDisplayIndex);
            if (targetDisplayIndex == null)
            {
                return;
            }

            column.DisplayIndex = targetDisplayIndex.Value;
        }

        // Only a press clearly inside a column starts the reorder drag; a press at a divider is a resize.
        private bool IsNearColumnBorder(int x)
        {
            return ColumnLayoutMath.TryGetColumnAtBorder(GetColumnsInDisplayOrder(), c => c.Width, x, Px(ResizeGripWidth96), out _);
        }

        // comctl32 shows the resize cursor at every divider, whatever SetColumnResizable says (the drag is blocked in
        // TryBeginResize and OnColumnWidthChanging, but the cursor would promise it). A divider resizes the column to its
        // left, so that column decides.
        private bool IsNearNonResizableColumnBorder(int x)
        {
            return ColumnLayoutMath.TryGetColumnAtBorder(GetColumnsInDisplayOrder(), c => c.Width, x, Px(ResizeGripWidth96), out ColumnHeader column) &&
                !IsColumnResizable(column.Index);
        }

        // (The coordinate space of x: see ColumnLayoutMath.GetDropInsertionIndex.)
        private int GetColumnDropInsertionIndex(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            return ColumnLayoutMath.GetDropInsertionIndex(orderedColumns, column => column.Width, x);
        }

        // The display index (the position among the columns as shown) of the column at x.
        private int GetColumnIndexAtX(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            var cumulativeWidth = 0;
            for (var displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
            {
                cumulativeWidth += orderedColumns[displayIndex].Width;
                if (x < cumulativeWidth)
                {
                    return displayIndex;
                }
            }

            return Math.Max(0, orderedColumns.Count - 1);
        }
    }
}
