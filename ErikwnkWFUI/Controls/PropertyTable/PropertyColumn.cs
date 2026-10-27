using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Pairs a <see cref="Control"/> (or a null placeholder) with a
    /// <see cref="TableLayoutPanel"/> column width, for
    /// <see cref="PropertyTable"/>'s multi-column row overloads. Build
    /// one with <see cref="Auto"/>, <see cref="Absolute"/>, or
    /// <see cref="Percent"/> rather than constructing it directly.
    /// </summary>
    public class PropertyColumn
    {
        /// <summary>The editor in this column; null leaves the cell empty.</summary>
        public Control Control { get; private set; }

        /// <summary>How wide the column is (absolute pixels, or a share of what the absolute columns leave).</summary>
        public ColumnStyle Style { get; private set; }

        protected PropertyColumn(
            Control control,
            ColumnStyle style)
        {
            Control = control;
            Style = style;
        }

        /// <summary>A column that takes up all remaining width (equivalent to <see cref="Percent"/> with 100).</summary>
        public static PropertyColumn Auto(Control control)
        {
            return new PropertyColumn(
                control,
                new ColumnStyle(SizeType.Percent, 100));
        }

        /// <summary>A column with a fixed pixel width.</summary>
        public static PropertyColumn Absolute(
            Control control,
            int width)
        {
            return new PropertyColumn(
                control,
                new ColumnStyle(SizeType.Absolute, width));
        }

        /// <summary>A column that takes this percentage of the width the absolute columns of the row leave (the percentages of the row are shared in that proportion).</summary>
        public static PropertyColumn Percent(
            Control control,
            float percent)
        {
            return new PropertyColumn(
                control,
                new ColumnStyle(SizeType.Percent, percent));
        }
    }
}
