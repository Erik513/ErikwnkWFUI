using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Keys, copying, cutting and clearing the selected cells.
    public partial class DataGridView : ReadOnlyDataGridView
    {
        // The base class claims Ctrl+C this way. Returning true keeps Ctrl+V and Ctrl+X from being taken as an unclaimed
        // shortcut and routes them to OnKeyDown below.
        /// <inheritdoc/>
        protected override bool IsInputKey(Keys keyData)
        {
            // Like OnKeyDown: on a ReadOnly grid the keys are not claimed, so they pass on as before.
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

        // Not in ProcessDataGridViewKey like Delete: the base OnKeyDown forwards only a fixed set of keys there
        // (navigation, C), and V and X are not among them. They are taken here, before base.OnKeyDown.
        /// <inheritdoc/>
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

        // The base class's Ctrl+C calls this too, so overriding it fixes copy as well as cut. Two kinds of cells are left
        // out of the copied text (the selection itself is restored in the finally below):
        // - the delete column's (its "X" can be selected in a drag, but means nothing),
        // - the "type here to add a row" placeholder's (it stays selectable, see OnRowLeave, but copying it would put a
        //   blank row on the clipboard).
        /// <inheritdoc/>
        public override DataObject GetClipboardContent()
        {
            return ExcludingSelectedCells(IsExcludedFromDataSelection, () => base.GetClipboardContent());
        }

        // Cut = copy, then clear (not delete the row, see Delete). The confirmations of both steps are suppressed: "copied"
        // and then "cleared" would be two toasts, neither saying "cut". One "cut" message is shown after both.
        private void CutSelectionToClipboard()
        {
            if (SelectedCells.Count == 0)
            {
                return;
            }

            // Nothing can be cleared (every selected cell is ReadOnly): the copy still happens and keeps its own "copied"
            // confirmation, since a "cut" message would never show.
            if (!HasClearableSelectedCell())
            {
                DataObject copyOnlyContent = GetClipboardContent();

                if (copyOnlyContent != null)
                {
                    ClipboardAccess.TrySetDataObject(copyOnlyContent);
                }

                return;
            }

            int clearedCount = 0;

            RunWithSuppressedActionConfirmation(() =>
            {
                DataObject clipboardContent = GetClipboardContent();

                // (A cut whose copy did not work clears nothing: the cells would be gone.)
                if (clipboardContent != null && !ClipboardAccess.TrySetDataObject(clipboardContent))
                {
                    return;
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

        // The "clear" half of Cut, for its own menu entry ("Delete" clears the cell contents; the rows are deleted by
        // "Delete selected rows"). It returns the count, so that Cut can build its message while this confirmation is
        // suppressed (RunWithSuppressedActionConfirmation).
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
