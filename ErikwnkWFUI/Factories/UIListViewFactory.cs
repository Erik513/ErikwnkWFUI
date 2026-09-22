using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    // Returns the System.Windows.Forms.ListView base type on purpose, not
    // Controls.ListView - callers never need to spell out the derived class
    // name (which would otherwise clash with "using System.Windows.Forms;"
    // in most consuming files).
    internal static class UIListViewFactory
    {
        public static ListView CreateStandard()
        {
            return new Controls.ListView();
        }

        // Same control, framed/selection-highlighted/reorder-line-colored
        // in the current accent instead of the fixed neutral gray
        // CreateStandard keeps for all three.
        public static ListView CreatePrimary()
        {
            return new Controls.ListView
            {
                BorderColor = UIColors.Primary,
                SelectionOverlayColor = UIColors.Selection,
                ColumnReorderIndicatorColor = UIColors.Primary
            };
        }
    }
}
