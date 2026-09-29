using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
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
        private readonly ThemeColor _selectionBackColor = new ThemeColor(() => UIColors.BorderLight);
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);
        private readonly ColumnFeatureSwitch _columnReordering = new ColumnFeatureSwitch();
        private readonly ThemeColor _columnReorderIndicatorColor = new ThemeColor(() => UIColors.BorderLight);
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private int _pendingReorderColumnIndex = -1;
        private int _pendingReorderStartX;
        private readonly ColumnFeatureSwitch _columnResizing = new ColumnFeatureSwitch();
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
        private DataGridViewColumn _enumerationColumn;
        private readonly ToolTip _copyConfirmationToolTip = new ToolTip();

        // Matches Controls.ContextMenuStrip's own CreateStandard default.
        // BuildContextMenu reads this once, when it builds the menu
        // in the constructor. The property setter also pushes a later
        // change straight into whichever menu is currently assigned (found
        // via the inherited ContextMenuStrip property) - needed because
        // UIDataGridViewFactory.CreateReadOnlyPrimary sets this through an
        // object initializer, which only runs after the constructor already
        // built the menu - same pattern as ListView.ContextMenuSelectionColor.
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);
        // Same shape as ListView's own menu: "Copy selection" and "Copy all"
        // are submenu parents, each holding the plain action and "As table"
        // (the same content plus a header row) - "As table" appears twice,
        // so this is 6 items for 3 distinct pieces of text.
        private ToolStripMenuItem _contextMenuCopySelectionItem;
        private ToolStripMenuItem _contextMenuCopySelectionWithHeaderItem;
        private ToolStripMenuItem _contextMenuCopyAllItem;
        private ToolStripMenuItem _contextMenuCopyAllWithHeaderItem;
        private ToolStripMenuItem _contextMenuSelectAllItem;

        /// <summary>
        /// Background of a hovered/selected item in this grid's own
        /// right-click context menu. Defaults to a fixed neutral gray,
        /// matching <see cref="Controls.ContextMenuStrip"/>'s own
        /// CreateStandard default; the factories' CreatePrimary variants set
        /// this to <see cref="UIColors.Primary"/> instead, same pattern as
        /// <see cref="BorderColor"/>.
        /// </summary>
        public Color ContextMenuSelectionColor
        {
            get { return _contextMenuSelectionColor.Value; }
            set
            {
                _contextMenuSelectionColor.Set(value);

                if (ContextMenuStrip is ContextMenuStrip menu)
                {
                    menu.SelectionBackColor = value;
                }
            }
        }

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
        /// Color of each column header cell's own border only - the
        /// gridlines BETWEEN data cells (this control's own
        /// <c>GridColor</c>) stay a fixed neutral color regardless, so
        /// <c>CreatePrimary</c> doesn't turn every row separator into a wash
        /// of accent color too. There used to also be a hand-drawn outer
        /// frame around the whole control following this same color, but a
        /// real, native in-place editing control (a child window, always on
        /// top of anything this control's own OnPaint draws) sitting at a
        /// grid edge - the first/last row or column - hid it completely
        /// while editing that cell, with no way to draw around a window
        /// that's actually there; removed rather than left broken. Only
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
            get => _borderColor.Value;
            set
            {
                _borderColor.Set(value);
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
        /// Background color of a selected cell/row. Defaults to a fixed,
        /// neutral gray (<see cref="UIColors.BorderLight"/>), not the
        /// current accent - <see cref="Factories.UIDataGridViewFactory.CreatePrimary"/>/
        /// <see cref="Factories.UIDataGridViewFactory.CreateReadOnlyPrimary"/>
        /// set this to <see cref="UIColors.Primary"/> explicitly instead,
        /// same pattern as ListView's SelectionOverlayColor.
        /// </summary>
        public Color SelectionBackColor
        {
            get => _selectionBackColor.Value;
            set
            {
                _selectionBackColor.Set(value);
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
            get => _columnReordering.AllowedByDefault;
            set => _columnReordering.AllowedByDefault = value;
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
            _columnReordering.SetAllowed(columnIndex, reorderable);
        }

        public bool IsColumnReorderable(int columnIndex)
        {
            return _columnReordering.IsAllowed(columnIndex);
        }

        /// <summary>
        /// Color of the vertical line the header shows while a column is
        /// being dragged to reorder it. Defaults to a fixed, neutral gray
        /// (<see cref="UIColors.BorderLight"/>), not the current accent -
        /// same pattern as <see cref="SelectionBackColor"/> and ListView's
        /// own ColumnReorderIndicatorColor.
        /// </summary>
        public Color ColumnReorderIndicatorColor
        {
            get => _columnReorderIndicatorColor.Value;
            set => _columnReorderIndicatorColor.Set(value);
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
        /// tracked separately (<see cref="_columnResizing"/>), not via
        /// the existing native <see cref="DataGridViewColumn.Resizable"/> -
        /// that property's own getter falls back to this control's
        /// (permanently false) AllowUserToResizeColumns whenever a column
        /// never had it explicitly set, which would otherwise make every
        /// ordinary column silently read back as "not resizable" the
        /// moment the native switch went false.
        /// </remarks>
        public bool AllowColumnResizing
        {
            get => _columnResizing.AllowedByDefault;
            set => _columnResizing.AllowedByDefault = value;
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
            _columnResizing.SetAllowed(columnIndex, resizable);
        }

        /// <summary>Mirrors <see cref="ListView.IsColumnResizable"/> - see <see cref="AllowColumnResizing"/>/<see cref="SetColumnResizable"/>.</summary>
        public bool IsColumnResizable(int columnIndex)
        {
            return _columnResizing.IsAllowed(columnIndex);
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

        /// <summary>
        /// How a copy, or any of DataGridView's own editing actions (cut,
        /// paste, clear, delete rows, insert row), is confirmed - the same
        /// three-way choice as <see cref="ListView.CopyConfirmation"/>
        /// (they share <see cref="CopyConfirmationStyle"/> and the same
        /// display logic - see <see cref="CopyConfirmationDisplay"/>),
        /// named more broadly here since this control has more than just
        /// copying to confirm. Defaults to <see cref="CopyConfirmationStyle.Toast"/>.
        /// </summary>
        public CopyConfirmationStyle ActionConfirmation { get; set; } = CopyConfirmationStyle.Toast;

        // Every copy (native Ctrl+C, or DataGridView's own context-menu
        // Copy/Cut, which both ultimately call this same virtual method)
        // goes through here - a single choke point for both showing the
        // confirmation and, if ShowEnumeration is on, keeping its column
        // out of the copied text. DataGridView's own override (its own
        // delete column, plus placeholder-row exclusion) wraps this same
        // method rather than duplicating any of it - deselects ITS
        // excluded cells first, then calls base (here), which deselects
        // the enumeration column on top before the real, native copy runs,
        // so SelectedCells.Count below is already correct for whichever of
        // the two (or both) actually applies.
        public override DataObject GetClipboardContent()
        {
            return ExcludingSelectedCells(cell => IsEnumerationColumn(cell.ColumnIndex), () =>
            {
                DataObject content = base.GetClipboardContent();
                ShowCopyConfirmation(SelectedCells.Count);
                return content;
            });
        }

        // Shared by both this override and DataGridView's own (delete
        // column + placeholder row) - only the exclusion predicate and the
        // action actually differ between them. `action` (not a hardcoded
        // call to base.GetClipboardContent() in here) is what lets
        // DataGridView's own override compose correctly: its own lambda's
        // "base.GetClipboardContent()" call resolves against DataGridView's
        // own base (this class), reaching the override just above - calling
        // that from inside a helper method living HERE would instead reach
        // straight past it to the native implementation, skipping the
        // enumeration-column exclusion entirely whenever both this and
        // ShowEnumeration are in play together.
        protected T ExcludingSelectedCells<T>(Predicate<DataGridViewCell> exclude, Func<T> action)
        {
            List<DataGridViewCell> excludedCells = new List<DataGridViewCell>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (exclude(cell))
                {
                    excludedCells.Add(cell);
                }
            }

            if (excludedCells.Count == 0)
            {
                return action();
            }

            foreach (DataGridViewCell cell in excludedCells)
            {
                cell.Selected = false;
            }

            try
            {
                return action();
            }
            finally
            {
                foreach (DataGridViewCell cell in excludedCells)
                {
                    cell.Selected = true;
                }
            }
        }

        private void ShowCopyConfirmation(int copiedCellCount)
        {
            if (copiedCellCount <= 0)
            {
                return;
            }

            ShowActionConfirmation(GetCopyConfirmationMessage(copiedCellCount));
        }

        private static string GetCopyConfirmationMessage(int copiedCellCount)
        {
            return copiedCellCount == 1
                ? UIStrings.Get("DataGridView.CellCopied")
                : string.Format(UIStrings.Get("DataGridView.CellsCopied"), copiedCellCount);
        }

        /// <summary>
        /// Shows <paramref name="message"/> via <see cref="ActionConfirmation"/> -
        /// the same mechanism <see cref="GetClipboardContent"/> uses for its
        /// own "copied" message, exposed so <see cref="DataGridView"/> can
        /// confirm its own editing actions (cut, paste, clear, delete rows,
        /// insert row) the same way, instead of each carrying its own copy
        /// of this same three-way Toast/ToolTip/None dispatch.
        /// </summary>
        protected void ShowActionConfirmation(string message)
        {
            if (_suppressActionConfirmation)
            {
                return;
            }

            OnActionConfirmation(message);
        }

        // Its own virtual seam (rather than making ShowActionConfirmation
        // itself virtual) so a test subclass can spy on exactly which
        // messages actually got displayed - i.e. only the ones that survive
        // the suppression check above - without being able to observe calls
        // RunWithSuppressedActionConfirmation swallowed.
        protected virtual void OnActionConfirmation(string message)
        {
            CopyConfirmationDisplay.Show(ActionConfirmation, message, this, _copyConfirmationToolTip);
        }

        private bool _suppressActionConfirmation;

        /// <summary>
        /// Runs <paramref name="action"/> with every <see cref="ShowActionConfirmation"/>
        /// call inside it silenced - for composite operations like Cut
        /// (copy, then clear) where the individual steps' own "copied"/
        /// "cleared" messages would otherwise fire before the composite's
        /// own, single "cut" message does.
        /// </summary>
        protected void RunWithSuppressedActionConfirmation(Action action)
        {
            bool previous = _suppressActionConfirmation;
            _suppressActionConfirmation = true;
            try
            {
                action();
            }
            finally
            {
                _suppressActionConfirmation = previous;
            }
        }

        /// <summary>
        /// Whether an optional "#" row-number column is shown, pinned as
        /// the LEFTMOST column regardless of what else is added afterward -
        /// the mirror image of <see cref="DataGridView.ShowDeleteRowColumn"/>'s
        /// own rightmost pinning. Off by default - a consumer opts in any
        /// time after construction. Each row's number is always its
        /// CURRENT position (1-based, top to bottom): sorting, inserting,
        /// or deleting rows renumbers automatically. The column IS
        /// sortable (unlike the delete column) - clicking it sorts by
        /// whatever position each row held right before the click, so
        /// ascending is a no-op (already in that order), descending
        /// reverses the current row order, and a third click restores the
        /// original bind order, same as any other column's cycle. Its
        /// width is fixed (not user-resizable, matching the delete
        /// column), but automatically wide enough for however many digits
        /// the current row count needs - 150 rows gets a column sized for
        /// "150", not padded for some arbitrary maximum.
        /// </summary>
        public bool ShowEnumeration
        {
            get => _enumerationColumn != null;
            set
            {
                if (value == (_enumerationColumn != null))
                {
                    return;
                }

                if (value)
                {
                    DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
                    {
                        Name = "__enumeration",
                        HeaderText = UIStrings.Get("DataGridView.EnumerationHeader"),
                        ReadOnly = true,
                        Resizable = DataGridViewTriState.False,
                        ValueType = typeof(int)
                    };

                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    column.HeaderCell.ToolTipText = UIStrings.Get("DataGridView.RowNumber");

                    // Assigned before Add() - OnColumnAdded below checks
                    // this field to recognize the column as it comes in,
                    // same pattern as DataGridView's own delete column.
                    _enumerationColumn = column;
                    Columns.Add(column);

                    // Pinned leftmost (see OnColumnAdded below) and never
                    // user-resizable, same reasoning as the delete column -
                    // Resizable above is a fixed native width hint, but
                    // resizing itself is hand-rolled and tracked
                    // separately (see AllowColumnResizing's own remarks),
                    // so it's excluded here too.
                    SetColumnReorderable(column.Index, false);
                    SetColumnResizable(column.Index, false);

                    RenumberEnumeration();
                }
                else
                {
                    Columns.Remove(_enumerationColumn);
                    _enumerationColumn = null;
                }
            }
        }

        /// <summary>
        /// Whether <paramref name="columnIndex"/> is the optional
        /// enumeration column (see <see cref="ShowEnumeration"/>) - exposed
        /// so <see cref="DataGridView"/> can exclude it from copy/paste/the
        /// right-click menu the same way it already excludes its own
        /// delete-row column, without the backing field itself needing to
        /// be any more visible than that.
        /// </summary>
        protected bool IsEnumerationColumn(int columnIndex)
        {
            return _enumerationColumn != null && columnIndex == _enumerationColumn.Index;
        }

        private const int EnumerationColumnPadding = 20;

        // The displayed number for each row is always its CURRENT position -
        // recomputed here from scratch rather than tracked incrementally,
        // since every operation that could change row order or count (a
        // fresh bind, a sort, DataGridView's own paste/insert/delete) already
        // funnels through either this property's own setter (toggling the
        // column on) or OnDataBindingComplete below exactly once. IsNewRow
        // (the "type here to add a row" placeholder, only ever present on a
        // DataGridView with AllowUserToAddRows enabled) is left blank, same
        // as the delete column shows nothing there - nothing to number yet.
        private void RenumberEnumeration()
        {
            if (_enumerationColumn == null)
            {
                return;
            }

            int columnIndex = _enumerationColumn.Index;

            int realRowCount = 0;
            foreach (DataGridViewRow row in Rows)
            {
                if (!row.IsNewRow)
                {
                    realRowCount++;
                }
            }

            // Counts down from the row count instead of up from 1 while
            // this column is itself the active DESCENDING sort column -
            // otherwise a click that reverses the row order (confirmed live
            // that it does - see CycleSort/ReorderDataSource) had no visible
            // effect on the numbers themselves, since renumbering to match
            // whatever the new top-to-bottom order is always produced the
            // same 1..N sequence regardless of which direction the rows
            // actually got reordered in. Every other case (ascending, no
            // active sort, or a DIFFERENT column being sorted) still just
            // counts up - this column's job there is "which visual row is
            // this", unrelated to what's being sorted.
            bool countDown = columnIndex == _sortedColumnIndex && _sortOrder == SortOrder.Descending;
            int number = countDown ? realRowCount : 1;

            foreach (DataGridViewRow row in Rows)
            {
                if (row.IsNewRow)
                {
                    row.Cells[columnIndex].Value = null;
                    continue;
                }

                row.Cells[columnIndex].Value = number;
                number += countDown ? -1 : 1;
            }

            _enumerationColumn.Width = GetEnumerationColumnWidth(Rows.Count);
        }

        // Rows.Count, not the real (possibly one-smaller, once the
        // placeholder is excluded) count of numbered rows - the difference
        // is at most one digit's worth of slack, and this avoids needing to
        // special-case AllowUserToAddRows here just to shave that off.
        private int GetEnumerationColumnWidth(int rowCount)
        {
            int digitCount = Math.Max(1, rowCount.ToString().Length);
            string widest = new string('9', digitCount);

            using (Font font = new Font(Font, FontStyle.Bold))
            {
                Size textSize = TextRenderer.MeasureText(widest, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                return textSize.Width + EnumerationColumnPadding;
            }
        }

        public ReadOnlyDataGridView()
        {
            _alternateRowBackColor = UIColors.Darken(_rowBackColor, 5);

            BackgroundColor = UIColors.BackgroundDark;

            // Fixed, independent of BorderColor - only each header cell's
            // own border (see DrawHeaderCellBorder below) follows
            // BorderColor/the accent in CreatePrimary; the gridlines
            // BETWEEN cells stay this same neutral color regardless, so
            // CreatePrimary doesn't turn every row separator into a wash of
            // accent color too.
            GridColor = UIColors.BorderMedium;

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
            // for it here (see DragDropSupport for why it's apartment-state
            // gated). OnCellMouseDown below checks AllowDrop itself and
            // never arms a drag if this never got set.
            DragDropSupport.EnableDropIfSta(this);

            // Without this, resizing the control (e.g. its containing Form
            // being resized, if this is docked/anchored to it) only
            // invalidates the newly-exposed strip, the standard WinForms
            // default - everything already visible before the resize is
            // assumed still valid and left unpainted until something else
            // happens to invalidate it, which can leave stale header-border
            // content sitting at its old position/size for a moment.
            // ListView already sets this same style for the same reason
            // (see its own SetStyle call); this control never had it.
            SetStyle(ControlStyles.ResizeRedraw, true);

            EnableDoubleBuffering();
            ApplyStyles();

            MouseDown += HandleMouseDown;
            MouseDown += HandleContextMenuMouseDown;

            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;
            ContextMenuStrip = BuildContextMenu();
        }

        // Which cell the context menu is about to act on - captured at
        // right-click time (see OnCellMouseDown/HandleContextMenuMouseDown),
        // read by the menu's own Opening handler and, in DataGridView, by
        // its row-scoped items. _contextMenuRowWasPlaceholder is still
        // needed as a snapshot, not just a live re-check, because
        // NewRowIndex itself can move between the click and whenever the
        // menu's Opening handler or an item's click actually runs (e.g. the
        // click itself may already have advanced it).
        protected int _contextMenuRowIndex = -1;
        protected int _contextMenuColumnIndex = -1;
        protected bool _contextMenuRowWasPlaceholder;

        // The one, single way this class family ever decides "is this row
        // the 'type here to add a row' placeholder, not real data" - every
        // command that must only ever act on real rows (copy, cut, clear,
        // paste, delete, insert-above/below, the context menu's own
        // enable-state) goes through this, never IsNewRow/DataBoundItem/a
        // list.Count comparison directly. NewRowIndex is WinForms' own
        // authoritative answer to "which row index is currently the
        // placeholder" (-1 when AllowUserToAddRows is off, i.e. always on a
        // read-only grid) - unlike IsNewRow or DataBoundItem, which can each
        // disagree about a row that's mid-way through becoming real (e.g.
        // merely tabbing or drag-selecting through the placeholder, with
        // nothing typed into it, already makes WinForms call
        // IBindingList.AddNew() on the bound list on its own), NewRowIndex
        // stays correct through all of that. A previous version used three
        // different, deliberately non-interchangeable checks for this
        // instead, picked per call site by timing - that fragmentation
        // caused real bugs (paste crashes, vanishing/duplicate rows,
        // insert-above/below silently no-opping), which is the whole reason
        // this exists now.
        protected bool IsPlaceholderRowIndex(int rowIndex)
        {
            return rowIndex >= 0 && rowIndex == NewRowIndex;
        }

        // A column whose cells hold no real data of the bound item's own -
        // the optional enumeration number here, plus DataGridView's
        // delete-row button (see its override) - excluded everywhere a
        // copy/paste/right-click action would otherwise treat it like an
        // ordinary data column. ReadOnly alone only blocks editing, none of
        // these.
        protected virtual bool IsSystemColumn(int columnIndex)
        {
            return IsEnumerationColumn(columnIndex);
        }

        private bool IsRowSelected(int rowIndex)
        {
            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex == rowIndex)
                {
                    return true;
                }
            }

            return false;
        }

        // Right-clicking a row that isn't already part of the current
        // selection replaces it with just that row - otherwise the context
        // menu's row-scoped actions (Copy here; Cut, Delete selected rows,
        // ... in DataGridView) would silently apply to whatever was
        // selected before, not the row actually under the cursor. A row
        // already part of a larger selection is left alone, so right-
        // clicking within an existing multi-row selection keeps it intact.
        // A system column's cell never triggers this (the menu itself is
        // cancelled there - see the Opening handlers).
        private void CaptureRightClickTarget(
            DataGridViewCellMouseEventArgs e, bool rowWasSelectedBeforeThisClick, bool rowWasPlaceholderBeforeThisClick)
        {
            _contextMenuRowIndex = e.RowIndex;
            _contextMenuColumnIndex = e.ColumnIndex;
            _contextMenuRowWasPlaceholder = rowWasPlaceholderBeforeThisClick;

            if (e.RowIndex >= 0 && !IsSystemColumn(e.ColumnIndex) && !rowWasSelectedBeforeThisClick)
            {
                SelectRow(e.RowIndex, e.ColumnIndex);
            }
        }

        // Selects every data cell in a row (skipping system columns - they
        // aren't a meaningful part of "this row is selected" the way the
        // right-click handling means it), making the actual cell that was
        // clicked current - not just whichever one happens to be first - so
        // the current cell stays under the cursor instead of jumping to the
        // row's first column.
        private void SelectRow(int rowIndex, int clickedColumnIndex)
        {
            ClearSelection();

            DataGridViewCell firstCell = null;
            DataGridViewCell clickedCell = null;

            foreach (DataGridViewColumn column in Columns)
            {
                if (IsSystemColumn(column.Index))
                {
                    continue;
                }

                DataGridViewCell cell = Rows[rowIndex].Cells[column.Index];
                firstCell = firstCell ?? cell;

                if (column.Index == clickedColumnIndex)
                {
                    clickedCell = cell;
                }
            }

            // Regression fix: this control's SelectionMode is CellSelect,
            // where assigning CurrentCell is itself a side-effecting
            // operation that collapses SelectedCells down to just the new
            // current cell - doing this LAST (as this used to) silently
            // undid every Selected = true the loop above had just made,
            // leaving only the clicked cell selected instead of the whole
            // row. Setting it FIRST instead means the loop below, which
            // selects every remaining cell afterward, is what actually
            // sticks.
            CurrentCell = clickedCell ?? firstCell;

            foreach (DataGridViewColumn column in Columns)
            {
                if (IsSystemColumn(column.Index))
                {
                    continue;
                }

                Rows[rowIndex].Cells[column.Index].Selected = true;
            }
        }

        // Separate from this class's own MouseDown subscription
        // (HandleMouseDown only clears a stray selection) - this one only
        // needs to run for a right-click landing on truly empty space (no
        // cell there at all, so OnCellMouseDown never fires), to forget
        // which row/column the context menu was about to target.
        private void HandleContextMenuMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            HitTestInfo hitTest = HitTest(e.X, e.Y);

            if (hitTest.Type == DataGridViewHitTestType.None)
            {
                _contextMenuRowIndex = -1;
                _contextMenuColumnIndex = -1;
            }
        }

        // The flat menu both grids (and ListView) share: Copy, Copy with
        // header | Copy all, Copy all with header | Select all - everything that still makes sense on a display-only grid. DataGridView (the editable
        // one) adds its own editing entries to this same menu instead of
        // building a second one (see its AddEditingMenuItems), so these
        // entries can never drift apart between the two grids.
        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                SelectionBackColor = ContextMenuSelectionColor
            };

            _contextMenuCopySelectionItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCopySelection"), null,
                (sender, e) => CopySelectionToClipboard());
            _contextMenuCopySelectionWithHeaderItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCopySelectionWithHeader"), null,
                (sender, e) => CopySelectionToClipboard(includeHeader: true));
            _contextMenuCopyAllItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCopyAll"), null,
                (sender, e) => CopyAllToClipboard(includeHeader: false));
            _contextMenuCopyAllWithHeaderItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCopyAllWithHeader"), null,
                (sender, e) => CopyAllToClipboard(includeHeader: true));
            _contextMenuSelectAllItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuSelectAll"), null, (sender, e) => SelectAll());

            menu.Items.Add(_contextMenuCopySelectionItem);
            menu.Items.Add(_contextMenuCopySelectionWithHeaderItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuCopyAllItem);
            menu.Items.Add(_contextMenuCopyAllWithHeaderItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuSelectAllItem);

            menu.Opening += (sender, e) =>
            {
                // No menu at all - not just disabled items - when the
                // right-click wasn't on any row (the header and empty space
                // below the rows both report RowIndex -1) or was on a
                // system column (the enumeration/delete column).
                if (_contextMenuRowIndex < 0 || IsSystemColumn(_contextMenuColumnIndex))
                {
                    e.Cancel = true;
                    return;
                }

                bool hasSelection = SelectedCells.Count > 0 && !_contextMenuRowWasPlaceholder;
                _contextMenuCopySelectionItem.Enabled = hasSelection;
                _contextMenuCopySelectionWithHeaderItem.Enabled = hasSelection;

                // Only worth offering with at least one real row - a grid
                // holding nothing but the "type here" placeholder (only ever
                // the editable one) has nothing meaningful to select or copy.
                int realRowCount = Rows.Count - (NewRowIndex >= 0 ? 1 : 0);
                _contextMenuCopyAllItem.Enabled = realRowCount > 0;
                _contextMenuCopyAllWithHeaderItem.Enabled = realRowCount > 0;
                _contextMenuSelectAllItem.Enabled = realRowCount > 0;
            };

            return menu;
        }

        // Puts the selection (minus system columns/the placeholder - see
        // GetClipboardContent) on the clipboard, the same content Ctrl+C
        // produces - shared by both grids' menus. includeHeader also puts
        // the selected columns' header text on top, in display order - the
        // native default (EnableWithAutoHeaderText) only does that for whole
        // rows/columns selected via their headers, which this control's
        // cell selection never produces, so it has to be asked for
        // explicitly (then put back, so a plain Ctrl+C is unaffected).
        protected void CopySelectionToClipboard(bool includeHeader = false)
        {
            if (SelectedCells.Count == 0)
            {
                return;
            }

            DataGridViewClipboardCopyMode previousMode = ClipboardCopyMode;

            if (includeHeader)
            {
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            }

            try
            {
                DataObject content = GetClipboardContent();

                if (content != null)
                {
                    Clipboard.SetDataObject(content);
                }
            }
            finally
            {
                ClipboardCopyMode = previousMode;
            }
        }

        private void CopyAllToClipboard(bool includeHeader)
        {
            SelectAll();
            CopySelectionToClipboard(includeHeader);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
                _copyConfirmationToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        // Keeps this class's own context menu and the enumeration column's
        // header/tooltip in whatever language the rest of the app just
        // switched to - mirrors DataGridView's own OnUIStringsLanguageChanged
        // for the delete column and its fuller menu, moved down here since
        // ShowEnumeration and the read-only menu live on this (base) class.
        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            if (_contextMenuCopySelectionItem != null)
            {
                _contextMenuCopySelectionItem.Text = UIStrings.Get("DataGridView.ContextMenuCopySelection");
                _contextMenuCopySelectionWithHeaderItem.Text = UIStrings.Get("DataGridView.ContextMenuCopySelectionWithHeader");
                _contextMenuCopyAllItem.Text = UIStrings.Get("DataGridView.ContextMenuCopyAll");
                _contextMenuCopyAllWithHeaderItem.Text = UIStrings.Get("DataGridView.ContextMenuCopyAllWithHeader");
                _contextMenuSelectAllItem.Text = UIStrings.Get("DataGridView.ContextMenuSelectAll");
            }

            if (_enumerationColumn == null)
            {
                return;
            }

            UpdateEnumerationHeaderText();
            _enumerationColumn.HeaderCell.ToolTipText = UIStrings.Get("DataGridView.RowNumber");
        }

        // "#" normally, but blank while this column is itself the active
        // sort column - ColumnHeaderPainting.DrawSortGlyph already draws
        // the ascending/descending arrow there regardless (the sort itself
        // creates that on its own, independent of this), and showing "#"
        // right next to a direction-flipping arrow read as if the "#"
        // itself was changing. Reverts back to "#" on the third click
        // (SortOrder.None, back to original bind order) same as any other
        // column losing the glyph.
        private void UpdateEnumerationHeaderText()
        {
            if (_enumerationColumn == null)
            {
                return;
            }

            bool isActiveSortColumn = _enumerationColumn.Index == _sortedColumnIndex && _sortOrder != SortOrder.None;
            _enumerationColumn.HeaderText = isActiveSortColumn ? string.Empty : UIStrings.Get("DataGridView.EnumerationHeader");
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

            // Every row-order/count-changing operation - a fresh bind, a
            // sort (CycleSort/ReorderDataSource below), or one of
            // DataGridView's own paste/insert/delete operations in a
            // subclass - ends up here via ResetBindings(), so renumbering
            // unconditionally (not just for a fresh bind, like the
            // bookkeeping below the early-return guard) is what keeps this
            // column's numbers live-correct after every one of them.
            RenumberEnumeration();

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

                UpdateEnumerationHeaderText();
            }
        }

        // The "type here to add a row" placeholder committing to a real row
        // (AllowUserToAddRows, only ever true on DataGridView) goes through
        // WinForms' own IBindingList.AddNew()/commit flow directly, never
        // through ApplyBatchedDataSourceChange/ResetBindings() - so
        // OnDataBindingComplete's own renumber above never runs for it,
        // and the enumeration column was left showing stale numbers (and
        // no number at all for the newly-committed row) until some
        // unrelated sort/insert/delete happened to trigger a renumber.
        protected override void OnUserAddedRow(DataGridViewRowEventArgs e)
        {
            base.OnUserAddedRow(e);
            RenumberEnumeration();
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
            _columnsInDisplayOrderCache = null;

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

            // Pinned leftmost regardless of SortingEnabled (unlike the
            // SortMode logic below, which the early return right after this
            // skips entirely when sorting is off) - a consumer disabling
            // sorting for the whole grid doesn't mean the row-number column
            // should stop being pinned in place. Re-applied on every add
            // (not just when the enumeration column itself comes in), same
            // defensive "always re-assert the pin" approach as
            // DataGridView's own rightmost delete column.
            if (e.Column == _enumerationColumn)
            {
                e.Column.DisplayIndex = 0;
            }
            else if (_enumerationColumn != null)
            {
                _enumerationColumn.DisplayIndex = 0;
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

            if (_pendingReorderColumnIndex >= 0 && !_isDraggingColumn &&
                Math.Abs(controlX - _pendingReorderStartX) >= SystemInformation.DragSize.Width)
            {
                int columnIndex = _pendingReorderColumnIndex;
                _pendingReorderColumnIndex = -1;
                BeginColumnDragDrop(columnIndex);
            }
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

        // Rebuilt only when the column SET or ORDER actually changes (see
        // the invalidation call sites: OnColumnAdded/OnColumnRemoved/
        // OnColumnDisplayIndexChanged) - not on every call, which used to
        // mean allocating and sorting a fresh list on every single header
        // mouse-move tick (TryGetColumnAtBorder's own hover-cursor check)
        // and every drag-over tick while reordering. Safe to hand callers
        // the cached list directly rather than a defensive copy - every
        // width this list's own entries get queried for (TryGetColumnAtBorder/
        // GetDropInsertionIndex both take a widthOf delegate) reads
        // DataGridViewColumn.Width live off the real column each call, not
        // a snapshotted value, so a column resizing without also reordering
        // never needs to invalidate this at all.
        private List<DataGridViewColumn> _columnsInDisplayOrderCache;

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

        protected override void OnColumnRemoved(DataGridViewColumnEventArgs e)
        {
            base.OnColumnRemoved(e);
            _columnsInDisplayOrderCache = null;
        }

        protected override void OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)
        {
            base.OnColumnDisplayIndexChanged(e);
            _columnsInDisplayOrderCache = null;
        }

        private bool IsColumnResizable(DataGridViewColumn column)
        {
            return IsColumnResizable(column.Index);
        }

        private const int ResizeGripWidth = 5;

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
            if (!_isDraggingColumn || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewColumn column = Columns[e.ColumnIndex];
            ColumnHeaderPainting.DrawColumnDragInsertionLine(
                e.Graphics, e.CellBounds, column.DisplayIndex, Columns.Count, _dragInsertBeforeDisplayIndex, ColumnReorderIndicatorColor);
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
                // remarks on why. Skipped for the enumeration column
                // specifically - confirmed live that combining this native
                // property with an EMPTY HeaderText (see
                // UpdateEnumerationHeaderText) makes WinForms' own default
                // header painting draw a second, native glyph on top of
                // ColumnHeaderPainting.DrawSortGlyph's own, the two
                // overlapping into a single "bowtie" blob instead of one
                // clean triangle. Every other column keeps real header
                // text, where this same combination never showed that
                // extra glyph at all.
                if (!IsEnumerationColumn(columnIndex))
                {
                    Columns[columnIndex].HeaderCell.SortGlyphDirection = _sortOrder;
                }

                ReorderDataSource(BuildSortedOrder(columnIndex, _sortOrder));
            }

            UpdateEnumerationHeaderText();

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

            // Whole-header repaint, forced synchronous rather than left for
            // the next WM_PAINT - the enumeration column's HeaderText just
            // changed (see UpdateEnumerationHeaderText) and every header
            // cell's own SortGlyphDirection/border can change too, so a
            // single-cell InvalidateCell isn't always enough to guarantee
            // the whole row is actually repainted before this method
            // returns.
            Invalidate();
            Update();

            ClearSelection();
            CurrentCell = null;
        }

        // Visually neutral, matching DataGridView's own delete-row column -
        // WinForms has no built-in "this column can't be selected" switch,
        // so a click still technically moves CurrentCell there, but giving
        // it the exact same SelectionBackColor/ForeColor as its own resting
        // colors means it never actually LOOKS selected, reading as a
        // passive label instead of a normal data cell.
        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);

            if (_enumerationColumn == null || e.ColumnIndex != _enumerationColumn.Index || e.RowIndex < 0)
            {
                return;
            }

            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }

        // ColumnHeaderPainting.DrawSortGlyph below draws its own triangle,
        // not the native SortGlyphDirection glyph (still set on the column,
        // for AT/screen-reader purposes, but not what actually paints here):
        // that glyph rides on the OS's own visual-style painting, which
        // EnableHeadersVisualStyles = false deliberately opts out of
        // everywhere else on this control (so its own
        // ColumnHeadersDefaultCellStyle colors apply instead of the OS
        // theme) - confirmed live that with it off, the native glyph
        // doesn't paint at all, not even faintly.
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);

            if (e.RowIndex != -1)
            {
                return;
            }

            // The default header painting (background/native border/text)
            // has to happen FIRST and be marked Handled here, same reason
            // as always with this trick - anything drawn before this
            // point in the method would otherwise just get painted over
            // once DataGridView's own default painting runs right after
            // this event handler returns (confirmed live: the insertion
            // line below drew, then immediately vanished, because it used
            // to run before this call).
            e.Paint(e.ClipBounds, e.PaintParts);
            e.Handled = true;

            // Every header cell's own border is redrawn here in
            // BorderColor - not the native ColumnHeadersBorderStyle one
            // (GridColor-driven, and GridColor is deliberately fixed - see
            // its own remarks on why), the same way ListView's own
            // OnDrawColumnHeader draws its header divider in BorderColor
            // regardless of what its (non-existent, for data rows) grid
            // lines look like. Together, every cell's own rectangle forms
            // one continuous frame around the whole header strip, matching
            // ListView's own look - CreatePrimary's header now reads as
            // accent-colored the same way ListView's does, while the
            // gridlines between actual rows underneath stay neutral.
            DrawHeaderCellBorder(e);

            DrawColumnDragInsertionLine(e);

            if (e.ColumnIndex != _sortedColumnIndex)
            {
                return;
            }

            ColumnHeaderPainting.DrawSortGlyph(e.Graphics, e.CellBounds, Font, _headerForeColor, _sortOrder);
        }

        private void DrawHeaderCellBorder(DataGridViewCellPaintingEventArgs e)
        {
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawRectangle(
                    pen,
                    e.CellBounds.Left,
                    e.CellBounds.Top,
                    e.CellBounds.Width - 1,
                    e.CellBounds.Height - 1);
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

        // The snapshot InsertItemsAt's own originalItems parameter expects
        // - must be taken BEFORE any AddNew() call the caller makes, per
        // InsertItemsAt's own remarks above on why.
        protected static List<object> SnapshotItems(IList list)
        {
            List<object> items = new List<object>(list.Count);

            foreach (object item in list)
            {
                items.Add(item);
            }

            return items;
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

            // ResetBindingsMethod below is exactly what DataGridView treats
            // as a fresh rebind - the same Reset notification a brand new
            // DataSource assignment would raise - which snaps the scroll
            // position back to the top-left the same way a real rebind
            // does, even though this change (a sort, a paste, a row
            // insert/delete) has nothing to do with actually rebinding.
            // Capturing both scroll properties before the change and
            // restoring them after (clamped to whatever's still valid once
            // the row count has potentially changed) is what keeps this
            // from silently scrolling the user back to the top of a long
            // grid on every one of those operations.
            int firstDisplayedScrollingRowIndex = Rows.Count > 0 ? FirstDisplayedScrollingRowIndex : -1;
            int horizontalScrollingOffset = HorizontalScrollingOffset;

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

            // FirstDisplayedScrollingRowIndex throws for an out-of-range
            // value rather than clamping itself (unlike
            // HorizontalScrollingOffset, which clamps silently) - guarded
            // for both an empty grid (nothing to scroll to) and a row
            // count that shrank (e.g. a delete) past where it used to be.
            if (firstDisplayedScrollingRowIndex >= 0 && Rows.Count > 0)
            {
                FirstDisplayedScrollingRowIndex = Math.Min(firstDisplayedScrollingRowIndex, Rows.Count - 1);
            }

            HorizontalScrollingOffset = horizontalScrollingOffset;

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
        // snapshot, rather than colliding at index -1 (see
        // ItemOrderMath.BuildOriginalOrder). Mirrors ListView's own
        // BuildOriginalOrder.
        private List<object> BuildOriginalOrder()
        {
            List<object> dataBoundItems = new List<object>();
            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem != null)
                {
                    dataBoundItems.Add(row.DataBoundItem);
                }
            }

            return ItemOrderMath.BuildOriginalOrder(dataBoundItems, _originalOrder);
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
