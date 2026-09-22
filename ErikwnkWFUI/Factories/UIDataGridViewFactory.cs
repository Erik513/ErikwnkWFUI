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
        public static DataGridView CreateStandard(object dataSource = null)
        {
            return new Controls.DataGridView
            {
                DataSource = dataSource
            };
        }

        public static DataGridView CreateReadOnlyStandard(object dataSource = null)
        {
            return new Controls.ReadOnlyDataGridView
            {
                DataSource = dataSource
            };
        }

        // Same editable control CreateStandard returns, framed (and
        // gridlined - see ReadOnlyDataGridView.BorderColor), selection-
        // highlighted, and reorder-line-colored in the current accent
        // instead of the fixed neutral gray CreateStandard keeps for all
        // three - mirrors UIListViewFactory.CreatePrimary.
        public static DataGridView CreatePrimary(object dataSource = null)
        {
            return new Controls.DataGridView
            {
                DataSource = dataSource,
                BorderColor = UIColors.Primary,
                SelectionBackColor = UIColors.Primary,
                ColumnReorderIndicatorColor = UIColors.Primary
            };
        }

        // Same read-only control CreateReadOnlyStandard returns, framed/
        // highlighted in the current accent instead - the read-only
        // counterpart to CreatePrimary, same as CreateReadOnlyStandard is
        // to CreateStandard.
        public static DataGridView CreateReadOnlyPrimary(object dataSource = null)
        {
            return new Controls.ReadOnlyDataGridView
            {
                DataSource = dataSource,
                BorderColor = UIColors.Primary,
                SelectionBackColor = UIColors.Primary,
                ColumnReorderIndicatorColor = UIColors.Primary
            };
        }
    }
}
