using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.DataGridView"/> for
    /// displaying data bound via
    /// <see cref="System.Windows.Forms.DataGridView.DataSource"/> - just
    /// showing rows, with column-header sorting, but no editing (no adding,
    /// deleting, cutting, or pasting rows). Reach for
    /// <see cref="DataGridView"/> instead - it inherits everything here and
    /// adds those - whenever the grid genuinely needs to be editable; this
    /// one is deliberately the lighter of the two for a grid that only ever
    /// shows data, and sets <see cref="System.Windows.Forms.DataGridView.ReadOnly"/>
    /// to enforce that by default.
    /// </summary>
    /// <remarks>
    /// Consumers never need to spell out
    /// <c>ErikwnkWFUI.Controls.ReadOnlyDataGridView</c>: go through
    /// <see cref="UIStyles.DataGridViews.CreateReadOnly"/>, which hands back
    /// a plain <see cref="System.Windows.Forms.DataGridView"/>-typed
    /// reference.
    /// </remarks>
    public class ReadOnlyDataGridView : System.Windows.Forms.DataGridView
    {
        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        private Color _selectionBackColorOverride;
        private bool _selectionBackColorIsOverridden;
        private bool _allowColumnReordering = true;
        private readonly HashSet<int> _nonReorderableColumns = new HashSet<int>();
        private Color _columnReorderIndicatorColorOverride;
        private bool _columnReorderIndicatorColorIsOverridden;
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private int _pendingReorderColumnIndex = -1;
        private int _pendingReorderStartX;

        // Snapshot of DataBoundItem references in the order they were bound,
        // captured on each fresh bind/reset - lets a header click's third
        // state ("original") restore that order without the underlying data
        // source needing to support sorting itself.
        private List<object> _originalOrder = new List<object>();
        private int _sortedColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private bool _clearSelectionOnNextVisible;
        private bool _isApplyingInternalDataChange;
        private readonly Dictionary<DataGridViewColumn, IComparer> _columnSortComparers =
            new Dictionary<DataGridViewColumn, IComparer>();

        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set { _headerBackColor = value; ApplyStyles(); }
        }

        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get => _headerForeColor;
            set { _headerForeColor = value; ApplyStyles(); }
        }

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get => _rowBackColor;
            set
            {
                _rowBackColor = value;
                _alternateRowBackColor = Darken(value, 5);
                ApplyStyles();
            }
        }

        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get => _rowForeColor;
            set { _rowForeColor = value; ApplyStyles(); }
        }

        /// <summary>
        /// Background color of a selected cell/row. Follows the current
        /// accent (<see cref="UIColors.Primary"/>) live until explicitly
        /// set - same pattern as ListView's SelectionOverlayColor.
        /// </summary>
        public Color SelectionBackColor
        {
            get => _selectionBackColorIsOverridden ? _selectionBackColorOverride : UIColors.Primary;
            set
            {
                _selectionBackColorOverride = value;
                _selectionBackColorIsOverridden = true;
                ApplyStyles();
            }
        }

        /// <summary>
        /// Master switch for whether ANY column can be dragged to reorder
        /// it at all - mirrors <see cref="ListView.AllowColumnReordering"/>.
        /// Per-column overrides via <see cref="SetColumnReorderable"/>
        /// still apply among whichever columns this allows; setting this
        /// false overrides all of them. Defaults to true.
        /// </summary>
        /// <remarks>
        /// Reordering is fully hand-rolled the same way as ListView's own
        /// (see <see cref="ColumnReorderIndicatorColor"/> for why) rather
        /// than using <see cref="System.Windows.Forms.DataGridView.AllowUserToOrderColumns"/> -
        /// that hands the drag feedback to DataGridView's own internal
        /// drawing, which always looks like plain Windows chrome with no
        /// way to recolor it to match this control's theme. That native
        /// property is deliberately left false (see the constructor); this
        /// one governs the owned implementation instead.
        /// </remarks>
        public bool AllowColumnReordering
        {
            get => _allowColumnReordering;
            set => _allowColumnReordering = value;
        }

        /// <summary>
        /// Configurable per column, independent of
        /// <see cref="AllowColumnReordering"/>: a column can be pinned in
        /// place (e.g. <see cref="DataGridView"/>'s own delete-row column,
        /// always pinned rightmost) while the rest can still be freely
        /// dragged into a new order.
        /// </summary>
        public void SetColumnReorderable(int columnIndex, bool reorderable)
        {
            if (reorderable)
            {
                _nonReorderableColumns.Remove(columnIndex);
            }
            else
            {
                _nonReorderableColumns.Add(columnIndex);
            }
        }

        public bool IsColumnReorderable(int columnIndex)
        {
            return _allowColumnReordering && !_nonReorderableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Color of the vertical line the header shows while a column is
        /// being dragged to reorder it. Follows the current accent
        /// (<see cref="UIColors.Primary"/>) live until explicitly set, same
        /// pattern as <see cref="SelectionBackColor"/> and ListView's own
        /// ColumnReorderIndicatorColor.
        /// </summary>
        public Color ColumnReorderIndicatorColor
        {
            get => _columnReorderIndicatorColorIsOverridden ? _columnReorderIndicatorColorOverride : UIColors.Primary;
            set
            {
                _columnReorderIndicatorColorOverride = value;
                _columnReorderIndicatorColorIsOverridden = true;
            }
        }

        /// <summary>
        /// Whether clicking a column header cycles it through ascending,
        /// descending, and original row order (see the "Column header
        /// sorting" remarks on <see cref="CycleSort"/>). Defaults to true;
        /// a consumer sets this to false - any time before rows are bound,
        /// since <see cref="OnColumnAdded"/> reads it once per column - to
        /// keep headers as plain labels for a grid where reordering rows
        /// wouldn't make sense. Clicking a header still clears a stray
        /// selection either way (see <see cref="HandleMouseDown"/>); only
        /// the sorting itself is affected. The glyph/arrow shown for the
        /// active sort column is always this control's own concern, not
        /// something a consumer sets.
        /// </summary>
        /// <remarks>
        /// This turns sorting on/off for every column at once - to keep
        /// just one specific column unsortable while the rest stay
        /// sortable, set that column's own standard
        /// <see cref="DataGridViewColumn.SortMode"/> to
        /// <see cref="DataGridViewColumnSortMode.NotSortable"/> instead
        /// (any time; before or after adding it) - <see cref="DataGridView"/>'s
        /// own optional delete-row column relies on the same thing.
        /// </remarks>
        public bool SortingEnabled { get; set; } = true;

        /// <summary>
        /// Registers a custom comparer for a specific column, used by
        /// column-header sorting (see <see cref="SortingEnabled"/>/
        /// <see cref="CycleSort"/>) instead of the default comparison
        /// (<see cref="IComparable"/>, falling back to case-insensitive
        /// string comparison) - useful for a column whose cell values
        /// don't sort correctly as plain strings/IComparables on their
        /// own, e.g. dates stored in a non-lexicographic text format, or
        /// numbers stored as strings. The comparer receives the two
        /// cells' raw <see cref="DataGridViewCell.Value"/> objects and is
        /// expected to compare them in plain ascending order - this
        /// control still applies ascending/descending itself, the same
        /// way it already does for the default comparison, so a
        /// registered comparer never needs to know which direction is
        /// currently active. Pass <c>null</c> as <paramref name="comparer"/>
        /// to remove one and revert that column to the default. Set any
        /// time after the column exists; not tied to binding order the
        /// way <see cref="SortingEnabled"/> is.
        /// </summary>
        public void SetSortComparer(DataGridViewColumn column, IComparer comparer)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            if (comparer == null)
            {
                _columnSortComparers.Remove(column);
            }
            else
            {
                _columnSortComparers[column] = comparer;
            }
        }

        public ReadOnlyDataGridView()
        {
            _alternateRowBackColor = Darken(_rowBackColor, 5);

            BackgroundColor = UIColors.BackgroundDark;
            GridColor = UIColors.BorderMedium;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersVisible = false;

            // Without this, the column headers ignore
            // ColumnHeadersDefaultCellStyle entirely and always render
            // with the OS's own visual-style theme instead - the
            // DataGridView equivalent of the SetWindowTheme("", "") dance
            // BorderedProgressBar needs for the exact same reason.
            EnableHeadersVisualStyles = false;

            // A display-only grid by default - DataGridView (which adds
            // adding/deleting/cutting/pasting rows on top of this) sets
            // both back to what it actually needs in its own constructor.
            ReadOnly = true;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;

            AllowUserToResizeRows = false;
            MultiSelect = true;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            Font = UIFonts.Normal;
            RowTemplate.Height = Font.Height + 12;

            // Left false deliberately - see AllowColumnReordering's own
            // remarks on why reordering is hand-rolled instead (OnCellMouseDown/
            // OnCellMouseMove/OnDragOver/OnDragDrop below) rather than using
            // this native switch.
            AllowUserToOrderColumns = false;

            // AllowDrop is only ever needed for that same hand-rolled drag,
            // not an app-facing drop target - and only actually settable
            // for it here. Registering a drop target needs an STA thread
            // (true for any real WinForms UI thread) - confirmed live that
            // setting this on an MTA one (e.g. a test harness thread with
            // no message loop) doesn't throw, but can silently block for
            // many seconds while the underlying OLE registration retries.
            // Skipped entirely off STA, where the drag itself couldn't
            // have worked anyway - OnCellMouseDown below checks AllowDrop
            // itself and never arms a drag if this never got set.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                AllowDrop = true;
            }

            EnableDoubleBuffering();
            ApplyStyles();

            MouseDown += HandleMouseDown;
        }

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);

            // Binding to a new DataSource replaces the row collection
            // entirely, which resets per-row visual state - reapplying
            // here keeps alternating-row/selection colors correct for
            // whatever just got bound in, not just whatever existed at
            // construction time.
            ApplyStyles();

            if (_isApplyingInternalDataChange)
            {
                // This Reset was caused by ApplyBatchedDataSourceChange
                // rewriting the bound list's contents (CycleSort/
                // ReorderDataSource here for a sort, or one of
                // DataGridView's own editing operations in a subclass),
                // not a real rebind - the bookkeeping below would
                // otherwise stomp on whatever state that change is
                // applying right now.
                return;
            }

            // A Reset is a fresh bind (DataSource assigned, or the bound
            // list reset) - that's the only time a "no cell selected on
            // load" default and a fresh original-order snapshot make sense.
            // Incremental changes (a row added/edited) leave both alone, so
            // typing into the grid doesn't fight the user's own selection.
            if (e.ListChangedType == ListChangedType.Reset)
            {
                ClearSelection();
                CurrentCell = null;

                // Binding normally happens before the control is ever shown,
                // and WinForms auto-selects the first cell the first time a
                // grid with rows but no CurrentCell receives focus - which
                // happens later, as part of the containing form being shown.
                // Clearing it again once actually visible is what defeats
                // that (setting it here alone doesn't stick).
                _clearSelectionOnNextVisible = true;

                _originalOrder = new List<object>();
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.DataBoundItem != null)
                    {
                        _originalOrder.Add(row.DataBoundItem);
                    }
                }

                _sortedColumnIndex = -1;
                _sortOrder = SortOrder.None;

                foreach (DataGridViewColumn column in Columns)
                {
                    column.HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible || !_clearSelectionOnNextVisible || !IsHandleCreated)
            {
                return;
            }

            _clearSelectionOnNextVisible = false;

            // Queued rather than called directly - becoming visible is what
            // triggers the framework's own auto-select in the first place,
            // so clearing it has to happen after that finishes, not during.
            BeginInvoke(new Action(() =>
            {
                ClearSelection();
                CurrentCell = null;
            }));
        }

        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);

            // Windows shows a cell's accessible "default action" as a hover
            // hint on its own (independent of ShowCellToolTips) whenever a
            // cell has no other accessible name to show instead - which is
            // exactly an empty, editable cell. For a plain DataGridViewTextBoxCell
            // that default action is a bare, unhelpful "Edit"/"Bearbeiten"
            // regardless of what the column actually holds, so it's replaced
            // here with nothing (or, for the "type here to add a row"
            // placeholder specifically - only ever present on a
            // DataGridView with AllowUserToAddRows enabled, not this class
            // itself - something that actually explains what clicking
            // there does).
            if (e.Column.CellTemplate is DataGridViewTextBoxCell &&
                !(e.Column.CellTemplate is EditHintTextBoxCell))
            {
                e.Column.CellTemplate = new EditHintTextBoxCell();
            }

            if (!SortingEnabled)
            {
                return;
            }

            // Programmatic hands full control of header-click behavior to
            // CycleSort below - Automatic would have the base DataGridView
            // attempt its own sort first (and throw if the bound list
            // doesn't support IBindingList sorting, which a plain
            // BindingList&lt;T&gt; doesn't). Left alone if a consumer already
            // set NotSortable on this exact column object before adding
            // it - e.g. to keep one specific column unsortable while
            // SortingEnabled stays true for the rest - rather than
            // unconditionally overwriting it.
            if (e.Column.SortMode != DataGridViewColumnSortMode.NotSortable)
            {
                e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnColumnHeaderMouseClick(e);

            // base's own header-click handling can move CurrentCell to the
            // column's first row as a side effect - SortMode.Programmatic
            // only suppresses its automatic sort, not that. Clearing again
            // here (in addition to HandleMouseDown's clear on the way in)
            // is what stops that from ever reaching the screen.
            ClearSelection();
            CurrentCell = null;

            bool isSortableColumn =
                e.ColumnIndex >= 0 && Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable;

            if (SortingEnabled && e.Button == MouseButtons.Left && isSortableColumn)
            {
                CycleSort(e.ColumnIndex);
            }
        }

        // Clicking truly empty space below the rows (no cell there at all,
        // so OnCellMouseDown below never fires) leaves a previously
        // selected cell visually selected by default - not a meaningful
        // selection target, so this clears it.
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            HitTestInfo hitTest = HitTest(e.X, e.Y);

            if (hitTest.Type == DataGridViewHitTestType.None)
            {
                ClearSelection();
                CurrentCell = null;
            }
        }

        // Forces a repaint on every selection change - a rapid drag that
        // extends the selection back and forth across many cells could
        // leave an earlier cell's highlight stuck stale: still actually
        // Selected, just not repainted to show it, until something else
        // forces a repaint. Only ever asks for a repaint here - never
        // touches Selected/SelectedCells itself.
        //
        // Coalesced rather than an immediate Invalidate() on every single
        // tick - SelectionChanged fires repeatedly (once per mouse-move)
        // during a fast drag-select, and a full-control repaint that often
        // was wasted work; at most one repaint stays queued at a time.
        private bool _selectionRepaintPending;

        protected override void OnSelectionChanged(EventArgs e)
        {
            base.OnSelectionChanged(e);

            if (!IsHandleCreated)
            {
                Invalidate();
                return;
            }

            if (_selectionRepaintPending)
            {
                return;
            }

            _selectionRepaintPending = true;
            BeginInvoke(new System.Windows.Forms.MethodInvoker(() =>
            {
                _selectionRepaintPending = false;

                if (!IsDisposed)
                {
                    Invalidate();
                }
            }));
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseDown(e);

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

            // !AllowDrop also covers the MTA case the constructor's own
            // apartment-state check guards against - without a registered
            // drop target, DoDragDrop has nothing to hand the drag to.
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0 || !AllowDrop || !IsColumnReorderable(e.ColumnIndex))
            {
                return;
            }

            // DataGridViewCellMouseEventArgs.X/Y are relative to the CELL,
            // not the control - PointToClient(Cursor.Position) sidesteps
            // that entirely rather than adding the cell's own display
            // rectangle back in.
            int controlX = PointToClient(Cursor.Position).X;

            // Resize grips live on the same header cells this would
            // otherwise start a reorder drag from - skipped here so a
            // width-resize drag (still fully native, unaffected by any of
            // this) never gets hijacked into a reorder attempt instead.
            if (IsNearColumnBorder(controlX + HorizontalScrollingOffset))
            {
                return;
            }

            _pendingReorderColumnIndex = e.ColumnIndex;
            _pendingReorderStartX = controlX;
        }

        protected override void OnCellMouseMove(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseMove(e);

            if (_pendingReorderColumnIndex < 0 || _isDraggingColumn || e.RowIndex != -1)
            {
                return;
            }

            int controlX = PointToClient(Cursor.Position).X;
            if (Math.Abs(controlX - _pendingReorderStartX) < SystemInformation.DragSize.Width)
            {
                return;
            }

            int columnIndex = _pendingReorderColumnIndex;
            _pendingReorderColumnIndex = -1;
            BeginColumnDragDrop(columnIndex);
        }

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
        }

        // Called once OnCellMouseMove sees the drag threshold exceeded.
        // From here on, tracking the rest of the drag is handed off to
        // WinForms' own DoDragDrop/OnDragOver/OnDragDrop - the same
        // mechanism ListView's own column reorder uses, and ListBox's own
        // item-reorder drag before that.
        private void BeginColumnDragDrop(int columnIndex)
        {
            _dragColumnIndex = columnIndex;
            _isDraggingColumn = true;
            _dragInsertBeforeDisplayIndex = -1;

            try
            {
                DoDragDrop(columnIndex, DragDropEffects.Move);
            }
            finally
            {
                // Covers every way the drag can end, including a cancelled
                // drag (Escape, or dropped somewhere OnDragDrop never
                // fires) - OnDragDrop itself only needs to perform the
                // actual move, not reset this shared state.
                _isDraggingColumn = false;
                _dragColumnIndex = -1;
                _dragInsertBeforeDisplayIndex = -1;

                // Unlike ListView, the header here isn't a separate native
                // child window - it's painted by this control's own
                // OnCellPainting, so a plain Invalidate() is enough to
                // make the insertion line disappear.
                Invalidate();
            }
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (!_isDraggingColumn)
            {
                base.OnDragOver(drgevent);
                return;
            }

            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));
            int insertBefore = GetColumnDropInsertionIndex(point.X + HorizontalScrollingOffset);

            drgevent.Effect = DragDropEffects.Move;

            if (insertBefore != _dragInsertBeforeDisplayIndex)
            {
                _dragInsertBeforeDisplayIndex = insertBefore;
                Invalidate();
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
            if (_isDraggingColumn)
            {
                gfbevent.UseDefaultCursors = false;
                Cursor.Current = Cursors.SizeWE;
            }

            base.OnGiveFeedback(gfbevent);
        }

        private List<DataGridViewColumn> GetColumnsInDisplayOrder()
        {
            List<DataGridViewColumn> ordered = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn column in Columns)
            {
                ordered.Add(column);
            }

            ordered.Sort((first, second) => first.DisplayIndex.CompareTo(second.DisplayIndex));
            return ordered;
        }

        // x is in "logical" (unscrolled) space - cumulative column widths
        // from DisplayIndex 0, same space GetColumnDropInsertionIndex uses -
        // callers convert from control-relative coordinates by adding
        // HorizontalScrollingOffset first, since this control (unlike
        // ListView's native header) can scroll its columns independently
        // of where they're actually drawn.
        private bool IsNearColumnBorder(int x)
        {
            const int resizeGripWidth = 5;
            int cumulativeWidth = 0;
            foreach (DataGridViewColumn column in GetColumnsInDisplayOrder())
            {
                cumulativeWidth += column.Width;
                if (Math.Abs(x - cumulativeWidth) <= resizeGripWidth)
                {
                    return true;
                }
            }

            return false;
        }

        // Where a column dropped at x would be inserted, expressed as
        // "insert before this display index" - the boundary flips at each
        // column's midpoint rather than its edges, so the insertion line
        // snaps to whichever side of the hovered column the cursor is
        // actually closer to, matching ListView's own GetColumnDropInsertionIndex.
        private int GetColumnDropInsertionIndex(int x)
        {
            List<DataGridViewColumn> orderedColumns = GetColumnsInDisplayOrder();
            int cumulativeWidth = 0;
            for (int displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
            {
                int columnWidth = orderedColumns[displayIndex].Width;
                if (x < cumulativeWidth + columnWidth / 2)
                {
                    return displayIndex;
                }

                cumulativeWidth += columnWidth;
            }

            return orderedColumns.Count;
        }

        // insertBeforeDisplayIndex is expressed in the ORIGINAL display
        // order (before the dragged column is removed from its old slot) -
        // the standard "move to before index P" -> "target index"
        // adjustment (subtract one if P is past the column's own current
        // position) is needed because DisplayIndex's setter moves the
        // column to an absolute position, and removing it from its old
        // slot first would shift everything after that slot left by one.
        // Mirrors ListView's own MoveColumnToDisplayIndex.
        private void MoveColumnToDisplayIndex(int columnIndex, int insertBeforeDisplayIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return;
            }

            DataGridViewColumn column = Columns[columnIndex];
            int originalDisplayIndex = column.DisplayIndex;
            int targetDisplayIndex = insertBeforeDisplayIndex > originalDisplayIndex
                ? insertBeforeDisplayIndex - 1
                : insertBeforeDisplayIndex;

            if (targetDisplayIndex == originalDisplayIndex)
            {
                return;
            }

            column.DisplayIndex = targetDisplayIndex;
        }

        // Drawn as part of the same OnCellPainting pass as the header
        // cell's own normal (native) painting, rather than replacing it -
        // exactly one header cell's left edge lines up with
        // _dragInsertBeforeDisplayIndex (or, for "insert after the last
        // column", the last cell's right edge), so at most one of these two
        // checks ever draws anything per call. Mirrors ListView's own
        // DrawColumnDragInsertionLine.
        private void DrawColumnDragInsertionLine(DataGridViewCellPaintingEventArgs e)
        {
            if (!_isDraggingColumn || _dragInsertBeforeDisplayIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewColumn column = Columns[e.ColumnIndex];
            const int lineWidth = 2;

            using (SolidBrush brush = new SolidBrush(ColumnReorderIndicatorColor))
            {
                if (column.DisplayIndex == _dragInsertBeforeDisplayIndex)
                {
                    e.Graphics.FillRectangle(brush, e.CellBounds.Left, e.CellBounds.Top, lineWidth, e.CellBounds.Height);
                }
                else if (_dragInsertBeforeDisplayIndex == Columns.Count && column.DisplayIndex == Columns.Count - 1)
                {
                    e.Graphics.FillRectangle(brush, e.CellBounds.Right - lineWidth, e.CellBounds.Top, lineWidth, e.CellBounds.Height);
                }
            }
        }

        /// <summary>
        /// Three-state header click: ascending, then descending, then back
        /// to the order rows were in when last bound - clicking a different
        /// column starts that column fresh at ascending.
        /// </summary>
        private void CycleSort(int columnIndex)
        {
            if (columnIndex != _sortedColumnIndex)
            {
                _sortedColumnIndex = columnIndex;
                _sortOrder = SortOrder.Ascending;
            }
            else if (_sortOrder == SortOrder.Ascending)
            {
                _sortOrder = SortOrder.Descending;
            }
            else if (_sortOrder == SortOrder.Descending)
            {
                _sortOrder = SortOrder.None;
            }
            else
            {
                _sortOrder = SortOrder.Ascending;
            }

            foreach (DataGridViewColumn column in Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ReorderDataSource(BuildOriginalOrder());
            }
            else
            {
                // Still set for AT/screen-reader purposes (this is the
                // property they'd actually look at), but not what the
                // arrow on screen is drawn from - see OnCellPainting's own
                // remarks on why.
                Columns[columnIndex].HeaderCell.SortGlyphDirection = _sortOrder;
                ReorderDataSource(BuildSortedOrder(columnIndex, _sortOrder));
            }

            // Redundant with Invalidate() already happening as a side
            // effect of ClearSelection()/CurrentCell = null below (via
            // OnSelectionChanged) in the case where selection actually
            // changes - but a sort that doesn't move CurrentCell off of
            // wherever it already was (e.g. nothing was selected to begin
            // with) wouldn't otherwise repaint the header row at all,
            // leaving the custom-drawn glyph below stale. -1 addresses the
            // header row, same as everywhere else in this file that reads
            // RowIndex against it.
            InvalidateCell(columnIndex, -1);

            ClearSelection();
            CurrentCell = null;
        }

        // A plain Unicode triangle character, drawn in the header's own
        // text color, right-aligned in the sorted column's header cell -
        // not the native SortGlyphDirection glyph (still set, for AT/
        // screen-reader purposes, but not what this actually reads to
        // draw): that glyph rides on the OS's own visual-style painting,
        // which EnableHeadersVisualStyles = false deliberately opts out
        // of everywhere else on this control (so its own
        // ColumnHeadersDefaultCellStyle colors apply instead of the OS
        // theme) - confirmed live that with it off, the native glyph
        // doesn't paint at all, not even faintly. A character from the
        // font, rather than a hand-built polygon, means the font's own
        // hinting/anti-aliasing draws it correctly and symmetrically in
        // both directions for free - an earlier version of this filled a
        // polygon by hand instead, which needed its own anti-aliasing
        // just to stop the two directions rasterizing at visibly
        // different sizes.
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);

            if (e.RowIndex != -1)
            {
                return;
            }

            bool isSortedColumn = e.ColumnIndex == _sortedColumnIndex && _sortOrder != SortOrder.None;
            bool isDragInsertionTarget = _isDraggingColumn && _dragInsertBeforeDisplayIndex >= 0;

            if (!isSortedColumn && !isDragInsertionTarget)
            {
                return;
            }

            // The default header painting (background/border/text) has to
            // happen FIRST and be marked Handled here, same reason as
            // always with this trick - anything drawn before this point in
            // the method would otherwise just get painted over once
            // DataGridView's own default painting runs right after this
            // event handler returns (confirmed live: the insertion line
            // below drew, then immediately vanished, because it used to
            // run before this call).
            e.Paint(e.ClipBounds, e.PaintParts);
            e.Handled = true;

            DrawColumnDragInsertionLine(e);

            if (!isSortedColumn)
            {
                return;
            }

            // Ascending points down, descending points up - the opposite
            // of what might seem obvious, but matches what was actually
            // asked for here.
            string glyph = _sortOrder == SortOrder.Ascending ? "▼" : "▲";

            const int rightMargin = 4;

            using (Brush brush = new SolidBrush(_headerForeColor))
            {
                SizeF glyphSize = e.Graphics.MeasureString(glyph, Font);
                float x = e.CellBounds.Right - rightMargin - glyphSize.Width;
                float y = e.CellBounds.Top + (e.CellBounds.Height - glyphSize.Height) / 2f;
                e.Graphics.DrawString(glyph, Font, brush, x, y);
            }
        }


        // DataGridView.Sort(IComparer) is a no-op for the display order when
        // the control is data bound (it only works unbound, or against a
        // source that implements IBindingListView) - so instead, the bound
        // list itself is rewritten in the desired order. Any IList data
        // source works, not just BindingList&lt;T&gt;.
        private void ReorderDataSource(List<object> desiredOrder)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            ApplyBatchedDataSourceChange(list, () =>
            {
                list.Clear();

                foreach (object item in desiredOrder)
                {
                    list.Add(item);
                }
            });
        }

        private readonly struct DataSourceReflectionInfo
        {
            public DataSourceReflectionInfo(PropertyInfo raiseListChangedEventsProperty, MethodInfo resetBindingsMethod)
            {
                RaiseListChangedEventsProperty = raiseListChangedEventsProperty;
                ResetBindingsMethod = resetBindingsMethod;
            }

            public PropertyInfo RaiseListChangedEventsProperty { get; }
            public MethodInfo ResetBindingsMethod { get; }
        }

        private static readonly BindingFlags AnyInstanceMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // GetProperty/GetMethod do their own metadata lookup every call -
        // cheap next to an actual UI interaction, but ApplyBatchedDataSourceChange
        // below repeats it on every single sort (or, in a subclass, every
        // delete/paste/insert too), always asking the same question about
        // the same handful of DataSource types a given app actually binds.
        // Caching by Type (shared across every instance of this control -
        // the reflected members are the same regardless of which grid
        // asked) turns that into a one-time cost per type instead.
        private static readonly ConcurrentDictionary<Type, DataSourceReflectionInfo> DataSourceReflectionCache =
            new ConcurrentDictionary<Type, DataSourceReflectionInfo>();

        private static DataSourceReflectionInfo GetDataSourceReflectionInfo(Type dataSourceType)
        {
            return DataSourceReflectionCache.GetOrAdd(dataSourceType, type => new DataSourceReflectionInfo(
                type.GetProperty("RaiseListChangedEvents", AnyInstanceMember),
                type.GetMethod("ResetBindings", AnyInstanceMember, null, Type.EmptyTypes, null)));
        }

        // Inserts newItems at insertAtIndex via Clear() + re-Add() in the
        // exact final order - a direct list.Insert(index, item) at a
        // hand-picked index was tried first and found to corrupt the
        // grid's internal row/cell bookkeeping even though the resulting
        // list CONTENTS were correct (a later, unrelated click threw
        // InvalidOperationException out of WinForms' own code). Used by
        // DataGridView's own row-growing editing operations (paste,
        // insert row above/below) for the same reason. Callers are
        // expected to already be running inside an
        // ApplyBatchedDataSourceChange batch - this only rewrites the
        // list, it doesn't suppress/replay change notifications itself.
        //
        // originalItems is a snapshot the CALLER takes, not one this
        // method derives from list itself - list already contains
        // newItems by the time every caller here gets to call this
        // (AddNew() appends to the bound list immediately, before this
        // runs). Snapshotting list at that point would already include
        // them at the tail, and the trailing copy loop below would then
        // re-add that same tail - duplicating every one of newItems once.
        protected static void InsertItemsAt(
            IList list, List<object> originalItems, int insertAtIndex, IEnumerable<object> newItems)
        {
            list.Clear();

            for (int i = 0; i < insertAtIndex; i++)
            {
                list.Add(originalItems[i]);
            }

            foreach (object newItem in newItems)
            {
                list.Add(newItem);
            }

            for (int i = insertAtIndex; i < originalItems.Count; i++)
            {
                list.Add(originalItems[i]);
            }
        }

        // Runs a multi-step change against the bound list (list.Clear()+Add()
        // for a reorder, or whatever a subclass's own editing operations do)
        // with change notifications suppressed, then raises exactly one
        // Reset via ResetBindings() at the end (both BindingList&lt;T&gt;
        // members - public and protected respectively, reached via
        // reflection the same way EnableDoubleBuffering reaches a
        // protected Control property). Without this, each individual
        // Clear()/Add()/Remove() call gets picked up and repainted
        // immediately - including whatever intermediate state
        // DataGridView reacts to along the way (e.g. the list being
        // briefly empty mid-reorder) - which is what previously showed up
        // as the first row flashing "selected". Protected (not private)
        // so a subclass's own editing operations can share this exact
        // same batching, not just this class's own sorting.
        protected void ApplyBatchedDataSourceChange(IList list, Action mutate)
        {
            OnApplyingBatchedDataSourceChange();

            object dataSource = DataSource;
            DataSourceReflectionInfo reflectionInfo = GetDataSourceReflectionInfo(dataSource.GetType());
            PropertyInfo raiseEventsProperty = reflectionInfo.RaiseListChangedEventsProperty;
            bool canSuppressEvents =
                raiseEventsProperty != null && raiseEventsProperty.CanRead && raiseEventsProperty.CanWrite;
            bool previousRaiseEvents = true;

            if (canSuppressEvents)
            {
                previousRaiseEvents = (bool)raiseEventsProperty.GetValue(dataSource);
                raiseEventsProperty.SetValue(dataSource, false);
            }

            _isApplyingInternalDataChange = true;

            try
            {
                mutate();
            }
            finally
            {
                // Stays true through ResetBindings() below, not just the
                // mutation above - ResetBindings() is what actually raises
                // the (suppressed-until-now) Reset notification, so it
                // re-enters OnDataBindingComplete on this same call stack.
                // Flipping the flag off before that would let that
                // reentrant call run the "fresh bind" bookkeeping again
                // mid-change - wiping out the glyph/sort-order CycleSort
                // just set, and calling ClearSelection() / CurrentCell =
                // null while the grid is mid-reset, which is exactly the
                // kind of reentrant state change DataGridView can throw
                // InvalidOperationException over.
                if (canSuppressEvents)
                {
                    raiseEventsProperty.SetValue(dataSource, previousRaiseEvents);
                    reflectionInfo.ResetBindingsMethod?.Invoke(dataSource, null);
                }

                _isApplyingInternalDataChange = false;
            }

            ClearSelection();
            CurrentCell = null;
        }

        // Hook for a subclass to drop any of its own per-row visual state
        // (DataGridView overrides this to forget its hovered delete-row
        // index) right before a batched change runs - rows can shift
        // position without the mouse moving (e.g. the hovered row itself
        // gets deleted, or a sort reorders things underneath the cursor),
        // and no mouse-move means no fresh OnCellMouseEnter/Leave to
        // correct a now-stale index. This class itself has no such state
        // of its own, so the base implementation is empty.
        protected virtual void OnApplyingBatchedDataSourceChange()
        {
        }

        private List<object> BuildSortedOrder(int columnIndex, SortOrder sortOrder)
        {
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            List<KeyValuePair<object, object>> itemsWithValues = new List<KeyValuePair<object, object>>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem != null)
                {
                    itemsWithValues.Add(new KeyValuePair<object, object>(
                        row.DataBoundItem,
                        row.Cells[columnIndex].Value));
                }
            }

            // A custom comparer (see SetSortComparer) always compares in
            // plain ascending order - direction is applied the same way
            // regardless of whether the default comparison or a
            // registered one is doing the actual comparing, so a
            // consumer's comparer never has to care which one is active.
            _columnSortComparers.TryGetValue(Columns[columnIndex], out IComparer customComparer);

            itemsWithValues.Sort((a, b) => customComparer != null
                ? customComparer.Compare(a.Value, b.Value) * direction
                : CompareCellValues(a.Value, b.Value, direction));

            List<object> ordered = new List<object>(itemsWithValues.Count);

            foreach (KeyValuePair<object, object> pair in itemsWithValues)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }

        // Rows added after the last bind/reset (e.g. a new row the user just
        // typed into) sort after every row that was present at that
        // snapshot, rather than colliding at index -1.
        private List<object> BuildOriginalOrder()
        {
            List<KeyValuePair<object, int>> itemsWithIndex = new List<KeyValuePair<object, int>>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem == null)
                {
                    continue;
                }

                int index = _originalOrder.IndexOf(row.DataBoundItem);
                itemsWithIndex.Add(new KeyValuePair<object, int>(
                    row.DataBoundItem,
                    index >= 0 ? index : _originalOrder.Count));
            }

            itemsWithIndex.Sort((a, b) => a.Value.CompareTo(b.Value));

            List<object> ordered = new List<object>(itemsWithIndex.Count);

            foreach (KeyValuePair<object, int> pair in itemsWithIndex)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }

        private static int CompareCellValues(object valueX, object valueY, int direction)
        {
            if (valueX == null && valueY == null)
            {
                return 0;
            }

            if (valueX == null)
            {
                return -1 * direction;
            }

            if (valueY == null)
            {
                return 1 * direction;
            }

            if (valueX is IComparable comparable)
            {
                try
                {
                    return comparable.CompareTo(valueY) * direction;
                }
                catch (ArgumentException)
                {
                    // Falls through to the string comparison below when the
                    // two values aren't directly comparable to each other
                    // (e.g. different underlying cell types).
                }
            }

            return string.Compare(
                Convert.ToString(valueX),
                Convert.ToString(valueY),
                StringComparison.CurrentCultureIgnoreCase) * direction;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ApplyStyles();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyStyles();
        }

        private void ApplyStyles()
        {
            // Flat gray when disabled - every control in this library mutes
            // this same way, no exceptions for a control's own fixed/
            // accent colors (see ListView's SetEnabledStyle-style
            // handling, and UIProgressBarFactory's disabled fill).
            Color rowForeColor = Enabled ? _rowForeColor : UIColors.DisabledGray;
            Color headerForeColor = Enabled ? _headerForeColor : UIColors.DisabledGray;
            Color selectionBackColor = Enabled ? SelectionBackColor : UIColors.DisabledGray;
            Color selectionForeColor = UIColors.GetContrastingForeColor(selectionBackColor);

            using (var headerFont = new Font(Font, FontStyle.Bold))
            {
                ColumnHeadersDefaultCellStyle.BackColor = _headerBackColor;
                ColumnHeadersDefaultCellStyle.ForeColor = headerForeColor;
                ColumnHeadersDefaultCellStyle.Font = headerFont;
                ColumnHeadersDefaultCellStyle.SelectionBackColor = _headerBackColor;
                ColumnHeadersDefaultCellStyle.SelectionForeColor = headerForeColor;
            }

            DefaultCellStyle.BackColor = _rowBackColor;
            DefaultCellStyle.ForeColor = rowForeColor;
            DefaultCellStyle.SelectionBackColor = selectionBackColor;
            DefaultCellStyle.SelectionForeColor = selectionForeColor;

            AlternatingRowsDefaultCellStyle.BackColor = _alternateRowBackColor;
            AlternatingRowsDefaultCellStyle.ForeColor = rowForeColor;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBackColor;
            AlternatingRowsDefaultCellStyle.SelectionForeColor = selectionForeColor;

            Invalidate();
        }

        private void EnableDoubleBuffering()
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true, null);
        }

        protected static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        protected static Color Lighten(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }

        // See OnColumnAdded - a plain DataGridViewTextBoxCell whose accessible
        // "default action" is blank instead of Windows' own generic "Edit",
        // except on the "type here to add a row" placeholder, which gets one
        // that actually says what it's for.
        private sealed class EditHintTextBoxCell : DataGridViewTextBoxCell
        {
            protected override AccessibleObject CreateAccessibilityInstance()
            {
                return new EditHintCellAccessibleObject(this);
            }

            private sealed class EditHintCellAccessibleObject : DataGridViewTextBoxCellAccessibleObject
            {
                public EditHintCellAccessibleObject(DataGridViewCell owner) : base(owner)
                {
                }

                public override string DefaultAction
                {
                    get
                    {
                        // OwningRow.IsNewRow would be the obvious way to
                        // check this, but OwningRow is null whenever the
                        // cell belongs to a shared row - an internal
                        // DataGridView memory optimization for rows that
                        // aren't current/selected/displayed, which most
                        // rows are most of the time. RowIndex still works
                        // for a shared row, so that's compared against
                        // NewRowIndex (the placeholder's row index, or -1
                        // if there isn't one) instead.
                        System.Windows.Forms.DataGridView dataGridView = Owner.DataGridView;

                        return dataGridView != null && Owner.RowIndex == dataGridView.NewRowIndex
                            ? UIStrings.Get("DataGridView.AddRow")
                            : string.Empty;
                    }
                }
            }
        }
    }
}
