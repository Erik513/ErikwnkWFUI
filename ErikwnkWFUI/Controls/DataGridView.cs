using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.DataGridView"/> for displaying
    /// data bound via <see cref="System.Windows.Forms.DataGridView.DataSource"/>
    /// (a DataTable, a List&lt;T&gt;, a BindingSource, ...). Unlike
    /// <see cref="ListView"/> - a plain native ListView has no
    /// data-binding support at all, and neither does anything built on top
    /// of it - this is the control to reach for when rows come from a bound
    /// source rather than being added by hand. A genuine WinForms control
    /// (not a thin wrapper around a native Win32 common control the way
    /// ListView is), so unlike ListView this needs none of that
    /// header-subclassing/WM_PAINT-interception - every visual it has is
    /// reachable through its own style properties.
    /// </summary>
    /// <remarks>
    /// Named the same as its own base class (a WinForms convention this
    /// library is moving toward, replacing the old "Styled" prefix) - the
    /// base type reference below has to stay fully qualified so the class
    /// doesn't try to inherit from itself. Consumers never need to spell out
    /// <c>ErikwnkWFUI.Controls.DataGridView</c> either: go through
    /// <see cref="UIStyles.DataGridViews.CreateStandard"/>, which hands back
    /// a plain <see cref="System.Windows.Forms.DataGridView"/>-typed
    /// reference.
    /// </remarks>
    public class DataGridView : System.Windows.Forms.DataGridView
    {
        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        private Color _selectionBackColorOverride;
        private bool _selectionBackColorIsOverridden;

        // Snapshot of DataBoundItem references in the order they were bound,
        // captured on each fresh bind/reset - lets a header click's third
        // state ("original") restore that order without the underlying data
        // source needing to support sorting itself.
        private List<object> _originalOrder = new List<object>();
        private int _sortedColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private bool _clearSelectionOnNextVisible;
        private bool _isApplyingInternalDataChange;
        private DataGridViewColumn _deleteRowColumn;
        private int _hoveredDeleteRowIndex = -1;
        private int _contextMenuRowIndex = -1;
        private int _contextMenuColumnIndex = -1;
        private bool _contextMenuRowWasPlaceholder;
        private ToolStripMenuItem _contextMenuCutItem;
        private ToolStripMenuItem _contextMenuCopyItem;
        private ToolStripMenuItem _contextMenuPasteItem;
        private ToolStripMenuItem _contextMenuClearItem;
        private ToolStripMenuItem _contextMenuDeleteRowsItem;
        private ToolStripMenuItem _contextMenuInsertRowAboveItem;
        private ToolStripMenuItem _contextMenuInsertRowBelowItem;

        /// <summary>Fixed width of the optional delete-row column (see <see cref="ShowDeleteRowColumn"/>), in case a consumer needs to reserve space for it in its own column-width math.</summary>
        public const int DeleteRowColumnWidth = 40;

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
        public bool SortingEnabled { get; set; } = true;

        /// <summary>
        /// Whether a small "delete this row" button column is shown, pinned
        /// as the rightmost column regardless of what else is added
        /// afterward. Off by default - a consumer opts in any time after
        /// construction (typically right after adding its own columns).
        /// The column's own cells are never selectable (clicking one only
        /// ever deletes that row, same as selecting a row and pressing
        /// Delete already does) and it's never sortable. Also respects
        /// <see cref="System.Windows.Forms.DataGridView.AllowUserToDeleteRows"/>:
        /// clicking the button does nothing while that's false.
        /// </summary>
        public bool ShowDeleteRowColumn
        {
            get => _deleteRowColumn != null;
            set
            {
                if (value == (_deleteRowColumn != null))
                {
                    return;
                }

                if (value)
                {
                    // A plain text column rather than DataGridViewButtonColumn -
                    // a button cell's face/border draws with the OS's own flat-
                    // button chrome regardless of DefaultCellStyle (a thin
                    // light/white edge stayed visible around the glyph even
                    // with FlatStyle.Flat), so the "X" couldn't be made fully
                    // red that way. Plain text has no such chrome: ForeColor
                    // below is the only color involved.
                    DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
                    {
                        Name = "__deleteRow",
                        // The Delete key does the same thing (see
                        // ProcessDataGridViewKey below) - naming that
                        // shortcut here doubles as the clearest possible
                        // label for what clicking the column does.
                        HeaderText = UIStrings.Get("DataGridView.DeleteRowHeader"),
                        ReadOnly = true,
                        Resizable = DataGridViewTriState.False,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        Width = DeleteRowColumnWidth,
                        MinimumWidth = DeleteRowColumnWidth
                    };

                    column.DefaultCellStyle.ForeColor = UIColors.Red;
                    column.DefaultCellStyle.SelectionForeColor = UIColors.Red;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

                    // The tooltip spells out what the header's short
                    // shortcut name only hints at.
                    column.HeaderCell.ToolTipText = UIStrings.Get("DataGridView.DeleteRow");

                    // Assigned before Add() - OnColumnAdded below checks
                    // this field to recognize the column as it comes in.
                    _deleteRowColumn = column;
                    Columns.Add(column);
                }
                else
                {
                    Columns.Remove(_deleteRowColumn);
                    _deleteRowColumn = null;
                }
            }
        }

        public DataGridView()
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

            AllowUserToAddRows = false;

            // The base property doubles as this control's own opt-out for
            // row deletion (see ProcessDataGridViewKey) - a consumer that
            // doesn't want rows deletable sets this false, same as they
            // would on a stock DataGridView.
            AllowUserToDeleteRows = true;
            AllowUserToResizeRows = false;
            MultiSelect = true;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            Font = UIFonts.Normal;
            RowTemplate.Height = Font.Height + 12;

            EnableDoubleBuffering();
            ApplyStyles();

            MouseDown += HandleMouseDown;
            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;
            ContextMenuStrip = BuildContextMenu();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
            }

            base.Dispose(disposing);
        }

        // Keeps the delete-row column's header text/tooltip, and the
        // context menu's own item text, in whatever language the rest of
        // the app just switched to.
        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            if (_deleteRowColumn != null)
            {
                _deleteRowColumn.HeaderText = UIStrings.Get("DataGridView.DeleteRowHeader");
                _deleteRowColumn.HeaderCell.ToolTipText = UIStrings.Get("DataGridView.DeleteRow");
            }

            if (_contextMenuCutItem != null)
            {
                _contextMenuCutItem.Text = UIStrings.Get("DataGridView.ContextMenuCut");
                _contextMenuCopyItem.Text = UIStrings.Get("DataGridView.ContextMenuCopy");
                _contextMenuPasteItem.Text = UIStrings.Get("DataGridView.ContextMenuPaste");
                _contextMenuClearItem.Text = UIStrings.Get("DataGridView.ContextMenuClear");
                _contextMenuDeleteRowsItem.Text = UIStrings.Get("DataGridView.ContextMenuDeleteRows");
                _contextMenuInsertRowAboveItem.Text = UIStrings.Get("DataGridView.ContextMenuInsertRowAbove");
                _contextMenuInsertRowBelowItem.Text = UIStrings.Get("DataGridView.ContextMenuInsertRowBelow");
            }
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
                // This Reset was caused by CycleSort/ReorderDataSource
                // rewriting the bound list's contents to apply or clear a
                // sort, not a real rebind - the bookkeeping below would
                // otherwise stomp on the sort state being applied right now.
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
                    column.HeaderText = StripSortSuffix(column.HeaderText);
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

            if (e.Column == _deleteRowColumn)
            {
                // SortMode/width etc. are already set on the column object
                // itself (see ShowDeleteRowColumn) - only the display
                // position needs handling here, same as below.
                e.Column.DisplayIndex = Columns.Count - 1;
                return;
            }

            if (_deleteRowColumn != null)
            {
                // A consumer adding a content column after already opting
                // into the delete column - keep the delete column pinned
                // as the rightmost one rather than letting the new column
                // land to its right.
                _deleteRowColumn.DisplayIndex = Columns.Count - 1;
            }

            // Windows shows a cell's accessible "default action" as a hover
            // hint on its own (independent of ShowCellToolTips) whenever a
            // cell has no other accessible name to show instead - which is
            // exactly an empty, editable cell. For a plain DataGridViewTextBoxCell
            // that default action is a bare, unhelpful "Edit"/"Bearbeiten"
            // regardless of what the column actually holds, so it's replaced
            // here with nothing (or, for the "type here to add a row"
            // placeholder specifically, something that actually explains
            // what clicking there does).
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
            // BindingList&lt;T&gt; doesn't).
            e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
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

            bool isDeleteRowColumn = _deleteRowColumn != null && e.ColumnIndex == _deleteRowColumn.Index;

            if (SortingEnabled && e.Button == MouseButtons.Left && e.ColumnIndex >= 0 && !isDeleteRowColumn)
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

                if (e.Button == MouseButtons.Right)
                {
                    _contextMenuRowIndex = -1;
                    _contextMenuColumnIndex = -1;
                }
            }
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseDown(e);

            if (e.Button == MouseButtons.Right)
            {
                _contextMenuRowIndex = e.RowIndex;
                _contextMenuColumnIndex = e.ColumnIndex;

                // IsNewRow, not a list.Count comparison, on purpose - see
                // InsertBlankRow for why: right-clicking the placeholder
                // itself can already grow the bound list by one before
                // that method ever runs, but IsNewRow still reports true
                // for it regardless (it only flips once the add is
                // actually committed), so this captures "was this really
                // the placeholder when clicked" reliably either way.
                _contextMenuRowWasPlaceholder = e.RowIndex >= 0 && Rows[e.RowIndex].IsNewRow;

                bool isDeleteColumnCell = _deleteRowColumn != null && e.ColumnIndex == _deleteRowColumn.Index;

                // Right-clicking a row that isn't already part of the
                // current selection replaces it with just that row -
                // otherwise the context menu's row-scoped actions (Cut,
                // Delete selected rows, ...) would silently apply to
                // whatever was selected before, not the row actually
                // under the cursor. A row already part of a larger
                // selection is left alone, so right-clicking within an
                // existing multi-row selection keeps it intact. This
                // still runs for the "type here to add a row" placeholder
                // (its own row-scoped actions are disabled below, but
                // Paste and "insert row above" both still make sense
                // there, and both read the current selection to know
                // where to act) - only the delete column is skipped, same
                // as the menu's own Opening handler below never showing a
                // menu there at all.
                if (e.RowIndex >= 0 && !isDeleteColumnCell && !IsRowSelected(e.RowIndex))
                {
                    SelectRow(e.RowIndex, e.ColumnIndex);
                }
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
            //
            // The delete-row column's own data cells (RowIndex >= 0) are
            // deliberately NOT cleared here the same way, even now that the
            // column is a plain DataGridViewTextBoxColumn rather than a
            // DataGridViewButtonColumn: nulling CurrentCell mid-press turned
            // out to still swallow the click (tried it - OnCellClick below
            // stopped firing), so this is a DataGridView-wide press-tracking
            // quirk, not something specific to button cells. Those cells get
            // the "unselectable" look a different way instead - see
            // OnCellFormatting, which keeps SelectionBackColor matching
            // whatever that row's own resting background already is.
            ClearSelection();
            CurrentCell = null;
        }

        // The delete-row column is a plain read-only text column showing a
        // static "X" glyph (see FormatDeleteRowCellText) rather than a
        // DataGridViewButtonColumn, so a genuine cell click is what
        // triggers deletion here, not CellContentClick (that event is
        // specific to button/checkbox column content).
        protected override void OnCellClick(DataGridViewCellEventArgs e)
        {
            base.OnCellClick(e);

            if (_deleteRowColumn == null ||
                e.ColumnIndex != _deleteRowColumn.Index ||
                e.RowIndex < 0 ||
                !AllowUserToDeleteRows)
            {
                return;
            }

            int rowIndex = e.RowIndex;

            // Deferred rather than deleted right here: this fires from
            // inside the clicked cell's own click handling (still on the
            // call stack), and ApplyBatchedDataSourceChange's
            // ResetBindings rebuilds the entire Rows collection - including
            // discarding the very row/cell whose click handling is what's
            // currently running. BeginInvoke runs the delete once that
            // finishes unwinding instead of while it's still live.
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() => DeleteRowAt(rowIndex)));
            }
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);

            if (_deleteRowColumn == null || e.ColumnIndex != _deleteRowColumn.Index || e.RowIndex < 0)
            {
                return;
            }

            // Applies to every cell in the column, including the "type here
            // to add a row" placeholder (IsNewRow) - that one shows no "X"
            // below (nothing to delete there yet), but it's still a cell in
            // this column, and pressing it showed the same real (green)
            // selection color this same fix already covers for real rows.
            // e.CellStyle.BackColor is already whatever this row's own
            // resting color is (normal or alternating - see the
            // OnCellMouseDown comment above for why CurrentCell can't just
            // be kept off this cell instead), so this doesn't hardcode a
            // color of its own - it only carries that same value over to
            // SelectionBackColor, so a press here never shows the grid's
            // real (green) selection color.
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;

            if (Rows[e.RowIndex].IsNewRow)
            {
                return;
            }

            e.Value = "✕";
            e.FormattingApplied = true;

            // AlternatingRowsDefaultCellStyle outranks Column.DefaultCellStyle
            // for odd-indexed rows, so every other row's "X" was coming out
            // in the normal row text color instead of red. Setting it here,
            // on the cell style actually used for this paint pass, outranks
            // both.
            Color foreColor = e.RowIndex == _hoveredDeleteRowIndex
                ? Lighten(UIColors.Red, 40)
                : UIColors.Red;
            e.CellStyle.ForeColor = foreColor;
            e.CellStyle.SelectionForeColor = foreColor;
        }

        protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
        {
            base.OnCellMouseEnter(e);

            if (_deleteRowColumn != null &&
                e.ColumnIndex == _deleteRowColumn.Index &&
                e.RowIndex >= 0)
            {
                _hoveredDeleteRowIndex = e.RowIndex;
                InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }

        protected override void OnCellMouseLeave(DataGridViewCellEventArgs e)
        {
            base.OnCellMouseLeave(e);

            if (_deleteRowColumn != null &&
                e.ColumnIndex == _deleteRowColumn.Index &&
                e.RowIndex == _hoveredDeleteRowIndex)
            {
                _hoveredDeleteRowIndex = -1;
                InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }

        // Base DataGridView's own IsInputKey already claims Ctrl+C this
        // same way (that's what having ClipboardCopyMode enabled actually
        // does under the hood) - returning true here is what stops Windows
        // treating a key as an unclaimed shortcut/mnemonic and keeps it
        // routing to this control's own OnKeyDown below instead, which is
        // where Ctrl+V/Ctrl+X actually get handled.
        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.Control) == Keys.Control)
            {
                Keys keyCode = keyData & Keys.KeyCode;

                if (keyCode == Keys.V || keyCode == Keys.X)
                {
                    return true;
                }
            }

            return base.IsInputKey(keyData);
        }

        /// <summary>
        /// Deletes whichever rows have a selected cell when Delete is
        /// pressed - RowHeadersVisible is false on this control (see the
        /// constructor), so the base DataGridView's own Delete handling
        /// (which acts on <see cref="System.Windows.Forms.DataGridView.SelectedRows"/>,
        /// populated by clicking a row header) never has anything to act on; this reads
        /// <see cref="System.Windows.Forms.DataGridView.SelectedCells"/>
        /// instead; clicking any cell in a row is already how the rest of
        /// this control's interactions treat "that row" as selected.
        /// </summary>
        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete &&
                AllowUserToDeleteRows &&
                !IsCurrentCellInEditMode)
            {
                DeleteSelectedRows();
                return true;
            }

            return base.ProcessDataGridViewKey(e);
        }

        // Not handled in ProcessDataGridViewKey above like Delete is: the
        // base class's own OnKeyDown only ever forwards a fixed set of keys
        // into ProcessDataGridViewKey at all (navigation keys, plus C for
        // its own Ctrl+C support) - V and X aren't in that set, so putting
        // this there like Delete would silently never run. OnKeyDown itself
        // is the actual hook base.OnKeyDown uses to decide that forwarding,
        // so intercepting Ctrl+V/Ctrl+X here, before calling base.OnKeyDown,
        // reaches them reliably instead.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && !IsCurrentCellInEditMode && !ReadOnly)
            {
                if (e.KeyCode == Keys.X)
                {
                    CutSelectionToClipboard();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.V)
                {
                    PasteFromClipboard();
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        // Overriding this (rather than building clipboard text by hand)
        // fixes copy for Ctrl+C too, not just Cut below - the base class's
        // own Ctrl+C (ProcessInsertKey) calls this same virtual method to
        // build what it puts on the clipboard. The delete column's cells
        // have real values ("✕") and can end up selected like any other
        // cell (e.g. a drag-select spanning the whole row), but there's
        // nothing meaningful to copy from a column that only ever deletes
        // rows - so its cells are excluded here regardless of how the
        // request came in.
        public override DataObject GetClipboardContent()
        {
            if (_deleteRowColumn == null)
            {
                return base.GetClipboardContent();
            }

            List<DataGridViewCell> deleteColumnCells = new List<DataGridViewCell>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.ColumnIndex == _deleteRowColumn.Index)
                {
                    deleteColumnCells.Add(cell);
                }
            }

            if (deleteColumnCells.Count == 0)
            {
                return base.GetClipboardContent();
            }

            foreach (DataGridViewCell cell in deleteColumnCells)
            {
                cell.Selected = false;
            }

            try
            {
                return base.GetClipboardContent();
            }
            finally
            {
                foreach (DataGridViewCell cell in deleteColumnCells)
                {
                    cell.Selected = true;
                }
            }
        }

        // Cut = copy (via the same clipboard content the base class's own
        // Ctrl+C builds) then clear, rather than deleting the row outright -
        // that's a different, already-existing action (see Delete above).
        private void CutSelectionToClipboard()
        {
            if (SelectedCells.Count == 0)
            {
                return;
            }

            DataObject clipboardContent = GetClipboardContent();

            if (clipboardContent != null)
            {
                Clipboard.SetDataObject(clipboardContent);
            }

            ClearSelectedCellValues();
        }

        // Just the "clear" half of Cut - its own context-menu entry
        // ("Delete", as in clear the cell contents, not delete the row -
        // that's the separate "Delete selected rows" entry) needs it
        // without also touching the clipboard.
        private void ClearSelectedCellValues()
        {
            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (!cell.ReadOnly && cell.RowIndex >= 0 && !Rows[cell.RowIndex].IsNewRow)
                {
                    cell.Value = null;
                }
            }
        }

        /// <summary>
        /// Pastes tab-separated, newline-separated text (what Excel, Word -
        /// copying a table - and this grid's own Ctrl+C all put on the
        /// clipboard as plain text) starting at the top-left of the
        /// current selection. Only the selected rows are overwritten in
        /// place; a pasted block taller than the selection gets the extra
        /// rows inserted right after it, via the bound list's own
        /// <see cref="IBindingList.AddNew"/>, rather than overwriting
        /// whatever real rows happened to already be sitting there.
        /// </summary>
        private void PasteFromClipboard()
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            string text = Clipboard.GetText();

            if (string.IsNullOrEmpty(text) || !(DataSource is IList list))
            {
                return;
            }

            string[] pastedRows = text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n').Split('\n');

            if (pastedRows.Length == 0)
            {
                return;
            }

            List<DataGridViewColumn> targetColumns = GetPasteTargetColumns();

            if (targetColumns.Count == 0)
            {
                return;
            }

            // Anchored at the top-left of the current SELECTION, not just
            // CurrentCell - CurrentCell is whichever cell was clicked or
            // navigated to LAST within a multi-cell selection (e.g. the
            // far corner of a drag-select or a Shift+Right extension), not
            // necessarily where the selection starts, so using it alone
            // put paste in the wrong column/row whenever more than one
            // cell was selected. The delete column is never a valid anchor
            // (nothing to paste into it), same as it's never a paste
            // target below.
            int minRowIndex = int.MaxValue;
            DataGridViewColumn minColumn = null;
            HashSet<int> selectedRealRowIndexes = new HashSet<int>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (_deleteRowColumn != null && cell.ColumnIndex == _deleteRowColumn.Index)
                {
                    continue;
                }

                if (cell.RowIndex < minRowIndex)
                {
                    minRowIndex = cell.RowIndex;
                }

                if (cell.RowIndex < list.Count)
                {
                    selectedRealRowIndexes.Add(cell.RowIndex);
                }

                DataGridViewColumn column = Columns[cell.ColumnIndex];

                if (minColumn == null || column.DisplayIndex < minColumn.DisplayIndex)
                {
                    minColumn = column;
                }
            }

            if (minColumn == null && CurrentCell != null)
            {
                minRowIndex = CurrentCell.RowIndex;
                minColumn = CurrentCell.OwningColumn;

                if (CurrentCell.RowIndex < list.Count)
                {
                    selectedRealRowIndexes.Add(CurrentCell.RowIndex);
                }
            }

            int startColumnIndex = minColumn != null ? targetColumns.IndexOf(minColumn) : -1;

            if (startColumnIndex < 0)
            {
                startColumnIndex = 0;
            }

            // Same "does this row already have a real backing item"
            // check selectedRealRowIndexes uses below (cell.RowIndex <
            // list.Count), not Rows[minRowIndex].IsNewRow - clicking into
            // the "type here to add a row" placeholder turns out to make
            // WinForms call IBindingList.AddNew() on the bound list right
            // then, growing list.Count immediately, well before
            // IsNewRow ever stops reporting true for that row (that only
            // happens once the add is actually committed, e.g. by typing
            // into it). Using IsNewRow here disagreed with
            // selectedRealRowIndexes about whether that same row counted
            // as "real" - it doesn't matter for this method whether
            // WinForms still considers the row uncommitted, only whether
            // it already has a list item to write into.
            int startDataRowIndex = minRowIndex != int.MaxValue && minRowIndex < list.Count
                ? minRowIndex
                : list.Count;

            // Only the rows actually selected get overwritten in place - a
            // paste block taller than the selection gets the rest INSERTED
            // right after it instead of overwriting whatever real rows
            // happened to already be sitting there (e.g. pasting 10 rows
            // onto a 4-row selection in the middle of a 20-row list must
            // not clobber rows 5-10 of someone's existing data; it should
            // push them down by 6 instead).
            int overflowRowCount = pastedRows.Length - selectedRealRowIndexes.Count;
            IBindingList bindingList = list as IBindingList;

            if (overflowRowCount > 0 && (bindingList == null || !bindingList.AllowNew))
            {
                // Can't grow the list - still worth filling whatever
                // existing rows the paste does reach below.
                pastedRows = Trim(pastedRows, list.Count - startDataRowIndex);
                overflowRowCount = 0;
            }

            // Growing the list AND writing the pasted values both happen
            // inside this one suppress-events-then-ResetBindings-once
            // helper (CycleSort/DeleteRows already use it) rather than as
            // two separate steps:
            //
            // - Reentrancy: with change notifications live, each
            //   individual mutation reenters the grid's own
            //   binding-complete handling while still inside this
            //   key-press handler (the same kind of reentrancy that
            //   caused real crashes for sort/delete before they went
            //   through this helper).
            //
            // - New rows vanishing: a first version of this wrote pasted
            //   values afterward, through DataGridViewCell.Value, once
            //   the grid had already rebound and displayed the freshly
            //   added blank rows. BindingList&lt;T&gt; treats whatever
            //   AddNew() just added as still cancellable until something
            //   commits it - normally a side effect of the grid's own
            //   edit-commit handling once a user types into a cell - and
            //   setting Value programmatically doesn't trigger that same
            //   commit. The row silently vanished the next time focus
            //   moved elsewhere, as if the add had been cancelled - even
            //   calling BindingList&lt;T&gt;.EndNew() by hand right after
            //   AddNew() didn't stop it, so something in the grid's own
            //   side of this (separate from BindingList's own pending-add
            //   tracking) was still involved. Setting the pasted values
            //   directly on the bound objects here instead - before
            //   ResetBindings ever shows the grid a "freshly added, still
            //   empty" row at all - sidesteps that whole lifecycle: the
            //   row only ever appears already complete.
            ApplyBatchedDataSourceChange(list, () =>
            {
                if (overflowRowCount > 0)
                {
                    // Removing/inserting at a hand-picked index one row at
                    // a time (an earlier version of this) left the grid's
                    // own internal row/cell bookkeeping corrupted even
                    // though the list ended up with the right contents -
                    // later clicks into the grid threw
                    // InvalidOperationException out of WinForms' own code.
                    // Clear() + re-Add() in the exact final order instead
                    // is the same rebuild-from-scratch pattern
                    // ReorderDataSource already uses safely for sorting,
                    // without that corruption.
                    int insertAtIndex = startDataRowIndex + selectedRealRowIndexes.Count;
                    List<object> originalItems = new List<object>(list.Count);

                    foreach (object item in list)
                    {
                        originalItems.Add(item);
                    }

                    List<object> newItems = new List<object>(overflowRowCount);

                    for (int i = 0; i < overflowRowCount; i++)
                    {
                        newItems.Add(bindingList.AddNew());
                    }

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

                for (int rowOffset = 0; rowOffset < pastedRows.Length; rowOffset++)
                {
                    int dataRowIndex = startDataRowIndex + rowOffset;

                    if (dataRowIndex >= list.Count)
                    {
                        break;
                    }

                    object targetItem = list[dataRowIndex];
                    string[] cellValues = pastedRows[rowOffset].Split('\t');

                    for (int columnOffset = 0;
                        columnOffset < cellValues.Length && startColumnIndex + columnOffset < targetColumns.Count;
                        columnOffset++)
                    {
                        DataGridViewColumn column = targetColumns[startColumnIndex + columnOffset];

                        if (column.ReadOnly || string.IsNullOrEmpty(column.DataPropertyName))
                        {
                            continue;
                        }

                        PropertyInfo property = targetItem.GetType().GetProperty(column.DataPropertyName);
                        property?.SetValue(targetItem, cellValues[columnOffset]);
                    }
                }
            });
        }

        private static string[] Trim(string[] values, int maxLength)
        {
            if (maxLength >= values.Length)
            {
                return values;
            }

            if (maxLength <= 0)
            {
                return Array.Empty<string>();
            }

            string[] trimmed = new string[maxLength];
            Array.Copy(values, trimmed, maxLength);
            return trimmed;
        }

        // Every data column in display order - the optional delete column
        // (see ShowDeleteRowColumn) is never a paste target, there's
        // nothing meaningful to paste into it.
        private List<DataGridViewColumn> GetPasteTargetColumns()
        {
            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();

            foreach (DataGridViewColumn column in Columns)
            {
                if (column != _deleteRowColumn)
                {
                    columns.Add(column);
                }
            }

            columns.Sort((first, second) => first.DisplayIndex.CompareTo(second.DisplayIndex));
            return columns;
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

        // Selects every data cell in a row (skipping the delete column, if
        // shown - it isn't a meaningful part of "this row is selected" the
        // way the right-click handling above means it), making the actual
        // cell that was clicked current - not just whichever one happens
        // to be first - so the current cell stays under the cursor instead
        // of jumping to the row's first column.
        private void SelectRow(int rowIndex, int clickedColumnIndex)
        {
            ClearSelection();

            DataGridViewCell firstCell = null;
            DataGridViewCell clickedCell = null;

            foreach (DataGridViewColumn column in Columns)
            {
                if (column == _deleteRowColumn)
                {
                    continue;
                }

                DataGridViewCell cell = Rows[rowIndex].Cells[column.Index];
                cell.Selected = true;
                firstCell = firstCell ?? cell;

                if (column.Index == clickedColumnIndex)
                {
                    clickedCell = cell;
                }
            }

            CurrentCell = clickedCell ?? firstCell;
        }

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            _contextMenuCutItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCut"), null, (sender, e) => CutSelectionToClipboard());
            _contextMenuCopyItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCopy"),
                null,
                (sender, e) =>
                {
                    DataObject clipboardContent = GetClipboardContent();

                    if (clipboardContent != null)
                    {
                        Clipboard.SetDataObject(clipboardContent);
                    }
                });
            _contextMenuPasteItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuPaste"), null, (sender, e) => PasteFromClipboard());
            _contextMenuClearItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuClear"), null, (sender, e) => ClearSelectedCellValues());
            _contextMenuDeleteRowsItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuDeleteRows"), null, (sender, e) => DeleteSelectedRows());
            _contextMenuInsertRowAboveItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuInsertRowAbove"),
                null,
                (sender, e) => InsertBlankRow(_contextMenuRowIndex, above: true));
            _contextMenuInsertRowBelowItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuInsertRowBelow"),
                null,
                (sender, e) => InsertBlankRow(_contextMenuRowIndex, above: false));

            menu.Items.Add(_contextMenuCutItem);
            menu.Items.Add(_contextMenuCopyItem);
            menu.Items.Add(_contextMenuPasteItem);
            menu.Items.Add(_contextMenuClearItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuDeleteRowsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuInsertRowAboveItem);
            menu.Items.Add(_contextMenuInsertRowBelowItem);

            menu.Opening += (sender, e) =>
            {
                // No menu at all - not just disabled items - when the
                // right-click wasn't on any row (the header and empty
                // space below the rows both report RowIndex -1), or was
                // on the delete column (its own left-click already
                // deletes the row; none of Cut/Copy/Paste/Clear/insert
                // make sense on it either). The "type here to add a row"
                // placeholder still gets a menu - Paste still makes sense
                // there - just with every row-scoped item disabled below,
                // including both insert options: typing into the
                // placeholder is already how a row gets added there, so
                // there's nothing left for "insert row above/below" to do.
                bool onDeleteColumn = _deleteRowColumn != null && _contextMenuColumnIndex == _deleteRowColumn.Index;

                if (_contextMenuRowIndex < 0 || onDeleteColumn)
                {
                    e.Cancel = true;
                    return;
                }

                // _contextMenuRowWasPlaceholder (captured from IsNewRow at
                // click time), not a list.Count comparison here - the
                // right-click itself can already grow the bound list by
                // one before this Opening handler ever runs (see
                // InsertBlankRow), which would make a plain "row index <
                // list.Count" check wrongly call the placeholder a real
                // row too.
                bool onRealRow = !_contextMenuRowWasPlaceholder;
                bool hasSelection = SelectedCells.Count > 0;

                _contextMenuCutItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuCopyItem.Enabled = hasSelection && onRealRow;
                _contextMenuPasteItem.Enabled = !ReadOnly && Clipboard.ContainsText();
                _contextMenuClearItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuDeleteRowsItem.Enabled = hasSelection && AllowUserToDeleteRows && onRealRow;
                _contextMenuInsertRowAboveItem.Enabled = AllowUserToAddRows && onRealRow;
                _contextMenuInsertRowBelowItem.Enabled = AllowUserToAddRows && onRealRow;
            };

            return menu;
        }

        // Inserts one blank row immediately above or below rowIndex,
        // through the same batched suppress-events-then-ResetBindings-once
        // rebuild PasteFromClipboard's own row-insertion uses, for the
        // same reasons (reentrancy while still inside this event handler,
        // and a freshly AddNew()'d row otherwise being left in a
        // still-cancellable state the grid can silently drop later). The
        // context menu itself never offers either direction for the "type
        // here to add a row" placeholder - typing into it is already how
        // a row gets added there - but the guard below still covers it
        // defensively in case this is ever called some other way.
        private void InsertBlankRow(int rowIndex, bool above)
        {
            if (!(DataSource is IList list) || rowIndex < 0 || rowIndex > list.Count)
            {
                return;
            }

            if (!(list is IBindingList bindingList) || !bindingList.AllowNew)
            {
                return;
            }

            // Right-clicking (or even just clicking) the placeholder
            // itself already makes WinForms call IBindingList.AddNew() on
            // the bound list on its own - same cause as the paste bug
            // this same pattern fixed earlier (see PasteFromClipboard). If
            // that already grew the list past rowIndex, adding another row
            // here would insert two for one call.
            if (_contextMenuRowWasPlaceholder && rowIndex < list.Count)
            {
                return;
            }

            int insertAtIndex = above ? rowIndex : rowIndex + 1;

            ApplyBatchedDataSourceChange(list, () =>
            {
                List<object> originalItems = new List<object>(list.Count);

                foreach (object item in list)
                {
                    originalItems.Add(item);
                }

                object newItem = bindingList.AddNew();

                list.Clear();

                for (int i = 0; i < insertAtIndex; i++)
                {
                    list.Add(originalItems[i]);
                }

                list.Add(newItem);

                for (int i = insertAtIndex; i < originalItems.Count; i++)
                {
                    list.Add(originalItems[i]);
                }
            });
        }

        private void DeleteSelectedRows()
        {
            HashSet<int> rowIndexes = new HashSet<int>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (cell.RowIndex >= 0)
                {
                    rowIndexes.Add(cell.RowIndex);
                }
            }

            DeleteRows(rowIndexes);
        }

        private void DeleteRowAt(int rowIndex)
        {
            DeleteRows(new[] { rowIndex });
        }

        private void DeleteRows(IEnumerable<int> rowIndexes)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            List<object> itemsToRemove = new List<object>();

            foreach (int rowIndex in rowIndexes)
            {
                DataGridViewRow row = Rows[rowIndex];

                // The "type here to add a row" placeholder has no
                // DataBoundItem yet - nothing to remove from the data
                // source, and IsNewRow confirms it isn't a real row.
                if (row.IsNewRow || row.DataBoundItem == null)
                {
                    continue;
                }

                itemsToRemove.Add(row.DataBoundItem);
            }

            if (itemsToRemove.Count == 0)
            {
                return;
            }

            ApplyBatchedDataSourceChange(list, () =>
            {
                foreach (object item in itemsToRemove)
                {
                    list.Remove(item);
                }
            });
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
                column.HeaderText = StripSortSuffix(column.HeaderText);
            }

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ReorderDataSource(BuildOriginalOrder());
            }
            else
            {
                DataGridViewColumn sortedColumn = Columns[columnIndex];
                sortedColumn.HeaderCell.SortGlyphDirection = _sortOrder;
                sortedColumn.HeaderText = StripSortSuffix(sortedColumn.HeaderText) +
                    (_sortOrder == SortOrder.Ascending ? AscendingSuffix : DescendingSuffix);
                ReorderDataSource(BuildSortedOrder(columnIndex, _sortOrder));
            }

            ClearSelection();
            CurrentCell = null;
        }

        // SortGlyphDirection alone doesn't reliably render against this
        // control's dark custom styling (EnableHeadersVisualStyles = false
        // bypasses the OS glyph painting it normally rides along with), so
        // the direction is spelled out in the header text itself instead -
        // guaranteed visible regardless of theme.
        private const string AscendingSuffix = " ▲";
        private const string DescendingSuffix = " ▼";

        private static string StripSortSuffix(string headerText)
        {
            if (headerText == null)
            {
                return headerText;
            }

            if (headerText.EndsWith(AscendingSuffix, StringComparison.Ordinal))
            {
                return headerText.Substring(0, headerText.Length - AscendingSuffix.Length);
            }

            if (headerText.EndsWith(DescendingSuffix, StringComparison.Ordinal))
            {
                return headerText.Substring(0, headerText.Length - DescendingSuffix.Length);
            }

            return headerText;
        }

        // DataGridView.Sort(IComparer) is a no-op for the display order when
        // the control is data bound (it only works unbound, or against a
        // source that implements IBindingListView) - so instead, the bound
        // list itself is rewritten in the desired order. Any IList data
        // source works, not just BindingList&lt;T&gt;.
        // A plain Clear()+per-item Add() fires one change notification per
        // call - each one gets picked up and repainted immediately, which
        // includes the moment the list is briefly empty and whatever
        // DataGridView's own reaction to that Reset selects as a result.
        // That intermediate repaint is what showed up as the first row
        // flashing "selected" while a sort was applied. Suppressing
        // notifications for the whole rewrite and raising exactly one
        // Reset via ResetBindings() at the end (both BindingList&lt;T&gt;
        // members - public and protected respectively, reached via
        // reflection the same way EnableDoubleBuffering reaches a
        // protected Control property) collapses that into a single
        // repaint, with no intermediate state to flash.
        private static readonly BindingFlags AnyInstanceMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

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

        // Shared by ReorderDataSource and DeleteSelectedRows: runs a
        // multi-step change against the bound list (list.Clear()+Add() for
        // a reorder, list.Remove() per item for a delete) with change
        // notifications suppressed, then raises exactly one Reset via
        // ResetBindings() at the end (both BindingList&lt;T&gt; members -
        // public and protected respectively, reached via reflection the
        // same way EnableDoubleBuffering reaches a protected Control
        // property). Without this, each individual Clear()/Add()/Remove()
        // call gets picked up and repainted immediately - including
        // whatever intermediate state DataGridView reacts to along the
        // way (e.g. the list being briefly empty mid-reorder) - which is
        // what previously showed up as the first row flashing "selected".
        private void ApplyBatchedDataSourceChange(IList list, Action mutate)
        {
            // Rows can shift position without the mouse moving (e.g. the
            // hovered row itself gets deleted, or a sort reorders things
            // underneath the cursor) - no mouse-move means no fresh
            // OnCellMouseEnter/Leave to correct a now-stale index, so drop
            // the hover highlight instead of risking it landing on the
            // wrong row.
            _hoveredDeleteRowIndex = -1;

            object dataSource = DataSource;
            Type dataSourceType = dataSource.GetType();

            PropertyInfo raiseEventsProperty =
                dataSourceType.GetProperty("RaiseListChangedEvents", AnyInstanceMember);
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

                    MethodInfo resetBindingsMethod = dataSourceType.GetMethod(
                        "ResetBindings",
                        AnyInstanceMember,
                        null,
                        Type.EmptyTypes,
                        null);

                    resetBindingsMethod?.Invoke(dataSource, null);
                }

                _isApplyingInternalDataChange = false;
            }

            ClearSelection();
            CurrentCell = null;
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

            itemsWithValues.Sort((a, b) => CompareCellValues(a.Value, b.Value, direction));

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

        private static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        private static Color Lighten(Color color, int amount)
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
