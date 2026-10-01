using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    // Returns the System.Windows.Forms.DataGridView base type on purpose,
    // not Controls.DataGridView - callers never need to spell out the
    // derived class name (which would otherwise clash with this same
    // "using System.Windows.Forms;" in most consuming files).
    internal static class UIDataGridViewFactory
    {
        public static DataGridView CreateStandard(object dataSource = null, UIColorSet colors = null)
        {
            Controls.DataGridView grid = new Controls.DataGridView
            {
                DataSource = dataSource
            };

            ApplyColors(grid, colors);

            return grid;
        }

        public static DataGridView CreateReadOnlyStandard(object dataSource = null, UIColorSet colors = null)
        {
            Controls.ReadOnlyDataGridView grid = new Controls.ReadOnlyDataGridView
            {
                DataSource = dataSource
            };

            ApplyColors(grid, colors);

            return grid;
        }

        // The editable grid framed (and gridlined - see
        // ReadOnlyDataGridView.BorderColor), selection-highlighted and
        // reorder-line-colored in the current accent instead of the neutral
        // gray - including its own right-click menu. Mirrors
        // UIListViewFactory.CreatePrimary.
        public static DataGridView CreatePrimary(object dataSource = null)
        {
            return CreateStandard(dataSource, UIColors.PrimarySet);
        }

        // The read-only counterpart to CreatePrimary.
        public static DataGridView CreateReadOnlyPrimary(object dataSource = null)
        {
            return CreateReadOnlyStandard(dataSource, UIColors.PrimarySet);
        }

        // The set's hover color is the color of the look (it is the accent
        // itself for the primary set).
        private static void ApplyColors(Controls.ReadOnlyDataGridView grid, UIColorSet colors)
        {
            if (colors == null)
                return;

            grid.BorderColor = colors.Hover;
            grid.SelectionBackColor = colors.Hover;
            grid.ColumnReorderIndicatorColor = colors.Hover;
            grid.ContextMenuSelectionColor = colors.Hover;
        }
    }
}
