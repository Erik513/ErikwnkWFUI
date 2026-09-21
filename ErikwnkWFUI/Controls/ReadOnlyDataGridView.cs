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
    /// <see cref="UIStyles.DataGridViews.CreateReadOnlyStandard"/>, which hands back
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
        private Color _borderColorOverride;
        private bool _borderColorIsOverridden;
        private bool _allowColumnReordering = true;
        private readonly HashSet<int> _nonReorderableColumns = new HashSet<int>();
        private Color _columnReorderIndicatorColorOverride;
        private bool _columnReorderIndicatorColorIsOverridden;
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private int _pendingReorderColumnIndex = -1;
        private int _pendingReorderStartX;
        private bool _allowColumnResizing = true;
        private readonly HashSet<int> _nonResizableColumns = new HashSet<int>();
        private int _minimumColumnWidth = DefaultMinimumColumnWidth;
        private bool _isResizingColumn;
        private int _resizeColumnIndex = -1;
        private int _resizeStartX;
        private int _resizeStartWidth;
        private bool _suppressNextHeaderClickSort;

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

        /// <summary>
        /// Color of the whole control's outer frame only - the gridlines
        /// BETWEEN cells (this control's own <c>GridColor</c>) stay a fixed
        /// neutral color regardless, so <c>CreatePrimary</c> doesn't turn
        /// every row separator into a wash of accent color too; only
        /// ListView's equivalent <see cref="ListView.BorderColor"/> drives
        /// both its frame and its header cell dividers with one color,
        /// since ListView never draws anything resembling gridlines
        /// between actual rows in the first place. Defaults to
        /// <see cref="UIColors.BorderMedium"/> - a fixed, neutral border
        /// regardless of the current accent, matching this control's
        /// original, unconditional look
        /// (<see cref="Factories.UIDataGridViewFactory.CreateStandard"/>/
        /// <see cref="Factories.UIDataGridViewFactory.CreateReadOnlyStandard"/>
        /// still get exactly that, unchanged).
        /// <see cref="Factories.UIDataGridViewFactory.CreatePrimary"/> sets
        /// this to <see cref="UIColors.Primary"/> instead, the same way
        /// ListView's own CreatePrimary does.
        /// </summary>
        public Color BorderColor
        {
            get => _borderColorIsOverridden ? _borderColorOverride : UIColors.BorderMedium;
            set
            {
                _borderColorOverride = value;
                _borderColorIsOverridden = true;
                ApplyStyles();
            }
        }

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get => _rowBackColor;
            set
            {
                _rowBackColor = value;
                _alternateRowBackColor = UIColors.Darken(value, 5);
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
        /// Master switch for whether ANY column can be resized by dragging
        /// its header border - mirrors <see cref="ListView.AllowColumnResizing"/>.
        /// Per-column overrides via <see cref="SetColumnResizable"/> still
        /// apply among whichever columns this allows; setting this false
        /// overrides all of them. Defaults to true.
        /// </summary>
        /// <remarks>
        /// Resizing is fully hand-rolled (OnCellMouseDown/OnCellMouseMove
        /// below), same as reordering and for a related but different
        /// reason: <see cref="System.Windows.Forms.DataGridView.AllowUserToResizeColumns"/>'s
        /// own native resize-drag was confirmed live to NOT repaint the
        /// column as it's being resized - only a thin guideline moves with
        /// the cursor, and the actual width is applied (and the grid
        /// repainted) once, on mouse-up. ListView's own column resize
        /// doesn't have this gap - it's backed by comctl32's own Header
        /// common control, which repaints continuously on its own, so
        /// there was nothing to hand-roll there. That native property is
        /// deliberately left false (see the constructor); this one governs
        /// the owned implementation instead. Per-column resizability is
        /// tracked separately (<see cref="_nonResizableColumns"/>), not via
        /// the existing native <see cref="DataGridViewColumn.Resizable"/> -
        /// that property's own getter falls back to this control's
        /// (permanently false) AllowUserToResizeColumns whenever a column
        /// never had it explicitly set, which would otherwise make every
        /// ordinary column silently read back as "not resizable" the
        /// moment the native switch went false.
        /// </remarks>
        public bool AllowColumnResizing
        {
            get => _allowColumnResizing;
            set => _allowColumnResizing = value;
        }

        /// <summary>
        /// No column can be resized narrower than this - mirrors
        /// <see cref="ListView.MinimumColumnWidth"/>. Whichever of this or
        /// the column's own native <see cref="DataGridViewColumn.MinimumWidth"/>
        /// is larger actually applies.
        /// </summary>
        /// <remarks>
        /// A column's native MinimumWidth defaults to a few pixels - fine
        /// for WinForms' own resize-drag, which always keeps the border
        /// wherever the column's own edge currently is, but this control's
        /// hand-rolled one (see AllowColumnResizing's own remarks on why)
        /// finds that edge by proximity, within a small pixel radius of
        /// each border. Once a column got down anywhere near that native
        /// floor, the radii around its own left and right borders started
        /// overlapping, so a drag meant for one of them intermittently
        /// grabbed the other instead - confirmed live as "resizing gets
        /// stuck around 5-10px, and never responds correctly again from
        /// there." This default is comfortably larger than that radius on
        /// either side, so the overlap that causes it is never reachable.
        /// </remarks>
        public int MinimumColumnWidth
        {
            get => _minimumColumnWidth;
            set => _minimumColumnWidth = Math.Max(1, value);
        }

        private const int DefaultMinimumColumnWidth = 40;

        /// <summary>
        /// Configurable per column, independent of
        /// <see cref="AllowColumnResizing"/>: a column can be locked at its
        /// current width entirely (e.g. <see cref="DataGridView"/>'s own
        /// delete-row column) while the rest can still be freely resized.
        /// Mirrors <see cref="ListView.SetColumnResizable"/>.
        /// </summary>
        public void SetColumnResizable(int columnIndex, bool resizable)
        {
            if (resizable)
            {
                _nonResizableColumns.Remove(columnIndex);
            }
            else
            {
                _nonResizableColumns.Add(columnIndex);
            }
        }

        /// <summary>Mirrors <see cref="ListView.IsColumnResizable"/> - see <see cref="AllowColumnResizing"/>/<see cref="SetColumnResizable"/>.</summary>
        public bool IsColumnResizable(int columnIndex)
        {
            return _allowColumnResizing && !_nonResizableColumns.Contains(columnIndex);
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
            _alternateRowBackColor = UIColors.Darken(_rowBackColor, 5);

            BackgroundColor = UIColors.BackgroundDark;

            // Fixed, independent of BorderColor - only the OUTER frame
            // (see OnPaint below) follows BorderColor/the accent in
            // CreatePrimary; the gridlines BETWEEN cells stay this same
            // neutral color regardless, so CreatePrimary doesn't turn every
            // row separator into a wash of accent color too.
            GridColor = UIColors.BorderMedium;

            // BorderStyle stays None regardless - this control draws its
            // own outer frame instead (see OnPaint below), the same way
            // ListView draws its own instead of using a native BorderStyle.
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersVisible = false;

            // The header row's own height is not something this control
            // wants a user casually dragging - EnableResizing (the native
            // default) also has no minimum-height floor of its own, so it
            // can be dragged down to where header text starts clipping.
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

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

            // Same treatment, see AllowColumnResizing's own remarks - its
            // native resize-drag doesn't repaint the column live while
            // dragging, only on mouse-up, so this is hand-rolled instead
            // (same OnCellMouseDown/OnCellMouseMove below).
            AllowUserToResizeColumns = false;

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

            // Regression test: resizing (or even just pressing down right
            // on top of) a column border still counts as a "click" on
            // whichever header cell that border belongs to as far as
            // DataGridView's own click detection is concerned - without
            // this, finishing a resize drag by releasing the mouse also
            // silently cycled that column's sort, since the resize itself
            // is hand-rolled (OnCellMouseDown/OnCellMouseMove) and never
            // told DataGridView's own click machinery to stay out of it.
            if (_suppressNextHeaderClickSort)
            {
                _suppressNextHeaderClickSort = false;
                return;
            }

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
            _isResizingColumn = false;
            _suppressNextHeaderClickSort = false;

            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0)
            {
                return;
            }

            // DataGridViewCellMouseEventArgs.X/Y are relative to the CELL,
            // not the control - PointToClient(Cursor.Position) sidesteps
            // that entirely rather than adding the cell's own display
            // rectangle back in.
            int controlX = PointToClient(Cursor.Position).X;
            int logicalX = controlX + HorizontalScrollingOffset;

            // A border always wins over starting a reorder drag, whether
            // or not it turns out resizable - a locked column's border
            // should do nothing at all, not fall through into moving the
            // column instead.
            if (TryGetColumnAtBorder(logicalX, out DataGridViewColumn borderColumn))
            {
                // The mouse still went down and (typically) back up on the
                // same header cell either way, which is exactly what
                // OnColumnHeaderMouseClick's own click detection is built
                // to catch - pressing right on a border was never actually
                // aiming to sort that column, whether or not a real resize
                // ends up happening from here.
                _suppressNextHeaderClickSort = true;

                if (IsColumnResizable(borderColumn))
                {
                    _isResizingColumn = true;
                    _resizeColumnIndex = borderColumn.Index;
                    _resizeStartX = controlX;
                    _resizeStartWidth = borderColumn.Width;

                    // Keeps this tracking correctly even if a fast drag
                    // carries the cursor outside the grid's own bounds -
                    // without it, MouseMove/MouseUp simply stop arriving
                    // once the cursor leaves this control's screen area,
                    // leaving the resize stuck "in progress" until some
                    // unrelated later click happens to reset it.
                    Capture = true;
                }

                return;
            }

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

        protected override void OnCellMouseMove(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseMove(e);

            if (_isResizingColumn)
            {
                ApplyLiveColumnResize();
                return;
            }

            if (e.RowIndex != -1)
            {
                return;
            }

            int controlX = PointToClient(Cursor.Position).X;

            if (_pendingReorderColumnIndex >= 0 && !_isDraggingColumn &&
                Math.Abs(controlX - _pendingReorderStartX) >= SystemInformation.DragSize.Width)
            {
                int columnIndex = _pendingReorderColumnIndex;
                _pendingReorderColumnIndex = -1;
                BeginColumnDragDrop(columnIndex);
                return;
            }

            // Hover-only cursor hint for a resizable border - the native
            // resize cursor is gone along with the native resize itself
            // (see AllowUserToResizeColumns in the constructor), so this
            // control has to show its own now, the same way it already
            // draws its own reorder insertion line instead of a native one.
            bool overResizableBorder = TryGetColumnAtBorder(controlX + HorizontalScrollingOffset, out DataGridViewColumn hoveredColumn) &&
                IsColumnResizable(hoveredColumn);
            Cursor = overResizableBorder ? Cursors.VSplit : Cursors.Default;
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

            if (_isResizingColumn)
            {
                _isResizingColumn = false;
                _resizeColumnIndex = -1;
                Capture = false;
            }
        }

        // Applies the column's new width directly, on every single
        // mouse-move tick while a resize is in progress - unlike
        // DataGridView's own native resize-drag (confirmed live to only
        // move a guideline and apply the real width once, on mouse-up),
        // setting Width here goes through the ordinary property-changed
        // path (not the native drag's own internal one), which repaints
        // normally on each call - there's no DoDragDrop-style blocking
        // loop starving the message queue the way there is for the
        // reorder line, so no separate forced Update() is needed here the
        // way OnDragOver's own live-repaint fix needed one.
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
            const int resizeGripWidth = 5;
            int cumulativeWidth = 0;
            foreach (DataGridViewColumn candidate in GetColumnsInDisplayOrder())
            {
                cumulativeWidth += candidate.Width;
                if (Math.Abs(x - cumulativeWidth) <= resizeGripWidth)
                {
                    column = candidate;
                    return true;
                }
            }

            column = null;
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

        // BorderStyle is None - this draws a light frame around the whole
        // control instead, matching ListView's own BorderColor and using
        // the exact same technique. A first attempt drew this via a plain
        // OnPaint override instead, on the theory that DataGridView (fully
        // managed, unlike ListView's wrapped native comctl32 control)
        // wouldn't have ListView's own reason for needing the lower-level
        // approach - confirmed wrong live: scrolling still visibly made
        // the border vanish/redraw incorrectly, meaning DataGridView's own
        // scroll handling also shifts existing pixels natively (likely
        // ScrollWindowEx-style, same as ListView's) rather than going
        // through OnPaint for every scrolled frame. Selecting a cell had
        // the same problem for the same underlying reason: an edge cell's
        // own repaint doesn't guarantee this override runs again for
        // exactly that same paint cycle in every case. Draws straight onto
        // the client DC right after WM_PAINT/the scroll messages finish,
        // same as ListView.
        private const int WM_PAINT = 0x000F;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_MOUSEWHEEL = 0x020A;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(IntPtr hWndLock);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                // base.WndProc below runs this control's own scroll
                // handling SYNCHRONOUSLY, including whatever partial
                // pixel-shifting repaint it does internally - by the time
                // control returns here, that frame (with the border
                // missing/stale along the scrolled edge) has already
                // reached the screen once. LockWindowUpdate blocks ANY
                // pixel of this window from reaching the screen at the GDI
                // level while locked, regardless of how the control
                // internally decides to paint during the scroll, so
                // nothing incorrect can flash through no matter the
                // mechanism - then a forced synchronous repaint (Update(),
                // not just Invalidate()) once unlocked means the very
                // first frame the user actually sees is the corrected one.
                // Exactly ListView's own WndProc technique for this same
                // class of problem.
                LockWindowUpdate(Handle);
                try
                {
                    base.WndProc(ref m);
                }
                finally
                {
                    LockWindowUpdate(IntPtr.Zero);
                }

                Invalidate();
                Update();
                return;
            }

            base.WndProc(ref m);

            if (m.Msg != WM_PAINT || ClientSize.Width <= 1 || ClientSize.Height <= 1)
            {
                return;
            }

            // Redrawn on every real WM_PAINT, deliberately - matches
            // ListView's own reasoning: this control can still repaint
            // parts of the border's own pixels through paths this class
            // doesn't get a hook into at all (e.g. its own native focus
            // rectangle around a selected cell), so a "only when something
            // relevant changed" version of this would leave the border
            // silently wrong after a selection change, same as ListView's
            // own history already found the hard way.
            IntPtr dc = GetDC(Handle);
            if (dc == IntPtr.Zero)
            {
                return;
            }

            try
            {
                using (Graphics g = Graphics.FromHdc(dc))
                using (Pen pen = new Pen(BorderColor))
                {
                    // ClientSize, not Width/Height - Width/Height are this
                    // control's FULL outer bounds, which include a native
                    // scrollbar's own strip when one is visible. A border
                    // drawn at Width - 1 would land exactly under that
                    // scrollbar, which then paints over it - invisible,
                    // not missing.
                    int right = ClientSize.Width - 1;
                    int bottom = ClientSize.Height - 1;
                    g.DrawRectangle(pen, 0, 0, right, bottom);
                }
            }
            finally
            {
                ReleaseDC(Handle, dc);
            }
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
