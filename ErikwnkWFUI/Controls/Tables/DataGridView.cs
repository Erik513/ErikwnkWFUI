using System.Windows.Forms;
using ErikwnkWFUI.Localization;

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
    // In files by task: this one (the construction), DataGridView.DeleteColumn.cs (the column with the X), DataGridView.Clipboard.cs
    // (keys, copy, cut, clear), DataGridView.Paste.cs, DataGridView.Rows.cs (inserting and deleting rows) and DataGridView.Menu.cs.
    public partial class DataGridView : ReadOnlyDataGridView
    {
        // The base class's own IsSystemColumn (the enumeration column) plus
        // this class's delete-row button - see ReadOnlyDataGridView's own
        // remarks on what "system column" means everywhere it's used.
        /// <inheritdoc/>
        protected override bool IsSystemColumn(int columnIndex)
        {
            return base.IsSystemColumn(columnIndex) || (_deleteRowColumn != null && columnIndex == _deleteRowColumn.Index);
        }

        // A cell that is never part of "the selected data": a system column or the "type here to add a row" placeholder.
        // The loops that need to know which of the two it was (TryFillSingleValueIntoSelection, ResolvePasteAnchor) check
        // them separately.
        private bool IsExcludedFromDataSelection(DataGridViewCell cell)
        {
            return IsSystemColumn(cell.ColumnIndex) || IsPlaceholderRowIndex(cell.RowIndex);
        }

        /// <summary>Makes an editable grid in the library's look; <c>UIStyles.DataGridViews</c> makes the usual ones.</summary>
        public DataGridView()
        {
            // ReadOnlyDataGridView is display-only; this is the editable grid. AllowUserToDeleteRows is also the switch for
            // deleting rows (see ProcessDataGridViewKey), as on the standard grid.
            ReadOnly = false;
            AllowUserToDeleteRows = true;
            AllowUserToAddRows = true;

            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;
            AddEditingMenuItems();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
            }

            base.Dispose(disposing);
        }
    }
}
