using System.ComponentModel;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

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
    /// The column types: text, check box and image columns follow the grid's colors; button, combo
    /// box and link columns are put in them when the column is added (flat, with the link colors
    /// readable on the rows). The system's own scroll bars and the editing control of a combo cell
    /// are not themed. Consumers never need to spell out
    /// <c>ErikwnkWFUI.Controls.ReadOnlyDataGridView</c>: go through
    /// <see cref="UIStyles.DataGridViews.CreateReadOnlyStandard"/>, which hands back
    /// a plain <see cref="System.Windows.Forms.DataGridView"/>-typed
    /// reference.
    /// </remarks>
    // In seven files: this one (properties, constructor, the common events), and ReadOnlyDataGridView.Columns.cs (moving and
    // resizing columns), .Sorting.cs, .DataSource.cs (changes the grid makes to its data source), .Menu.cs (right-click menu,
    // copying), .Enumeration.cs (the numbered column) and .Theme.cs (colors, cell styles, painting).
    public partial class ReadOnlyDataGridView : System.Windows.Forms.DataGridView
    {
        // The colors that follow the theme until they are set (see ThemeWatcher).
        private readonly ThemeColor _headerBackColor = new ThemeColor(() => UIColors.BackgroundDarkElevated);

        private readonly ThemeColor _headerForeColor = new ThemeColor(() => UIColors.TextTertiary);

        private readonly ThemeColor _rowBackColor = new ThemeColor(() => UIColors.BackgroundMedium);

        private readonly ThemeColor _rowForeColor = new ThemeColor(() => UIColors.TextPrimary);

        private readonly ThemeColor _selectionBackColor = new ThemeColor(() => UIColors.BorderLight);

        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);

        private readonly ColumnFeatureSwitch _columnReordering = new ColumnFeatureSwitch();

        private readonly ThemeColor _columnReorderIndicatorColor = new ThemeColor(() => UIColors.BorderLight);

        private readonly ColumnDragState _columnDrag = new ColumnDragState();

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


        // Matches Controls.ContextMenuStrip's own CreateStandard default. The menu that is built in the constructor takes
        // its highlight from here each time it is painted (see BuildContextMenu), so a later change is seen by it.
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
            set { _contextMenuSelectionColor.Set(value); }
        }


        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get => _headerBackColor.Value;
            set { _headerBackColor.Set(value); ApplyStyles(); }
        }


        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get => _headerForeColor.Value;
            set { _headerForeColor.Set(value); ApplyStyles(); }
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
            get => _rowBackColor.Value;
            set
            {
                _rowBackColor.Set(value);
                ApplyStyles();
            }
        }


        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get => _rowForeColor.Value;
            set { _rowForeColor.Set(value); ApplyStyles(); }
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
        [DefaultValue(true)]
        public bool AllowColumnReordering
        {
            get => _columnReordering.AllowedByDefault;
            set => _columnReordering.AllowedByDefault = value;
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
        [DefaultValue(true)]
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
        [DefaultValue(DefaultMinimumColumnWidth)]
        public int MinimumColumnWidth
        {
            get => _minimumColumnWidth;
            set => _minimumColumnWidth = Math.Max(1, value);
        }


        private const int DefaultMinimumColumnWidth = 40;


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
        [DefaultValue(true)]
        public bool SortingEnabled { get; set; } = true;


        /// <summary>
        /// How a copy, or any of DataGridView's own editing actions (cut,
        /// paste, clear, delete rows, insert row), is confirmed - the same
        /// three-way choice as <see cref="ListView.CopyConfirmation"/>
        /// (they share <see cref="CopyConfirmationStyle"/> and the same
        /// display logic - see <see cref="CopyConfirmationDisplay"/>),
        /// named more broadly here since this control has more than just
        /// copying to confirm. Defaults to <see cref="CopyConfirmationStyle.Toast"/>.
        /// </summary>
        [DefaultValue(CopyConfirmationStyle.Toast)]
        public CopyConfirmationStyle ActionConfirmation { get; set; } = CopyConfirmationStyle.Toast;

        /// <summary>
        /// The colors of the toast that confirms an action (<see cref="CopyConfirmationStyle.Toast"/>): a color set (one of <c>UIStyles.Colors.Sets</c>,
        /// <c>UIStyles.Colors.CreateSet(color)</c> or a <c>Color</c>), null for the neutral gray. Until it is set it follows the look of the grid:
        /// gray for the standard one, the color of the set of the factories' <c>colors</c>, the accent for <c>CreatePrimary</c>.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet ToastColors
        {
            get { return _toastColors.Value; }
            set { _toastColors.Value = value; }
        }

        private readonly ColorSetHolder _toastColors = new ColorSetHolder();


        private bool _suppressActionConfirmation;


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
        [DefaultValue(false)]
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


        private const int EnumerationColumnPadding = 20;


        /// <summary>Makes a grid in the library's look, read only, with the context menu to copy and select; <c>UIStyles.DataGridViews</c> makes the usual ones.</summary>
        public ReadOnlyDataGridView()
        {
            _followedBackgroundColor = BackgroundColor = UIColors.BackgroundDark;

            // Fixed, independent of BorderColor - only each header cell's
            // own border (see DrawHeaderCellBorder below) follows
            // BorderColor/the accent in CreatePrimary; the gridlines
            // BETWEEN cells stay this same neutral color regardless, so
            // CreatePrimary doesn't turn every row separator into a wash of
            // accent color too.
            _followedGridColor = GridColor = UIColors.BorderMedium;

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
            // ProgressBar needs for the exact same reason.
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
            ThemeScrollBars();
            ApplyStyles();

            ThemeWatcher.Attach(this, OnThemeChanged);

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
        /// <summary>The row the context menu was opened on (-1: none); a snapshot taken at the click.</summary>
        protected int _contextMenuRowIndex = -1;

        /// <summary>The column the context menu was opened on (-1: none).</summary>
        protected int _contextMenuColumnIndex = -1;

        /// <summary>Whether the row the context menu was opened on was the placeholder row for a new row.</summary>
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
        /// <summary>Whether the row is the placeholder row for a new row.</summary>
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
        /// <summary>Whether the column is one of the library's own (the numbers, the delete button), which the user cannot select, sort or move.</summary>
        protected virtual bool IsSystemColumn(int columnIndex)
        {
            return IsEnumerationColumn(columnIndex);
        }


        // The designer writes a color only where it was set (the defaults follow the theme, they are not stored); Reset gives the default back.
        private bool ShouldSerializeContextMenuSelectionColor()
        {
            return _contextMenuSelectionColor.IsSet;
        }

        private void ResetContextMenuSelectionColor()
        {
            _contextMenuSelectionColor.Reset();
        }

        private bool ShouldSerializeHeaderBackColor()
        {
            return _headerBackColor.IsSet;
        }

        private void ResetHeaderBackColor()
        {
            _headerBackColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeHeaderForeColor()
        {
            return _headerForeColor.IsSet;
        }

        private void ResetHeaderForeColor()
        {
            _headerForeColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeBorderColor()
        {
            return _borderColor.IsSet;
        }

        private void ResetBorderColor()
        {
            _borderColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeRowBackColor()
        {
            return _rowBackColor.IsSet;
        }

        private void ResetRowBackColor()
        {
            _rowBackColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeRowForeColor()
        {
            return _rowForeColor.IsSet;
        }

        private void ResetRowForeColor()
        {
            _rowForeColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeSelectionBackColor()
        {
            return _selectionBackColor.IsSet;
        }

        private void ResetSelectionBackColor()
        {
            _selectionBackColor.Reset();
            ApplyStyles();
        }

        private bool ShouldSerializeColumnReorderIndicatorColor()
        {
            return _columnReorderIndicatorColor.IsSet;
        }

        private void ResetColumnReorderIndicatorColor()
        {
            _columnReorderIndicatorColor.Reset();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
                if (_outsideClickDeselectFilter != null)
                    _outsideClickDeselectFilter.Stop();

                _copyConfirmationToolTip.Dispose();
                _headerFont?.Dispose();
                _headerFont = null;
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


        /// <inheritdoc/>
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


        // The colors the grid has as long as nothing sets them.
        private Color _followedBackgroundColor;

        private Color _followedGridColor;

        private Color _followedButtonBackColor;

        private Color _followedButtonForeColor;

        private Color[] _followedLinkColors = new Color[3];


        /// <inheritdoc/>
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

            ThemeColumnType(e.Column);

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


        /// <inheritdoc/>
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


        private const int ResizeGripWidth = 5;


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


        // The scroll bars of the grid are controls of their own inside it (private fields of the
        // standard grid, named differently on .NET Framework and .NET); they get the dark visual
        // style whenever their window is created.
        private Control[] _scrollBars = new Control[0];


        // The bold font of the column headers. The cell style keeps the reference it is given (it does not copy the
        // font), so the font has to live as long as the style uses it: it is held here, made again when the font of the
        // grid changes, and freed with the grid.
        private Font _headerFont;

        private Font _headerFontSource;


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
