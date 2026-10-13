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

        // Adds the editing entries to the menu that ReadOnlyDataGridView built (copy and select all, the same in both grids
        // and in the ListView), so that the shared entries cannot drift apart. Cut goes in front, Paste and Clear behind
        // the copy entries, the row entries follow.
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
                // The base menu's Opening handler ran first and cancelled the menu where the click was on no row (header,
                // empty space) or on a system column. The placeholder still gets one: Paste works there, and Insert row
                // works if the selection reaches real rows (hasRealRowSelected). Only the entries tied to a real row (Cut,
                // Clear, Delete rows, the copy entries) are disabled when the clicked row is the placeholder.
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

                // Not onRealRow: Insert above/below work on the topmost and bottommost row of the whole selection
                // (TryGetSelectedRowIndexRange), not on the clicked row. They stay available when the placeholder is
                // clicked inside a selection that has real rows, and are off only when there is none.
                bool hasRealRowSelected = TryGetSelectedRowIndexRange(out int _, out int _);
                _contextMenuInsertRowAboveItem.Enabled = AllowUserToAddRows && hasRealRowSelected;
                _contextMenuInsertRowBelowItem.Enabled = AllowUserToAddRows && hasRealRowSelected;
            };
        }
    }
}
