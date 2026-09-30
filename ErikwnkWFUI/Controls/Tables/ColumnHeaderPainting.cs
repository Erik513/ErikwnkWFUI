using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // Shared column-header drawing for ReadOnlyDataGridView and ListView -
    // both draw their sort glyph and column-reorder insertion line exactly
    // the same way, just from different owner-draw event types
    // (DataGridViewCellPaintingEventArgs vs DrawListViewColumnHeaderEventArgs)
    // with no common base to hang a shared instance method off. Each caller
    // pulls out the one Graphics/Rectangle pair its own event type actually
    // has (e.CellBounds vs e.Bounds) and hands those in directly instead.
    internal static class ColumnHeaderPainting
    {
        // A plain Unicode triangle character, drawn in the header's own
        // text color, right-aligned in the sorted column's header cell -
        // not a hand-built polygon: the font's own hinting/anti-aliasing
        // draws it correctly and symmetrically in both directions for
        // free, where an earlier, hand-filled polygon version of this
        // needed its own anti-aliasing just to stop the two directions
        // rasterizing at visibly different sizes. A no-op for
        // SortOrder.None, so a caller can call this unconditionally once
        // it's already confirmed this is the sorted column, regardless of
        // which direction (or neither) that turns out to be.
        public static void DrawSortGlyph(Graphics graphics, Rectangle bounds, Font font, Color foreColor, SortOrder sortOrder)
        {
            if (sortOrder == SortOrder.None)
            {
                return;
            }

            // Ascending points down, descending points up - the opposite
            // of what might seem obvious, but matches what was actually
            // asked for on both controls.
            string glyph = sortOrder == SortOrder.Ascending ? "▼" : "▲";

            const int rightMargin = 4;

            using (Brush brush = new SolidBrush(foreColor))
            {
                SizeF glyphSize = graphics.MeasureString(glyph, font);
                float x = bounds.Right - rightMargin - glyphSize.Width;
                float y = bounds.Top + (bounds.Height - glyphSize.Height) / 2f;
                graphics.DrawString(glyph, font, brush, x, y);
            }
        }

        // Exactly one header cell's left edge lines up with
        // dragInsertBeforeDisplayIndex (or, for "insert after the last
        // column", the last cell's right edge), so at most one of the two
        // branches below ever draws anything for a given call - callers
        // paint every header cell during a drag, so this runs once per
        // cell and is a no-op for all but (at most) one of them. A filled
        // rectangle rather than a DrawLine pen - a pen is centered on its
        // coordinate, so a line drawn exactly at the first column's left
        // edge (x=0, or the cell's own Left) would have half its width
        // clipped off, making that one position look thinner than every
        // other. A no-op when nothing is being dragged
        // (dragInsertBeforeDisplayIndex &lt; 0), so a caller can call this
        // unconditionally once it already knows a drag might be in
        // progress, regardless of whether this specific call turns out to
        // need it.
        public static void DrawColumnDragInsertionLine(
            Graphics graphics,
            Rectangle bounds,
            int columnDisplayIndex,
            int columnCount,
            int dragInsertBeforeDisplayIndex,
            Color indicatorColor)
        {
            if (dragInsertBeforeDisplayIndex < 0)
            {
                return;
            }

            const int lineWidth = 2;

            using (SolidBrush brush = new SolidBrush(indicatorColor))
            {
                if (columnDisplayIndex == dragInsertBeforeDisplayIndex)
                {
                    graphics.FillRectangle(brush, bounds.Left, bounds.Top, lineWidth, bounds.Height);
                }
                else if (dragInsertBeforeDisplayIndex == columnCount && columnDisplayIndex == columnCount - 1)
                {
                    graphics.FillRectangle(brush, bounds.Right - lineWidth, bounds.Top, lineWidth, bounds.Height);
                }
            }
        }
    }
}
