using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Keys, copying, cutting and clearing the selected cells.
    public partial class DataGridView : ReadOnlyDataGridView
    {

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
            return ExcludingSelectedCells(IsExcludedFromDataSelection, () => base.GetClipboardContent());
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

            // Nothing here can actually be cleared (e.g. every selected
            // cell is ReadOnly) - the copy half still puts the values on the
            // clipboard, same as a plain Ctrl+C on that selection would,
            // so it keeps its own normal "copied" confirmation instead of
            // being suppressed in favor of a "cut" message that would
            // never show (clearedCount would be 0), which used to leave a
            // successful copy with no feedback at all.
            if (!HasClearableSelectedCell())
            {
                DataObject copyOnlyContent = GetClipboardContent();

                if (copyOnlyContent != null)
                {
                    ClipboardAccess.SetDataObject(copyOnlyContent);
                }

                return;
            }

            int clearedCount = 0;

            RunWithSuppressedActionConfirmation(() =>
            {
                DataObject clipboardContent = GetClipboardContent();

                if (clipboardContent != null)
                {
                    ClipboardAccess.SetDataObject(clipboardContent);
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

        private bool IsClearable(DataGridViewCell cell)
        {
            return !cell.ReadOnly && cell.RowIndex >= 0 && !IsPlaceholderRowIndex(cell.RowIndex);
        }

        private bool HasClearableSelectedCell()
        {
            foreach (DataGridViewCell cell in SelectedCells)
            {
                if (IsClearable(cell))
                {
                    return true;
                }
            }

            return false;
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
                if (IsClearable(cell))
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
    }
}
