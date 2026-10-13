using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The tooltip with the full text of a cell that is cut off.
    public partial class ListView : System.Windows.Forms.ListView
    {

        // The full text of a cell is shown on hover where the cell has to end it with an ellipsis.
        private void OnListViewMouseMoveForToolTip(object sender, MouseEventArgs e)
        {
            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item == null)
            {
                HideCellToolTip();
                return;
            }

            var row = hitTest.Item.Index;
            var displayColumn = -1;
            foreach (ColumnHeader candidate in Columns)
            {
                if (TryGetHeaderBounds(candidate.Index, out Rectangle bounds) &&
                    e.X >= bounds.Left && e.X < bounds.Right)
                {
                    displayColumn = candidate.DisplayIndex;
                    break;
                }
            }
            if (row == _toolTipRow && displayColumn == _toolTipDisplayColumn)
            {
                return;
            }

            _toolTipRow = row;
            _toolTipDisplayColumn = displayColumn;

            var orderedColumns = GetColumnsInDisplayOrder();
            if (displayColumn < 0 || displayColumn >= orderedColumns.Count)
            {
                _cellToolTip.Hide(this);
                return;
            }

            var column = orderedColumns[displayColumn];
            var item = hitTest.Item;
            var text = column.Index < item.SubItems.Count ? item.SubItems[column.Index].Text : string.Empty;

            if (string.IsNullOrEmpty(text) || !IsTextTruncated(text, item.Font ?? Font, column))
            {
                _cellToolTip.Hide(this);
                return;
            }

            _cellToolTip.Show(text, this, e.Location.X + Px(ToolTipOffsetX96), e.Location.Y + Px(ToolTipOffsetY96), 8000);
        }

        private void OnListViewMouseLeave(object sender, EventArgs e)
        {
            HideCellToolTip();
        }

        private void HideCellToolTip()
        {
            _toolTipRow = -1;
            _toolTipDisplayColumn = -1;
            _cellToolTip.Hide(this);
        }

        private bool IsTextTruncated(string text, Font font, ColumnHeader column)
        {
            var availableWidth = GetCellTextRectangle(new Rectangle(0, 0, column.Width, 1), column.Index).Width;
            if (availableWidth <= 0)
            {
                return true;
            }

            var measured = TextRenderer.MeasureText(
                text,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return measured.Width > availableWidth;
        }
    }
}
