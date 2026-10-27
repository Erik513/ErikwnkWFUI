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
        public Control Control { get; private set; }

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

        /// <summary>A column sized as a percentage of the row's total width.</summary>
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
