using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // Moving and resizing the columns by hand (the native drag and resize are replaced), the mouse and drag handling it needs.
    public partial class ReadOnlyDataGridView
    {

        /// <summary>
        /// Configurable per column, independent of
        /// <see cref="AllowColumnReordering"/>: a column can be pinned in
        /// place (e.g. <see cref="DataGridView"/>'s own delete-row column,
        /// always pinned rightmost) while the rest can still be freely
        /// dragged into a new order.
        /// </summary>
        public void SetColumnReorderable(int columnIndex, bool reorderable)
        {
            _columnReordering.SetAllowed(columnIndex, reorderable);
        }


        /// <summary>Whether the user can drag this column to another place (see <see cref="AllowColumnReordering"/> and <see cref="SetColumnReorderable"/>).</summary>
        public bool IsColumnReorderable(int columnIndex)
        {
            return _columnReordering.IsAllowed(columnIndex);
        }


        /// <summary>
        /// Configurable per column, independent of
        /// <see cref="AllowColumnResizing"/>: a column can be locked at its
        /// current width entirely (e.g. <see cref="DataGridView"/>'s own
        /// delete-row column) while the rest can still be freely resized.
        /// Mirrors <see cref="ListView.SetColumnResizable"/>.
        /// </summary>
        public void SetColumnResizable(int columnIndex, bool resizable)
        {
            _columnResizing.SetAllowed(columnIndex, resizable);
        }


        /// <summary>Mirrors <see cref="ListView.IsColumnResizable"/> - see <see cref="AllowColumnResizing"/>/<see cref="SetColumnResizable"/>.</summary>
        public bool IsColumnResizable(int columnIndex)
        {
            return _columnResizing.IsAllowed(columnIndex);
        }


        /// <inheritdoc/>
        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            // Captured BEFORE base.OnCellMouseDown below, not after -
            // regression fix: this control's SelectionMode is CellSelect,
            // whose own native mouse-down handling (run by that base call)
            // already selects the single clicked cell as its own default
            // behavior. Checking IsRowSelected afterward, as this used to,
            // meant the row it just clicked always already had ONE selected
            // cell (the one just clicked) by the time it was checked -
            // silently skipping SelectRow below for EVERY previously-
            // unselected row, every time, and leaving just that one cell
            // selected instead of the whole row the context menu's own
            // row-scoped actions actually need.
            bool rowWasSelectedBeforeThisClick = e.RowIndex >= 0 && IsRowSelected(e.RowIndex);

            // Same reason: moving CurrentCell into the placeholder (which
            // that base call does for any mouse button) makes WinForms call
            // IBindingList.AddNew() on its own, after which NewRowIndex
            // advances past the row that was just clicked - checked
            // afterward, this would wrongly report "not the placeholder"
            // and defeat DataGridView's own InsertBlankRow double-insert
            // guard.
            bool rowWasPlaceholderBeforeThisClick = IsPlaceholderRowIndex(e.RowIndex);

            base.OnCellMouseDown(e);

            if (e.Button == MouseButtons.Right)
            {
                CaptureRightClickTarget(e, rowWasSelectedBeforeThisClick, rowWasPlaceholderBeforeThisClick);
            }

            if (e.RowIndex != -1)
            {
                return;
            }

            // The MouseDown event (see HandleMouseDown) fires from
            // Control.OnMouseDown, which DataGridView's own OnMouseDown
            // override calls partway through its own processing - moving
            // CurrentCell to this column's first row, as a side effect of
            // pressing a header, happens to fall AFTER that point, so
            // clearing selection there never stuck (visible for as long as
            // the button stayed down, before any Click/MouseUp fired).
            // base.OnCellMouseDown above is exactly where that side effect
            // itself happens, so clearing immediately after it - still
            // within the same synchronous mouse-down handling, before the
            // control repaints - is what actually keeps it from showing.
            ClearSelection();
            CurrentCell = null;

            _pendingReorderColumnIndex = -1;

            // The border/resize check itself now lives in OnMouseDown
            // below, not here - see its own remarks on why. Only the
            // reorder-arming stays here, since it genuinely needs a real
            // column under the cursor to have anything to drag at all.
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0 || _isResizingColumn)
            {
                return;
            }

            // DataGridViewCellMouseEventArgs.X/Y are relative to the CELL,
            // not the control - PointToClient(Cursor.Position) sidesteps
            // that entirely rather than adding the cell's own display
            // rectangle back in.
            int controlX = PointToClient(Cursor.Position).X;

            // !AllowDrop also covers the MTA case the constructor's own
            // apartment-state check guards against - without a registered
            // drop target, DoDragDrop has nothing to hand the drag to.
            if (!AllowDrop || !IsColumnReorderable(e.ColumnIndex))
            {
                return;
            }

            _pendingReorderColumnIndex = e.ColumnIndex;
            _pendingReorderStartX = controlX;
        }


        // The border/resize check used to live in OnCellMouseDown above,
        // gated behind e.ColumnIndex >= 0 - past the last column's own
        // right edge, in the empty header space beyond every real column,
        // DataGridView reports RowIndex -1 (still "the header") but
        // ColumnIndex -1 too (no column there), so that check silently
        // never ran there at all. OnCellMouseMove's own hover cursor never
        // had this problem (it computes its own control-relative X instead
        // of trusting e.ColumnIndex - see its own TryGetColumnAtBorder
        // call), which is exactly why the VSplit hint could show out there
        // while actually pressing down to start a resize from that same
        // spot silently did nothing. This plain Control-level MouseDown -
        // not tied to resolving a cell at all - is the fix, the same
        // reasoning as OnMouseUp/OnMouseMove already needed it for ending/
        // continuing a resize. e.Y < ColumnHeadersHeight stands in for
        // "RowIndex == -1" here, since MouseEventArgs has no such thing.
        /// <inheritdoc/>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            _isResizingColumn = false;
            _suppressNextHeaderClickSort = false;

            if (e.Button != MouseButtons.Left || e.Y >= ColumnHeadersHeight)
            {
                return;
            }

            int controlX = PointToClient(Cursor.Position).X;
            int logicalX = controlX + HorizontalScrollingOffset;

            if (!TryGetColumnAtBorder(logicalX, out DataGridViewColumn borderColumn))
            {
                return;
            }

            // A border always wins over a reorder drag OnCellMouseDown may
            // already have armed for this same press (a real cell right at
            // a border can resolve to either handler depending on exactly
            // which pixel, and either could end up running first) - a
            // locked column's border should do nothing at all, not fall
            // through into moving the column instead.
            _pendingReorderColumnIndex = -1;

            // The mouse still went down and (typically) back up on the
            // same header cell either way, which is exactly what
            // OnColumnHeaderMouseClick's own click detection is built to
            // catch - pressing right on a border was never actually
            // aiming to sort that column, whether or not a real resize
            // ends up happening from here.
            _suppressNextHeaderClickSort = true;

            if (!IsColumnResizable(borderColumn))
            {
                return;
            }

            _isResizingColumn = true;
            _resizeColumnIndex = borderColumn.Index;
            _resizeStartX = controlX;
            _resizeStartWidth = borderColumn.Width;

            // Keeps this tracking correctly even if a fast drag carries
            // the cursor outside the grid's own bounds - without it,
            // MouseMove/MouseUp simply stop arriving once the cursor
            // leaves this control's screen area, leaving the resize stuck
            // "in progress" until some unrelated later click happens to
            // reset it.
            Capture = true;
        }


        /// <inheritdoc/>
        protected override void OnCellMouseMove(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseMove(e);

            // An active resize is tracked from the plain Control-level
            // OnMouseMove below instead, not here - see its own remarks on
            // why. The hover cursor hint moved there too, for the same
            // underlying reason - see OnMouseMove's own remarks.
            if (_isResizingColumn || e.RowIndex != -1)
            {
                return;
            }

            int controlX = PointToClient(Cursor.Position).X;

            if (_pendingReorderColumnIndex >= 0 && !_columnDrag.IsDragging &&
                Math.Abs(controlX - _pendingReorderStartX) >= SystemInformation.DragSize.Width)
            {
                int columnIndex = _pendingReorderColumnIndex;
                _pendingReorderColumnIndex = -1;
                BeginColumnDragDrop(columnIndex);
            }
        }


        /// <inheritdoc/>
        protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseUp(e);

            // A plain click (never exceeded the drag threshold) just clears
            // the pending state - nothing to reorder, nothing to undo,
            // since BeginColumnDragDrop is only ever called once
            // OnCellMouseMove sees the threshold exceeded. The header's own
            // click-to-sort (OnColumnHeaderMouseClick) is unaffected either
            // way: a real reorder drag is handed off to DoDragDrop below,
            // which - like any OLE drag-drop - never lets the originating
            // control see a matching MouseUp/Click of its own once the drag
            // starts, so there's nothing here that needs to suppress it.
            _pendingReorderColumnIndex = -1;

            if (_isResizingColumn)
            {
                EndColumnResize();
            }
        }


        // Also ends an in-progress resize on the plain Control-level
        // MouseUp, not just OnCellMouseUp above - confirmed live that
        // OnCellMouseUp/CellMouseUp simply never fires for a MouseUp whose
        // cursor is outside this control's own bounds at the time (this
        // control's Capture still routes the underlying message here
        // regardless, but DataGridView apparently only raises the CELL-
        // level event once it can resolve an actual cell under the
        // cursor, and there wasn't one out there). This one is the same
        // message either way, just not conditioned on resolving a cell
        // first, so it reliably fires regardless of where the cursor
        // ends up. OnMouseMove's own button-state check (see its own
        // remarks) is the other half of this same fix, for the case where
        // even this somehow doesn't run before the cursor moves again.
        /// <inheritdoc/>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isResizingColumn)
            {
                EndColumnResize();
            }
        }


        // A second, independent safety net for the same "Cursor is a
        // whole-control property" problem EndColumnResize's own reset
        // covers (see its own remarks) - this one for hovering a border
        // (arming nothing yet, just showing VSplit) and then leaving the
        // control entirely without ever resizing at all, which leaves
        // nothing to trigger that reset. Skipped while a resize is
        // actually in progress - Capture keeps this control receiving
        // mouse events even once the cursor is physically outside its
        // bounds, and MouseLeave still fires for that, but flipping the
        // cursor back to Default mid-drag would fight the still-accurate
        // VSplit hint for no reason; EndColumnResize's own reset already
        // covers the moment the drag actually ends, wherever that is.
        /// <inheritdoc/>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!_isResizingColumn)
            {
                Cursor = Cursors.Default;
            }
        }


        // An active resize is tracked here, not from OnCellMouseMove, for
        // exactly the same reason OnMouseUp above is: confirmed live that
        // widening the LAST column specifically made resizing intermittently
        // stop responding while the button was still held. Widening it
        // means the cursor is naturally ahead of that column's own
        // (not-yet-updated) right edge, past every real cell entirely -
        // CellMouseMove, needing an actual cell to resolve under the
        // cursor the same way CellMouseUp needs one, simply never fires
        // for that position, so ApplyLiveColumnResize never got called
        // for it. Any OTHER column's right edge still has the NEXT
        // column's own cell sitting there to catch it, which is why only
        // the last column ever showed this.
        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isResizingColumn)
            {
                if ((MouseButtons & MouseButtons.Left) != MouseButtons.Left)
                {
                    EndColumnResize();
                    return;
                }

                ApplyLiveColumnResize();
                return;
            }

            // Hover-only cursor hint for a resizable border - moved here
            // from OnCellMouseMove for the same reason OnMouseDown's own
            // border/resize check moved out of OnCellMouseDown: past the
            // last column's own right edge, in the empty header space
            // beyond every real column, CellMouseMove never fires at all,
            // so a hint set while still over the actual border tolerance
            // (back when CellMouseMove still fired, right at the column's
            // own edge) could never be cleared again once the cursor kept
            // moving right past it - it just stayed VSplit for the WHOLE
            // remaining empty area instead of only the real tolerance
            // zone. e.Y < ColumnHeadersHeight stands in for "RowIndex ==
            // -1" here, the same substitution OnMouseDown already needed.
            if (e.Y >= ColumnHeadersHeight)
            {
                Cursor = Cursors.Default;
                return;
            }

            int controlX = PointToClient(Cursor.Position).X;
            bool overResizableBorder = TryGetColumnAtBorder(controlX + HorizontalScrollingOffset, out DataGridViewColumn hoveredColumn) &&
                IsColumnResizable(hoveredColumn);
            Cursor = overResizableBorder ? Cursors.VSplit : Cursors.Default;
        }


        private void EndColumnResize()
        {
            _isResizingColumn = false;
            _resizeColumnIndex = -1;
            Capture = false;

            // Cursor is a whole-control property, not a per-pixel one -
            // once OnCellMouseMove's hover check (or arming the resize
            // itself) sets it to VSplit, it stays VSplit everywhere on
            // this control, for however long, until something explicitly
            // sets it back. That "something" was only ever the next hover
            // recalculation - fine as long as one actually happens before
            // the cursor leaves, but confirmed live that moving the mouse
            // off the control right after finishing a resize (nothing left
            // to fire a recalculation over) leaves it stuck on VSplit.
            // Resetting the moment a resize actually ends, regardless of
            // where the cursor is, means there's nothing left to get stuck.
            Cursor = Cursors.Default;
        }


        // Applies the column's new width directly, on every single
        // mouse-move tick while a resize is in progress - unlike
        // DataGridView's own native resize-drag (confirmed live to only
        // move a guideline and apply the real width once, on mouse-up),
        // setting Width here goes through the ordinary property-changed
        // path, which repaints normally on each call rather than only once
        // at the end.
        private void ApplyLiveColumnResize()
        {
            int controlX = PointToClient(Cursor.Position).X;
            int delta = controlX - _resizeStartX;
            int minimumWidth = Math.Max(_minimumColumnWidth, Columns[_resizeColumnIndex].MinimumWidth);
            int newWidth = Math.Max(minimumWidth, _resizeStartWidth + delta);

            if (Columns[_resizeColumnIndex].Width != newWidth)
            {
                Columns[_resizeColumnIndex].Width = newWidth;
            }
        }


        // Called once OnCellMouseMove sees the drag threshold exceeded.
        // From here on, tracking the rest of the drag is handed off to
        // WinForms' own DoDragDrop/OnDragOver/OnDragDrop - the same
        // mechanism ListView's own column reorder uses, and ListBox's own
        // item-reorder drag before that.
        private void BeginColumnDragDrop(int columnIndex)
        {
            // Run takes care of every way the drag can end, including a cancelled drag (Escape, or dropped somewhere OnDragDrop never
            // fires) - OnDragDrop itself only needs to perform the actual move. Unlike ListView, the header here isn't a separate
            // native child window - it's painted by this control's own OnCellPainting, so a plain Invalidate() is enough to make
            // the insertion line disappear.
            _columnDrag.Run(columnIndex, () => DoDragDrop(columnIndex, DragDropEffects.Move), Invalidate);
        }


        /// <inheritdoc/>
        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (!_columnDrag.IsDragging)
            {
                base.OnDragOver(drgevent);
                return;
            }

            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));
            int insertBefore = GetColumnDropInsertionIndex(point.X + HorizontalScrollingOffset);

            drgevent.Effect = DragDropEffects.Move;

            if (_columnDrag.MoveInsertTo(insertBefore))
                Invalidate();

            base.OnDragOver(drgevent);
        }


        /// <inheritdoc/>
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (_columnDrag.HasTarget)
            {
                MoveColumnToDisplayIndex(_columnDrag.ColumnIndex, _columnDrag.InsertBeforeDisplayIndex);
            }

            base.OnDragDrop(drgevent);
        }


        /// <inheritdoc/>
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            if (_columnDrag.IsDragging)
            {
                gfbevent.UseDefaultCursors = false;
                Cursor.Current = Cursors.SizeWE;
            }

            base.OnGiveFeedback(gfbevent);
        }


        private List<DataGridViewColumn> GetColumnsInDisplayOrder()
        {
            if (_columnsInDisplayOrderCache != null)
            {
                return _columnsInDisplayOrderCache;
            }

            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn column in Columns)
            {
                columns.Add(column);
            }

            _columnsInDisplayOrderCache = ColumnLayoutMath.OrderByDisplayIndex(columns, column => column.DisplayIndex);
            return _columnsInDisplayOrderCache;
        }


        /// <inheritdoc/>
        protected override void OnColumnRemoved(DataGridViewColumnEventArgs e)
        {
            base.OnColumnRemoved(e);
            _columnsInDisplayOrderCache = null;
        }


        /// <inheritdoc/>
        protected override void OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)
        {
            base.OnColumnDisplayIndexChanged(e);
            _columnsInDisplayOrderCache = null;
        }


        private bool IsColumnResizable(DataGridViewColumn column)
        {
            return IsColumnResizable(column.Index);
        }


        // x is in "logical" (unscrolled) space - cumulative column widths
        // from DisplayIndex 0, same space GetColumnDropInsertionIndex uses -
        // callers convert from control-relative coordinates by adding
        // HorizontalScrollingOffset first, since this control (unlike
        // ListView's native header) can scroll its columns independently
        // of where they're actually drawn. Resizability itself is a
        // separate question from being AT a border at all (see
        // IsColumnResizable) - a locked column still has to block a
        // reorder drag from starting there, it just doesn't also start a
        // resize.
        private bool TryGetColumnAtBorder(int x, out DataGridViewColumn column)
        {
            return ColumnLayoutMath.TryGetColumnAtBorder(GetColumnsInDisplayOrder(), c => c.Width, x, ResizeGripWidth, out column);
        }


        // x is in "logical" (unscrolled) space - see this method's own
        // caller for why. Mirrors ListView's own GetColumnDropInsertionIndex.
        private int GetColumnDropInsertionIndex(int x)
        {
            List<DataGridViewColumn> orderedColumns = GetColumnsInDisplayOrder();
            int insertBeforeDisplayIndex = ColumnLayoutMath.GetDropInsertionIndex(orderedColumns, column => column.Width, x);
            return ClampReorderInsertionIndex(orderedColumns, insertBeforeDisplayIndex);
        }


        // A real column being dragged must never land ahead of a pinned
        // leftmost column (ShowEnumeration's own "#" column) or past a
        // pinned rightmost one (DataGridView's own delete-row column) -
        // both are already excluded from being the column that STARTS a
        // drag (see SetColumnReorderable in their own setters), but a drop
        // target computed purely from cursor position could otherwise still
        // land in their slot, displacing them there instead. Walks in from
        // both ends of the CURRENT display order counting how many leading/
        // trailing columns are non-reorderable, rather than checking the
        // enumeration/delete columns by name - keeps this working for any
        // future pinned column the same way, without this class needing to
        // know DataGridView's own delete-column field exists.
        private int ClampReorderInsertionIndex(List<DataGridViewColumn> orderedColumns, int insertBeforeDisplayIndex)
        {
            int minimum = 0;
            while (minimum < orderedColumns.Count && !IsColumnReorderable(orderedColumns[minimum].Index))
            {
                minimum++;
            }

            int maximum = orderedColumns.Count;
            while (maximum > minimum && !IsColumnReorderable(orderedColumns[maximum - 1].Index))
            {
                maximum--;
            }

            return Math.Max(minimum, Math.Min(insertBeforeDisplayIndex, maximum));
        }


        // Mirrors ListView's own MoveColumnToDisplayIndex.
        private void MoveColumnToDisplayIndex(int columnIndex, int insertBeforeDisplayIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return;
            }

            DataGridViewColumn column = Columns[columnIndex];
            int? targetDisplayIndex = ColumnLayoutMath.GetMoveTargetDisplayIndex(column.DisplayIndex, insertBeforeDisplayIndex);
            if (targetDisplayIndex == null)
            {
                return;
            }

            column.DisplayIndex = targetDisplayIndex.Value;
        }


        // Drawn as part of the same OnCellPainting pass as the header
        // cell's own normal (native) painting, rather than replacing it.
        // Mirrors ListView's own DrawColumnDragInsertionLine.
        private void DrawColumnDragInsertionLine(DataGridViewCellPaintingEventArgs e)
        {
            if (!_columnDrag.IsDragging || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewColumn column = Columns[e.ColumnIndex];
            ColumnHeaderPainting.DrawColumnDragInsertionLine(
                e.Graphics, e.CellBounds, column.DisplayIndex, Columns.Count, _columnDrag.InsertBeforeDisplayIndex, ColumnReorderIndicatorColor);
        }
    }
}
