using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Adds row adding/deleting, an optional delete-row column, clipboard
    /// cut/copy/paste, and a right-click context menu on top of
    /// <see cref="ReadOnlyDataGridView"/> - the control to reach for when a
    /// grid needs to be editable, not just shown. Genuine WinForms controls
    /// throughout (not a thin wrapper around a native Win32 common control
    /// the way ListView is), so unlike ListView this needs none of that
    /// header-subclassing/WM_PAINT-interception - every visual either class
    /// has is reachable through its own style properties.
    /// </summary>
    /// <remarks>
    /// Named the same as the stock <see cref="System.Windows.Forms.DataGridView"/>
    /// it's ultimately built on (a WinForms convention this library is
    /// moving toward, replacing the old "Styled" prefix) - unambiguous here
    /// since this class's own base is <see cref="ReadOnlyDataGridView"/>,
    /// not that stock class directly. Consumers never need to spell out
    /// <c>ErikwnkWFUI.Controls.DataGridView</c> either: go through
    /// <see cref="UIStyles.DataGridViews.CreateStandard"/>, which hands back
    /// a plain <see cref="System.Windows.Forms.DataGridView"/>-typed
    /// reference.
    /// </remarks>
    public class DataGridView : ReadOnlyDataGridView
    {
        private DataGridViewColumn _deleteRowColumn;
        private int _hoveredDeleteRowIndex = -1;
        private int _contextMenuRowIndex = -1;
        private int _contextMenuColumnIndex = -1;

        // The one, single way this class ever decides "is this row the
        // 'type here to add a row' placeholder, not real data" - every
        // command that must only ever act on real rows (copy, cut, clear,
        // paste, delete, insert-above/below, the context menu's own
        // enable-state) goes through this, never IsNewRow/DataBoundItem/a
        // list.Count comparison directly. NewRowIndex is WinForms' own
        // authoritative answer to "which row index is currently the
        // placeholder" (-1 when AllowUserToAddRows is off) - unlike
        // IsNewRow or DataBoundItem, which can each disagree about a row
        // that's mid-way through becoming real (e.g. merely tabbing or
        // drag-selecting through the placeholder, with nothing typed into
        // it, already makes WinForms call IBindingList.AddNew() on the
        // bound list on its own), NewRowIndex stays correct through all of
        // that, which is exactly why every one of those commands now
        // shares this. A previous version of this file used three
        // different, deliberately non-interchangeable checks for this
        // instead, picked per call site by timing - that fragmentation
        // caused real bugs here more than once (paste crashes, vanishing/
        // duplicate rows, insert-above/below silently no-opping), which is
        // the whole reason this exists now.
        private bool IsPlaceholderRowIndex(int rowIndex)
        {
            return rowIndex >= 0 && rowIndex == NewRowIndex;
        }

        // A column whose cells hold no real data of the bound item's own -
        // the delete-row button, and (from ReadOnlyDataGridView) the
        // optional enumeration number - excluded everywhere a copy/paste/
        // right-click action would otherwise treat it like an ordinary
        // data column. Replaces what used to be a separate
        // "_deleteRowColumn != null && columnIndex == _deleteRowColumn.Index"
        // check repeated at each of these call sites on its own, which is
        // exactly why the enumeration column was still copyable/pasteable/
        // right-clickable despite being ReadOnly - ReadOnly alone only
        // blocks editing, none of these.
        private bool IsSystemColumn(int columnIndex)
        {
            return (_deleteRowColumn != null && columnIndex == _deleteRowColumn.Index) || IsEnumerationColumn(columnIndex);
        }

        // Captured at mouse-down time (see OnCellMouseDown) via
        // IsPlaceholderRowIndex - still needed as a snapshot, not just a
        // live re-check, because NewRowIndex itself can move between the
        // click and whenever the context menu's Opening handler or a menu
        // item's click actually runs (e.g. the click itself may already
        // have advanced it).
        private bool _contextMenuRowWasPlaceholder;
        // Matches Controls.ContextMenuStrip's own CreateStandard default.
        // BuildContextMenu reads this once, when it builds the menu in the
        // constructor. The property setter below also pushes a later
        // change straight into that already-built menu (found via the
        // inherited ContextMenuStrip property) - needed because
        // UIDataGridViewFactory.CreatePrimary sets this through an object
        // initializer, which only runs after the constructor already built
        // the menu - same pattern as ListView.ContextMenuSelectionColor.
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);
        private ToolStripMenuItem _contextMenuCutItem;
        private ToolStripMenuItem _contextMenuCopyItem;
        private ToolStripMenuItem _contextMenuPasteItem;
        private ToolStripMenuItem _contextMenuClearItem;
        private ToolStripMenuItem _contextMenuDeleteRowsItem;
        private ToolStripMenuItem _contextMenuInsertRowAboveItem;
        private ToolStripMenuItem _contextMenuInsertRowBelowItem;

        /// <summary>Fixed width of the optional delete-row column (see <see cref="ShowDeleteRowColumn"/>), in case a consumer needs to reserve space for it in its own column-width math.</summary>
        public const int DeleteRowColumnWidth = 40;

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

                    // Pinned rightmost (see OnColumnAdded below) - dragging
                    // it to reorder would fight that pinning right back, so
                    // it's excluded from reordering the same way it's
                    // already excluded from resizing (Resizable above).
                    SetColumnReorderable(column.Index, false);

                    // Resizable above is a fixed native width hint, but
                    // resizing itself is hand-rolled and tracked separately
                    // (see AllowColumnResizing's own remarks on why it
                    // can't just read that property back) - excluded here
                    // too, or the fixed width from Resizable/MinimumWidth/
                    // Width above would just get dragged away again.
                    SetColumnResizable(column.Index, false);
                }
                else
                {
                    Columns.Remove(_deleteRowColumn);
                    _deleteRowColumn = null;
                }
            }
        }

        /// <summary>
        /// Background of a hovered/selected item in this grid's own
        /// right-click context menu. Defaults to a fixed neutral gray,
        /// matching <see cref="Controls.ContextMenuStrip"/>'s own
        /// CreateStandard default; <see cref="Factories.UIDataGridViewFactory.CreatePrimary"/>
        /// sets this to <see cref="UIColors.Primary"/> instead, same pattern
        /// as <see cref="ReadOnlyDataGridView.BorderColor"/>.
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

        public DataGridView()
        {
            // ReadOnlyDataGridView defaults to a display-only grid - this
            // is the editable one, so both get turned back to what a
            // consumer actually expects here. AllowUserToDeleteRows
            // doubles as this control's own opt-out for row deletion (see
            // ProcessDataGridViewKey) - a consumer that doesn't want rows
            // deletable sets this false, same as they would on a stock
            // DataGridView.
            ReadOnly = false;
            AllowUserToDeleteRows = true;
            AllowUserToAddRows = true;

            MouseDown += HandleContextMenuMouseDown;
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

        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);

            if (e.Column == _deleteRowColumn)
            {
                // SortMode/width etc. are already set on the column object
                // itself (see ShowDeleteRowColumn) - only the display
                // position needs handling here, same as below. base's own
                // OnColumnAdded already left this column's SortMode alone
                // (it was already NotSortable by the time it ran), and
                // applied the EditHintTextBoxCell swap harmlessly - this
                // column's cells never actually reach the "type here to
                // add a row" placeholder row logic that hint is for.
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
        }

        // Separate from ReadOnlyDataGridView's own MouseDown subscription
        // (HandleMouseDown there only clears a stray selection) - this one
        // only needs to run for a right-click landing on truly empty space
        // (no cell there at all, so OnCellMouseDown below never fires),
        // to forget which row/column the context menu was about to target.
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

        // The "type here to add a row" placeholder is left exactly as
        // selectable as stock DataGridView already makes it - a drag-
        // select or Ctrl+A reaching down that far selects it like any
        // other row, same as it always would. An earlier version of this
        // fought that instead (forcing it back out of SelectedCells
        // on every selection change), which fixed one bug but caused a
        // worse one: reading/mutating selection state from within
        // OnSelectionChanged while WinForms' own selection-building loop
        // was still actively extending a drag turned out to disturb that
        // loop badly enough to drop the drag's own anchor cell from the
        // final selection entirely. The actual correctness problems this
        // was trying to solve - copy/paste/clear/delete treating the
        // placeholder as real data - are handled individually at each of
        // those call sites instead (see GetClipboardContent,
        // PasteFromClipboard, ClearSelectedCellValues, DeleteRows), each
        // already checking IsNewRow or list membership on its own terms;
        // none of them need the placeholder to be actually unselectable
        // for that.
        //
        // The one thing genuinely worth cleaning up here: merely having
        // moved through the placeholder at all - a drag passing over it,
        // Ctrl+A, arrow-keying past it, not just clicking directly into
        // it - already makes WinForms call IBindingList.AddNew() on the
        // bound list (same cause as InsertBlankRow/PasteFromClipboard).
        // If nothing was actually typed there, that leaves a genuinely
        // blank item sitting in the list, invisible for now (that row
        // still reports IsNewRow == true, same as ever, until something
        // commits it) - but no longer just a harmless placeholder either:
        // anything that later rebuilds the grid from the list's current
        // contents (e.g. a paste) reveals it as a genuine extra blank
        // row. Leaving the row - rather than every keystroke within it -
        // is the natural, standard point to settle whether it should
        // stick around: RowValidating/CellEndEdit (which is what commits
        // a real edit) already runs before RowLeave, so by the time this
        // fires, CancelNew is a documented no-op for a row that was
        // actually typed into - it only undoes an add nobody meant to
        // make. Deferred via BeginInvoke since CancelNew's own list
        // mutation would otherwise reenter this same row-leave handling
        // while it's still on the call stack.
        protected override void OnRowLeave(DataGridViewCellEventArgs e)
        {
            base.OnRowLeave(e);

            if (e.RowIndex != NewRowIndex || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                if (!(DataSource is ICancelAddNew cancelAddNew) || !(DataSource is IList list) || list.Count == 0)
                {
                    return;
                }

                int countBeforeCancel = list.Count;
                cancelAddNew.CancelNew(list.Count - 1);

                if (list.Count < countBeforeCancel)
                {
                    // A pending add really was cancelled (the no-op case,
                    // e.g. the user actually typed a new row and moved
                    // on, leaves list.Count unchanged and skips this) -
                    // CancelNew's own list-shrink is itself enough of a
                    // change to make WinForms pick some "current" cell of
                    // its own afterward (observed: the first cell of what
                    // was the last real row), which nobody asked for.
                    // Sweeping it up here, rather than leaving it for
                    // whatever happens to run next, keeps this contained
                    // to exactly the case it caused.
                    ClearSelection();
                    CurrentCell = null;
                }
            }));
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseDown(e);

            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            _contextMenuRowIndex = e.RowIndex;
            _contextMenuColumnIndex = e.ColumnIndex;

            _contextMenuRowWasPlaceholder = IsPlaceholderRowIndex(e.RowIndex);

            bool isSystemColumnCell = IsSystemColumn(e.ColumnIndex);

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
            // where to act) - only the delete/enumeration columns are
            // skipped, same as the menu's own Opening handler below
            // never showing a menu there at all.
            if (e.RowIndex >= 0 && !isSystemColumnCell && !IsRowSelected(e.RowIndex))
            {
                SelectRow(e.RowIndex, e.ColumnIndex);
            }
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

            // Captured as the actual bound item, not just e.RowIndex - the
            // delete itself is deferred (see below), and if two delete-glyph
            // clicks land before either deferred call runs, resolving by a
            // raw row index later would hit whatever row has shifted into
            // that index after the first delete already ran, not the row
            // that was actually clicked.
            object item = Rows[e.RowIndex].DataBoundItem;

            // Deferred rather than deleted right here: this fires from
            // inside the clicked cell's own click handling (still on the
            // call stack), and ApplyBatchedDataSourceChange's
            // ResetBindings rebuilds the entire Rows collection - including
            // discarding the very row/cell whose click handling is what's
            // currently running. BeginInvoke runs the delete once that
            // finishes unwinding instead of while it's still live.
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() => DeleteItem(item)));
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

            if (IsPlaceholderRowIndex(e.RowIndex))
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
                ? UIColors.Lighten(UIColors.Red, 40)
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

        // See ApplyBatchedDataSourceChange's own remarks on this hook -
        // rows can shift position without the mouse moving (e.g. the
        // hovered row itself gets deleted, or a sort reorders things
        // underneath the cursor), so the stale hover index is dropped
        // rather than risking it landing on the wrong row afterward.
        protected override void OnApplyingBatchedDataSourceChange()
        {
            base.OnApplyingBatchedDataSourceChange();
            _hoveredDeleteRowIndex = -1;
        }

        // Base DataGridView's own IsInputKey already claims Ctrl+C this
        // same way (that's what having ClipboardCopyMode enabled actually
        // does under the hood) - returning true here is what stops Windows
        // treating a key as an unclaimed shortcut/mnemonic and keeps it
        // routing to this control's own OnKeyDown below instead, which is
        // where Ctrl+V/Ctrl+X actually get handled.
        protected override bool IsInputKey(Keys keyData)
        {
            // Matches OnKeyDown's own !ReadOnly condition below - claiming
            // these keys on a ReadOnly grid (where OnKeyDown never acts on
            // them anyway) would just swallow Ctrl+V/Ctrl+X silently
            // instead of letting them pass through as an unclaimed
            // shortcut, same as before this control had any clipboard
            // support at all.
            if (!ReadOnly && (keyData & Keys.Control) == Keys.Control)
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
        /// pressed - RowHeadersVisible is false on this control (see
        /// <see cref="ReadOnlyDataGridView"/>'s constructor), so the base
        /// DataGridView's own Delete handling (which acts on
        /// <see cref="System.Windows.Forms.DataGridView.SelectedRows"/>,
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
        // build what it puts on the clipboard. Two kinds of cells are
        // excluded here regardless of how the request came in:
        //
        // - The delete column's: real values ("✕") and can end up
        //   selected like any other cell (e.g. a drag-select spanning the
        //   whole row), but there's nothing meaningful to copy from a
        //   column that only ever deletes rows.
        //
        // - The "type here to add a row" placeholder's: left fully
        //   selectable like any other row (see the remarks on
        //   OnRowLeave), so a drag-select or Ctrl+A reaching down that
        //   far selects it same as stock DataGridView always would - but
        //   it's still entirely blank, so copying it along would put one
        //   extra, entirely blank row on the clipboard, and from there
        //   into whatever gets pasted.
        //
        // Both are only excluded from the copied TEXT, not left
        // deselected afterward - restored in the `finally` below either
        // way, so the visible selection looks exactly like it would
        // without this override.
        public override DataObject GetClipboardContent()
        {
            List<DataGridViewCell> excludedCells = new List<DataGridViewCell>();

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsSystemColumn(cell.ColumnIndex) || IsPlaceholderRowIndex(cell.RowIndex))
                {
                    excludedCells.Add(cell);
                }
            }

            if (excludedCells.Count == 0)
            {
                return base.GetClipboardContent();
            }

            foreach (DataGridViewCell cell in excludedCells)
            {
                cell.Selected = false;
            }

            try
            {
                return base.GetClipboardContent();
            }
            finally
            {
                foreach (DataGridViewCell cell in excludedCells)
                {
                    cell.Selected = true;
                }
            }
        }

        // Cut = copy (via the same clipboard content the base class's own
        // Ctrl+C builds) then clear, rather than deleting the row outright -
        // that's a different, already-existing action (see Delete above).
        // The copy and clear steps' own confirmations are suppressed here -
        // "copied" followed by "cleared" would be a confusing double toast,
        // and neither wording actually says "cut" - so a single, dedicated
        // "cut" message is shown instead, once, after both steps complete.
        private void CutSelectionToClipboard()
        {
            if (SelectedCells.Count == 0)
            {
                return;
            }

            int clearedCount = 0;

            RunWithSuppressedActionConfirmation(() =>
            {
                DataObject clipboardContent = GetClipboardContent();

                if (clipboardContent != null)
                {
                    Clipboard.SetDataObject(clipboardContent);
                }

                clearedCount = ClearSelectedCellValues();
            });

            if (clearedCount == 0)
            {
                return;
            }

            string message = clearedCount == 1
                ? UIStrings.Get("DataGridView.CellCut")
                : string.Format(UIStrings.Get("DataGridView.CellsCut"), clearedCount);
            ShowActionConfirmation(message);
        }

        // Just the "clear" half of Cut - its own context-menu entry
        // ("Delete", as in clear the cell contents, not delete the row -
        // that's the separate "Delete selected rows" entry) needs it
        // without also touching the clipboard. Returns the cleared count so
        // Cut can build its own message from it while this method's own
        // confirmation is suppressed (see RunWithSuppressedActionConfirmation).
        private int ClearSelectedCellValues()
        {
            int clearedCount = 0;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (!cell.ReadOnly && cell.RowIndex >= 0 && !IsPlaceholderRowIndex(cell.RowIndex))
                {
                    cell.Value = null;
                    clearedCount++;
                }
            }

            if (clearedCount == 0)
            {
                return clearedCount;
            }

            string message = clearedCount == 1
                ? UIStrings.Get("DataGridView.CellCleared")
                : string.Format(UIStrings.Get("DataGridView.CellsCleared"), clearedCount);
            ShowActionConfirmation(message);
            return clearedCount;
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

            // Copying exactly one cell, then pasting into a selection of
            // more than one, fills every selected cell with that same
            // value (standard spreadsheet "fill" behavior) instead of
            // anchoring at the top-left and only ever writing the one
            // value there. Copying more than one cell never fills/tiles a
            // larger selection this way, regardless of its size - it's
            // always just anchored at the top-left and extends from there
            // (same as always), matching Excel's own paste behavior.
            if (pastedRows.Length == 1 && pastedRows[0].Split('\t').Length == 1)
            {
                // Captured as plain (row, column) index pairs, not the
                // DataGridViewCell references themselves - FillCellsWithValue
                // cancels a pending placeholder add before writing, and a
                // DataGridViewCell held onto across that mutation turned out
                // to no longer resolve to the row it was read from.
                List<(int RowIndex, int ColumnIndex)> fillTargetCells = new List<(int, int)>();
                bool fillTouchedPlaceholder = false;

                foreach (DataGridViewCell cell in SelectedCells)
                {
                    if (IsSystemColumn(cell.ColumnIndex))
                    {
                        continue;
                    }

                    if (IsPlaceholderRowIndex(cell.RowIndex))
                    {
                        fillTouchedPlaceholder = true;
                        continue;
                    }

                    fillTargetCells.Add((cell.RowIndex, cell.ColumnIndex));
                }

                if (fillTargetCells.Count > 1)
                {
                    FillCellsWithValue(list, fillTargetCells, pastedRows[0], fillTouchedPlaceholder);
                    return;
                }
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

            // Set when the placeholder was part of what got interacted
            // with (selected, or left as CurrentCell) - merely that, with
            // nothing typed, already made WinForms call
            // IBindingList.AddNew() on the bound list on its own (see
            // InsertBlankRow's own comment on this). The placeholder
            // itself is still correctly excluded everywhere below either
            // way; this only tracks whether that now-pending, still-empty
            // item needs cancelling so it doesn't linger as a permanent
            // stray row once this method is done (see where it's used,
            // further down).
            bool placeholderWasTouched = false;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsSystemColumn(cell.ColumnIndex))
                {
                    continue;
                }

                // The COLUMN anchor is tracked regardless of whether this
                // cell's row is the placeholder - selecting column 2 of
                // the placeholder and pasting must still start at column
                // 2 for whatever real row the paste ends up writing/
                // inserting, not silently fall back to column 0 just
                // because that particular cell's row got excluded below.
                DataGridViewColumn column = Columns[cell.ColumnIndex];

                if (minColumn == null || column.DisplayIndex < minColumn.DisplayIndex)
                {
                    minColumn = column;
                }

                // The placeholder is never a paste target itself, same as
                // the delete column - a selection that includes it (e.g. a
                // drag-select or Ctrl+A reaching down that far) must only
                // ever affect the real rows above it.
                if (IsPlaceholderRowIndex(cell.RowIndex))
                {
                    placeholderWasTouched = true;
                    continue;
                }

                if (cell.RowIndex < minRowIndex)
                {
                    minRowIndex = cell.RowIndex;
                }

                selectedRealRowIndexes.Add(cell.RowIndex);
            }

            if (minColumn == null && CurrentCell != null)
            {
                minColumn = CurrentCell.OwningColumn;

                if (IsPlaceholderRowIndex(CurrentCell.RowIndex))
                {
                    placeholderWasTouched = true;
                }
                else
                {
                    minRowIndex = CurrentCell.RowIndex;
                    selectedRealRowIndexes.Add(CurrentCell.RowIndex);
                }
            }

            int startColumnIndex = minColumn != null ? targetColumns.IndexOf(minColumn) : -1;

            if (startColumnIndex < 0)
            {
                startColumnIndex = 0;
            }

            // Only the rows actually selected get overwritten in place - a
            // paste block taller than the selection gets the rest INSERTED
            // right after it instead of overwriting whatever real rows
            // happened to already be sitting there (e.g. pasting 10 rows
            // onto a 4-row selection in the middle of a 20-row list must
            // not clobber rows 5-10 of someone's existing data; it should
            // push them down by 6 instead).
            // Sorted so the write loop below can target the actual
            // selected rows in order, not just walk sequentially from
            // startDataRowIndex - a non-contiguous selection (e.g. rows 0
            // and 5 only) must overwrite exactly those rows, not rows 0
            // and 1.
            List<int> sortedSelectedRowIndexes = new List<int>(selectedRealRowIndexes);
            sortedSelectedRowIndexes.Sort();

            IBindingList bindingList = list as IBindingList;

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
                // Discards a pending-but-abandoned placeholder add before
                // anything below reads list.Count - the placeholder is
                // purely a visual way to add rows by typing, and pasting
                // instead of typing means that pending item was never
                // really wanted (same reasoning as DeleteRows' own
                // placeholderIndexesToCancel). Must run first, before
                // anything below reads list.Count - startDataRowIndex's own
                // fallback (nothing selected -> append at the end) and the
                // overflow/trim math right after it both need to see the
                // post-cancel count, not the stale one from before this
                // batch started.
                if (placeholderWasTouched && list is ICancelAddNew cancelAddNew)
                {
                    cancelAddNew.CancelNew(list.Count - 1);
                }

                // Every index in selectedRealRowIndexes/minRowIndex is
                // already guaranteed to be a real row (the placeholder was
                // excluded above, before either was ever populated) -
                // nothing selected at all (a plain append at the end of
                // the list) is the only remaining case minRowIndex's
                // sentinel has to cover.
                int startDataRowIndex = minRowIndex != int.MaxValue ? minRowIndex : list.Count;

                // Where overflow rows land once InsertItemsAt grows the
                // list - computed here (not just inside the overflow
                // branch below) so the write loop can use the same value
                // regardless of whether growth actually happened.
                int insertAtIndex = startDataRowIndex + selectedRealRowIndexes.Count;

                int overflowRowCount = pastedRows.Length - selectedRealRowIndexes.Count;

                if (overflowRowCount > 0 && (bindingList == null || !bindingList.AllowNew))
                {
                    // Can't grow the list - still worth filling whatever
                    // existing rows the paste does reach below.
                    pastedRows = Trim(pastedRows, list.Count - startDataRowIndex);
                    overflowRowCount = 0;
                }

                if (overflowRowCount > 0)
                {
                    // See InsertItemsAt for why this rebuilds via Clear() +
                    // re-Add() rather than list.Insert() at a hand-picked
                    // index, and why originalItems has to be snapshotted
                    // before AddNew() below, not derived from list after.
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

                    InsertItemsAt(list, originalItems, insertAtIndex, newItems);
                }

                // Looked up once per (item type, property name) rather
                // than once per pasted cell - GetProperty for the same
                // column resolves to the same PropertyInfo on every row
                // for the common case of a homogeneous bound list, so
                // repeating that lookup per row was pure waste on a paste
                // of any real size. Keyed by the item's own runtime type
                // (not just the property name) so a list that genuinely
                // mixes item types - unusual, but IList doesn't rule it
                // out - still resolves correctly per item.
                Dictionary<(Type ItemType, string PropertyName), PropertyInfo> propertyCache =
                    new Dictionary<(Type, string), PropertyInfo>();

                for (int rowOffset = 0; rowOffset < pastedRows.Length; rowOffset++)
                {
                    // Write into the actually-selected rows first, in
                    // ascending order (not sequentially from
                    // startDataRowIndex), then into the newly-inserted
                    // overflow rows once the selection is exhausted.
                    int dataRowIndex = rowOffset < sortedSelectedRowIndexes.Count
                        ? sortedSelectedRowIndexes[rowOffset]
                        : insertAtIndex + (rowOffset - sortedSelectedRowIndexes.Count);

                    if (dataRowIndex >= list.Count)
                    {
                        break;
                    }

                    object targetItem = list[dataRowIndex];
                    Type targetItemType = targetItem.GetType();
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

                        (Type, string) propertyCacheKey = (targetItemType, column.DataPropertyName);

                        if (!propertyCache.TryGetValue(propertyCacheKey, out PropertyInfo property))
                        {
                            property = targetItemType.GetProperty(column.DataPropertyName);
                            propertyCache[propertyCacheKey] = property;
                        }

                        if (property == null)
                        {
                            continue;
                        }

                        // Pasted text is always a raw string - property is
                        // whatever type the bound item's own property
                        // actually is (int, decimal, bool, an enum, ...).
                        // SetValue throws instead of converting on its own,
                        // so pasting into any non-string column threw
                        // before this. A cell that fails to convert (e.g.
                        // pasting "abc" into a number column) is skipped
                        // rather than aborting the whole paste, the same
                        // "best effort" spirit as the rest of this method.
                        if (TryConvertPastedValue(cellValues[columnOffset], property.PropertyType, out object convertedValue))
                        {
                            property.SetValue(targetItem, convertedValue);
                        }
                    }
                }
            });

            string message = pastedRows.Length == 1
                ? UIStrings.Get("DataGridView.RowPasted")
                : string.Format(UIStrings.Get("DataGridView.RowsPasted"), pastedRows.Length);
            ShowActionConfirmation(message);
        }

        private static bool TryConvertPastedValue(string text, Type targetType, out object convertedValue)
        {
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (string.IsNullOrEmpty(text))
            {
                // Blank pasted into a nullable/reference column clears it;
                // a non-nullable value type has no such thing as "blank",
                // so that cell is left alone instead of guessing a default.
                bool canBeNull = targetType != underlyingType || !underlyingType.IsValueType;
                convertedValue = null;
                return canBeNull;
            }

            if (underlyingType == typeof(string))
            {
                convertedValue = text;
                return true;
            }

            try
            {
                convertedValue = underlyingType.IsEnum
                    ? Enum.Parse(underlyingType, text, ignoreCase: true)
                    : Convert.ChangeType(text, underlyingType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is ArgumentException)
            {
                convertedValue = null;
                return false;
            }
        }

        // Writes the same value into every one of targetCells (the "fill"
        // path PasteFromClipboard uses when exactly one cell was copied
        // into a selection of more than one) - never grows the list, since
        // every cell here is already a real, existing row.
        private void FillCellsWithValue(IList list, List<(int RowIndex, int ColumnIndex)> targetCells, string value, bool cancelPendingPlaceholder)
        {
            ApplyBatchedDataSourceChange(list, () =>
            {
                // Same reasoning as PasteFromClipboard's own placeholder
                // cancellation - a pending add from merely including the
                // placeholder in the selection was never really wanted
                // once the user pastes instead of types.
                if (cancelPendingPlaceholder && list is ICancelAddNew cancelAddNew)
                {
                    cancelAddNew.CancelNew(list.Count - 1);
                }

                Dictionary<(Type ItemType, string PropertyName), PropertyInfo> propertyCache =
                    new Dictionary<(Type, string), PropertyInfo>();

                foreach ((int rowIndex, int columnIndex) in targetCells)
                {
                    if (rowIndex >= list.Count)
                    {
                        continue;
                    }

                    DataGridViewColumn column = Columns[columnIndex];

                    if (column.ReadOnly || string.IsNullOrEmpty(column.DataPropertyName))
                    {
                        continue;
                    }

                    object targetItem = list[rowIndex];
                    Type targetItemType = targetItem.GetType();
                    (Type, string) propertyCacheKey = (targetItemType, column.DataPropertyName);

                    if (!propertyCache.TryGetValue(propertyCacheKey, out PropertyInfo property))
                    {
                        property = targetItemType.GetProperty(column.DataPropertyName);
                        propertyCache[propertyCacheKey] = property;
                    }

                    if (property == null)
                    {
                        continue;
                    }

                    if (TryConvertPastedValue(value, property.PropertyType, out object convertedValue))
                    {
                        property.SetValue(targetItem, convertedValue);
                    }
                }
            });

            ShowActionConfirmation(string.Format(UIStrings.Get("DataGridView.CellsPasted"), targetCells.Count));
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
                if (!IsSystemColumn(column.Index))
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

        // "Insert row above/below" (see BuildContextMenu) needs the top
        // and bottom of the WHOLE current selection, not just the single
        // row that happened to be right-clicked (_contextMenuRowIndex) -
        // right-clicking a row that's already part of a larger selection
        // leaves that selection intact (see OnCellMouseDown), so
        // right-clicking anywhere within a multi-row selection has to
        // insert above its topmost row / below its bottommost row, not
        // wherever the cursor happened to land inside it. Returns false
        // (leaving both out parameters unset) when nothing is selected -
        // shouldn't normally happen for a row-targeted right-click (see
        // OnCellMouseDown), but callers fall back to _contextMenuRowIndex
        // for that case regardless.
        private bool TryGetSelectedRowIndexRange(out int minRowIndex, out int maxRowIndex)
        {
            minRowIndex = int.MaxValue;
            maxRowIndex = -1;

            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsSystemColumn(cell.ColumnIndex))
                {
                    continue;
                }

                // The placeholder is left fully selectable like any other
                // row (see the remarks on OnRowLeave) - a drag-select
                // reaching down that far could otherwise make this treat
                // it as the selection's bottommost row, and "insert row
                // below" would then try inserting past it.
                if (IsPlaceholderRowIndex(cell.RowIndex))
                {
                    continue;
                }

                if (cell.RowIndex < minRowIndex)
                {
                    minRowIndex = cell.RowIndex;
                }

                if (cell.RowIndex > maxRowIndex)
                {
                    maxRowIndex = cell.RowIndex;
                }
            }

            return maxRowIndex >= 0;
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
            // None of this menu's items ever get an Image, so the native
            // reserved left-hand icon gutter just showed up as a blank
            // strip nothing ever used - same fix as ListView.BuildContextMenu.
            ContextMenuStrip menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                SelectionBackColor = ContextMenuSelectionColor
            };

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
                (sender, e) =>
                {
                    int rowIndex = TryGetSelectedRowIndexRange(out int minRowIndex, out int _)
                        ? minRowIndex
                        : _contextMenuRowIndex;
                    InsertBlankRow(rowIndex, above: true);
                });
            _contextMenuInsertRowBelowItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuInsertRowBelow"),
                null,
                (sender, e) =>
                {
                    int rowIndex = TryGetSelectedRowIndexRange(out int _, out int maxRowIndex)
                        ? maxRowIndex
                        : _contextMenuRowIndex;
                    InsertBlankRow(rowIndex, above: false);
                });

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
                // there, and so can insert row above/below if it's part
                // of a larger selection that also reaches real rows (see
                // hasRealRowSelected below) - only the row-scoped items
                // tied to a specific real row (Cut/Copy/Clear/Delete
                // selected rows) are unconditionally disabled below when
                // the clicked row itself was the placeholder.
                bool onSystemColumn = IsSystemColumn(_contextMenuColumnIndex);

                if (_contextMenuRowIndex < 0 || onSystemColumn)
                {
                    e.Cancel = true;
                    return;
                }

                bool onRealRow = !_contextMenuRowWasPlaceholder;
                bool hasSelection = SelectedCells.Count > 0;

                _contextMenuCutItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuCopyItem.Enabled = hasSelection && onRealRow;
                _contextMenuPasteItem.Enabled = !ReadOnly && Clipboard.ContainsText();
                _contextMenuClearItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuDeleteRowsItem.Enabled = hasSelection && AllowUserToDeleteRows && onRealRow;

                // Not onRealRow here - that's about whether the row
                // actually right-clicked was the placeholder, but insert
                // above/below now act on the whole selection's topmost/
                // bottommost row (see TryGetSelectedRowIndexRange), not
                // the clicked row itself. Right-clicking the placeholder
                // while it's part of a larger selection that also has
                // real rows in it (a drag reaching down that far) should
                // still offer both, targeting those real rows - only
                // disabled when there's genuinely no real row anywhere in
                // the selection to insert relative to.
                bool hasRealRowSelected = TryGetSelectedRowIndexRange(out int _, out int _);
                _contextMenuInsertRowAboveItem.Enabled = AllowUserToAddRows && hasRealRowSelected;
                _contextMenuInsertRowBelowItem.Enabled = AllowUserToAddRows && hasRealRowSelected;
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
            // here would insert two for one call. Only skips when rowIndex
            // is the SAME row that was actually right-clicked
            // (_contextMenuRowIndex) - rowIndex can also be a different,
            // real row resolved from a multi-row selection that happens to
            // include the placeholder (see TryGetSelectedRowIndexRange),
            // and that row was never auto-grown by the click at all.
            if (_contextMenuRowWasPlaceholder && rowIndex == _contextMenuRowIndex && rowIndex < list.Count)
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
                InsertItemsAt(list, originalItems, insertAtIndex, new[] { newItem });
            });

            ShowActionConfirmation(UIStrings.Get("DataGridView.RowInserted"));
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

        private void DeleteItem(object item)
        {
            if (item == null || !(DataSource is IList list))
            {
                return;
            }

            ApplyBatchedDataSourceChange(list, () => list.Remove(item));
            ShowActionConfirmation(UIStrings.Get("DataGridView.RowDeleted"));
        }

        private void DeleteRows(IEnumerable<int> rowIndexes)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            HashSet<int> indexesToRemove = new HashSet<int>();
            HashSet<int> placeholderIndexesToCancel = new HashSet<int>();

            foreach (int rowIndex in rowIndexes)
            {
                // The placeholder has nothing to remove from the data
                // source. Selecting it - even just landing CurrentCell on
                // it as part of a larger selection, no typing required -
                // already makes WinForms call IBindingList.AddNew() on the
                // bound list on its own, the same as a real click does
                // (see InsertBlankRow's own comment on this). Left alone,
                // that still-empty pending item would never get cleaned up
                // and would sit in the data source forever as a permanent
                // stray blank row - CancelNew below discards it instead,
                // same as WinForms' own native handling does when a
                // pending add is abandoned rather than committed.
                if (IsPlaceholderRowIndex(rowIndex))
                {
                    placeholderIndexesToCancel.Add(rowIndex);
                    continue;
                }

                indexesToRemove.Add(rowIndex);
            }

            if (indexesToRemove.Count == 0 && placeholderIndexesToCancel.Count == 0)
            {
                return;
            }

            ApplyBatchedDataSourceChange(list, () =>
            {
                // Cancelled first, not folded into the rebuild loop below -
                // the placeholder is always the LAST row, so discarding its
                // still-pending item (if any) never shifts the position of
                // any real row indexesToRemove refers to.
                if (list is ICancelAddNew cancelAddNew)
                {
                    foreach (int placeholderIndex in placeholderIndexesToCancel)
                    {
                        cancelAddNew.CancelNew(placeholderIndex);
                    }
                }

                // Single filtering pass instead of one list.Remove(item)
                // call per row - Remove is an O(n) IndexOf-then-shift on
                // List<T>/BindingList<T>, so removing m rows that way costs
                // O(n*m) instead of the O(n) this rebuild does. Filtered by
                // POSITION, not by item identity/HashSet<object> - two
                // different rows can legitimately be bound to the exact
                // same object reference, and a set of items would then
                // remove every occurrence instead of just the one(s)
                // actually selected.
                List<object> remainingItems = new List<object>(list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    if (!indexesToRemove.Contains(i))
                    {
                        remainingItems.Add(list[i]);
                    }
                }

                list.Clear();

                foreach (object item in remainingItems)
                {
                    list.Add(item);
                }
            });

            if (indexesToRemove.Count > 0)
            {
                string message = indexesToRemove.Count == 1
                    ? UIStrings.Get("DataGridView.RowDeleted")
                    : string.Format(UIStrings.Get("DataGridView.RowsDeleted"), indexesToRemove.Count);
                ShowActionConfirmation(message);
            }
        }
    }
}
