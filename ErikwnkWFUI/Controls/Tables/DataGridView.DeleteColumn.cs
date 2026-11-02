using System.ComponentModel;
using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The column with the X that deletes a row: adding it, formatting its cells, the hover and the click.
    public partial class DataGridView : ReadOnlyDataGridView
    {
        private DataGridViewColumn _deleteRowColumn;
        private int _hoveredDeleteRowIndex = -1;

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
        [DefaultValue(false)]
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
                    // A text column, not a button column: a button cell draws the system's button chrome (a light edge
                    // stayed visible around the glyph), so the "X" could not be made fully red. A text cell has no chrome.
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

                    // RedLight, not Red (see OnCellFormatting). Set here as well, so that anything reading DefaultCellStyle
                    // before the first format pass sees the right color.
                    column.DefaultCellStyle.ForeColor = UIColors.RedLight;
                    column.DefaultCellStyle.SelectionForeColor = UIColors.RedLight;
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

                    // The grid resizes columns itself and keeps track of it separately (see AllowColumnResizing): this
                    // column is excluded there too, or its fixed width would be dragged away.
                    SetColumnResizable(column.Index, false);
                }
                else
                {
                    Columns.Remove(_deleteRowColumn);
                    _deleteRowColumn = null;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);

            if (e.Column == _deleteRowColumn)
            {
                // SortMode and width are set on the column (see ShowDeleteRowColumn); only the position is handled here.
                // This column never reaches the placeholder-row logic that the base OnColumnAdded prepares.
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

        // The delete-row column is a plain read-only text column showing a
        // static "X" glyph (see FormatDeleteRowCellText) rather than a
        // DataGridViewButtonColumn, so a genuine cell click is what
        // triggers deletion here, not CellContentClick (that event is
        // specific to button/checkbox column content).
        /// <inheritdoc/>
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

            // The bound item is kept, not the row index: the delete is deferred, and a second click before it runs would
            // find another row at that index.
            object item = Rows[e.RowIndex].DataBoundItem;

            // Deferred: the click handling of this cell is still on the call stack, and the reset of the bindings rebuilds
            // all rows, this one included. BeginInvoke deletes after it has unwound.
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() => DeleteItem(item)));
            }
        }

        /// <inheritdoc/>
        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);

            if (_deleteRowColumn == null || e.ColumnIndex != _deleteRowColumn.Index || e.RowIndex < 0)
            {
                return;
            }

            // For every cell of the column, the placeholder included (it shows no "X" but can be pressed): the selection
            // color is the row's own color, so a press never shows the grid's selection color.
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;

            if (IsPlaceholderRowIndex(e.RowIndex))
            {
                return;
            }

            e.Value = UIStyles.Symbols.Close;
            e.FormattingApplied = true;

            // AlternatingRowsDefaultCellStyle outranks the column's style for odd rows (their "X" came out in the text
            // color), so the color is set on the style used for this paint.
            //
            // RedLight, not Red: Red on the dark rows reaches only 2.46:1, below the 3:1 a glyph needs
            // (DataGridViewColorContrastTests). Scoped to this glyph, because other surfaces use Red on other backgrounds.
            Color foreColor = e.RowIndex == _hoveredDeleteRowIndex
                ? UIColors.Lighten(UIColors.RedLight, 20)
                : UIColors.RedLight;
            e.CellStyle.ForeColor = foreColor;
            e.CellStyle.SelectionForeColor = foreColor;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        protected override void OnApplyingBatchedDataSourceChange()
        {
            base.OnApplyingBatchedDataSourceChange();
            _hoveredDeleteRowIndex = -1;
        }
    }
}
