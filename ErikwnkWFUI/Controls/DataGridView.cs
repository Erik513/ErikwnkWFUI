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
            // ReadOnlyDataGridView defaults to a display-only grid - this
            // is the editable one, so both get turned back to what a
            // consumer actually expects here. AllowUserToDeleteRows
            // doubles as this control's own opt-out for row deletion (see
            // ProcessDataGridViewKey) - a consumer that doesn't want rows
            // deletable sets this false, same as they would on a stock
            // DataGridView.
            ReadOnly = false;
            AllowUserToDeleteRows = true;

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
                if ((_deleteRowColumn != null && cell.ColumnIndex == _deleteRowColumn.Index) ||
                    Rows[cell.RowIndex].IsNewRow)
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
                    // See InsertItemsAt for why this rebuilds via Clear() +
                    // re-Add() rather than list.Insert() at a hand-picked
                    // index, and why originalItems has to be snapshotted
                    // before AddNew() below, not derived from list after.
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
                    int dataRowIndex = startDataRowIndex + rowOffset;

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
                if (_deleteRowColumn != null && cell.ColumnIndex == _deleteRowColumn.Index)
                {
                    continue;
                }

                // The placeholder is left fully selectable like any other
                // row (see the remarks on OnRowLeave) - a drag-select
                // reaching down that far could otherwise make this treat
                // it as the selection's bottommost row, and "insert row
                // below" would then try inserting past it.
                if (Rows[cell.RowIndex].IsNewRow)
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
                InsertItemsAt(list, originalItems, insertAtIndex, new[] { newItem });
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
    }
}
