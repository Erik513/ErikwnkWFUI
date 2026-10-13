using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The editing entries of the right-click menu and their texts.
    public partial class DataGridView : ReadOnlyDataGridView
    {

        // ContextMenuSelectionColor and the right-click row tracking
        // (_contextMenuRowIndex/_contextMenuColumnIndex/
        // _contextMenuRowWasPlaceholder, OnCellMouseDown's row selection)
        // all live on ReadOnlyDataGridView now - it has its own, smaller
        // right-click menu that needs exactly the same behavior.
        private ToolStripMenuItem _contextMenuCutItem;
        private ToolStripMenuItem _contextMenuPasteItem;
        private ToolStripMenuItem _contextMenuClearItem;
        private ToolStripMenuItem _contextMenuDeleteRowsItem;
        private ToolStripMenuItem _contextMenuCutRowsItem;
        private ToolStripMenuItem _contextMenuInsertRowAboveItem;
        private ToolStripMenuItem _contextMenuInsertRowBelowItem;

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
                _contextMenuPasteItem.Text = UIStrings.Get("DataGridView.ContextMenuPaste");
                _contextMenuClearItem.Text = UIStrings.Get("DataGridView.ContextMenuClear");
                _contextMenuCutRowsItem.Text = UIStrings.Get("DataGridView.ContextMenuCutRows");
                _contextMenuDeleteRowsItem.Text = UIStrings.Get("DataGridView.ContextMenuDeleteRows");
                _contextMenuInsertRowAboveItem.Text = UIStrings.Get("DataGridView.ContextMenuInsertRowAbove");
                _contextMenuInsertRowBelowItem.Text = UIStrings.Get("DataGridView.ContextMenuInsertRowBelow");
            }
        }

        // Adds this class's editing entries to the menu ReadOnlyDataGridView
        // already built (Copy selection / Copy all / Select all, identical
        // in both grids and in ListView) rather than building a second,
        // separate menu - so the shared entries can never drift apart
        // between the two. Cut goes in front, Paste/Clear right after the
        // copy entries (before Select all), and the row-level entries
        // follow, separated.
        private void AddEditingMenuItems()
        {
            System.Windows.Forms.ContextMenuStrip menu = ContextMenuStrip;

            _contextMenuCutItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCut"), null, (sender, e) => CutSelectionToClipboard());
            _contextMenuPasteItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuPaste"), null, (sender, e) => PasteFromClipboard());
            _contextMenuClearItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuClear"), null, (sender, e) => ClearSelectedCellValues());
            _contextMenuCutRowsItem = new ToolStripMenuItem(
                UIStrings.Get("DataGridView.ContextMenuCutRows"), null, (sender, e) => CutSelectedRows());
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

            // The base menu holds Copy, Copy with header | Copy all, Copy
            // all with header | Select all. Cut goes in front, Paste and
            // Delete right after the copy entries, then the row-level
            // entries (insert, delete rows) at the end, each group
            // separated.
            menu.Items.Insert(0, _contextMenuCutItem);
            menu.Items.Insert(3, _contextMenuPasteItem);
            menu.Items.Insert(4, _contextMenuClearItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuInsertRowAboveItem);
            menu.Items.Add(_contextMenuInsertRowBelowItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_contextMenuCutRowsItem);
            menu.Items.Add(_contextMenuDeleteRowsItem);

            menu.Opening += (sender, e) =>
            {
                // The base menu's own Opening handler already ran first and
                // cancelled the whole menu when the right-click wasn't on
                // any row (the header and empty space below the rows both
                // report RowIndex -1) or was on a system column (the delete
                // column's own left-click already deletes the row; nothing
                // else makes sense on it either). The "type here to add a
                // row" placeholder still gets a menu - Paste still makes
                // sense there, and so can insert row above/below if it's
                // part of a larger selection that also reaches real rows
                // (see hasRealRowSelected below) - only the row-scoped items
                // tied to a specific real row (Cut/Clear/Delete selected
                // rows, and the base menu's copy entries) are
                // unconditionally disabled when the clicked row itself was
                // the placeholder.
                if (e.Cancel)
                {
                    return;
                }

                bool onRealRow = !_contextMenuRowWasPlaceholder;
                bool hasSelection = SelectedCells.Count > 0;

                _contextMenuCutItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuPasteItem.Enabled = !ReadOnly && ClipboardAccess.ContainsText();
                _contextMenuClearItem.Enabled = hasSelection && !ReadOnly && onRealRow;
                _contextMenuCutRowsItem.Enabled = hasSelection && AllowUserToDeleteRows && onRealRow;
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
        }
    }
}
