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

                    // RedLight, not UIColors.Red - see OnCellFormatting's
                    // own remarks on why (this is just the column's initial
                    // default; OnCellFormatting recomputes the same color
                    // per cell on every paint anyway, but an accessibility
                    // tool or anything else reading DefaultCellStyle
                    // directly, before that first format pass, should still
                    // see the correct color, not the old one).
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
            // this column, and pressing it needs the same fix. e.CellStyle.BackColor
            // is already whatever this row's own resting color is (normal or
            // alternating - see the OnCellMouseDown comment above for why
            // CurrentCell can't just be kept off this cell instead), so this
            // doesn't hardcode a color of its own - it only carries that same
            // value over to SelectionBackColor, so a press here never shows
            // the grid's real (green) selection color.
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;

            if (IsPlaceholderRowIndex(e.RowIndex))
            {
                return;
            }

            e.Value = UIStyles.Symbols.Close;
            e.FormattingApplied = true;

            // AlternatingRowsDefaultCellStyle outranks Column.DefaultCellStyle
            // for odd-indexed rows, so every other row's "X" was coming out
            // in the normal row text color instead of red. Setting it here,
            // on the cell style actually used for this paint pass, outranks
            // both.
            //
            // RedLight, not the plain UIColors.Red every other red surface
            // in this library uses - confirmed via ColorContrastHelperTests'
            // sibling DataGridViewColorContrastTests that plain Red on this
            // control's own Dark-theme row background only reaches 2.46:1,
            // below WCAG's 3:1 floor for a UI glyph like this one. Scoped to
            // just this glyph rather than changing UIColors.Red itself,
            // which other surfaces use against their own, different
            // backgrounds.
            Color foreColor = e.RowIndex == _hoveredDeleteRowIndex
                ? UIColors.Lighten(UIColors.RedLight, 20)
                : UIColors.RedLight;
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
    }
}
