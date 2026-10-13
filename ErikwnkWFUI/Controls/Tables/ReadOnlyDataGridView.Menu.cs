using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The right-click menu, copying to the clipboard, selecting all, and the confirmation that shows what was done.
    public partial class ReadOnlyDataGridView
    {

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


        // Says so when the header text went along too - the same
        // " (with header)" suffix ListView's own copy toast carries. The
        // clipboard mode itself is the signal (CopySelectionToClipboard
        // switches it for the "with header" entries), so a consumer who
        // sets EnableAlwaysIncludeHeaderText for Ctrl+C gets the suffix too.
        private string GetCopyConfirmationMessage(int copiedCellCount)
        {
            string message = copiedCellCount == 1
                ? UIStrings.Get("DataGridView.CellCopied")
                : string.Format(UIStrings.Get("DataGridView.CellsCopied"), copiedCellCount);

            return ClipboardCopyMode == DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
                ? message + UIStrings.Get("DataGridView.WithHeaderSuffix")
                : message;
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
                ShowImageMargin = false
            };

            menu.FollowSelectionBackColor(() => ContextMenuSelectionColor);

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
                UIStrings.Get("DataGridView.ContextMenuSelectAll"), null, (sender, e) => SelectAllRealCells());

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
                    ClipboardAccess.SetDataObject(content);
                }
            }
            finally
            {
                ClipboardCopyMode = previousMode;
            }
        }


        // Select all, minus the "type here to add a row" placeholder (only
        // ever present on the editable grid): it isn't a real row, so it
        // must not count as selected - neither for the copy/cut/delete
        // actions that follow, nor visually.
        private void SelectAllRealCells()
        {
            SelectAll();

            if (NewRowIndex >= 0)
            {
                foreach (DataGridViewCell cell in Rows[NewRowIndex].Cells)
                {
                    cell.Selected = false;
                }
            }
        }


        // Ctrl+A goes through the same path as the menu entry. Not while a
        // cell is being edited - there it selects the text in the editor.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A && !IsCurrentCellInEditMode)
            {
                SelectAllRealCells();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }


        private void CopyAllToClipboard(bool includeHeader)
        {
            SelectAllRealCells();
            CopySelectionToClipboard(includeHeader);
        }
    }
}
