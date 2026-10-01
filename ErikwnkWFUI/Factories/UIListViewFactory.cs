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
        public static ListView CreateStandard(UIColorSet colors = null)
        {
            Controls.ListView listView = new Controls.ListView();

            if (colors != null)
            {
                // The set's hover color is the color of the look (it is the
                // accent itself for the primary set).
                listView.BorderColor = colors.Hover;
                listView.SelectionOverlayColor = UIColors.CreateSelectionOverlay(colors.Hover);
                listView.ColumnReorderIndicatorColor = colors.Hover;
                listView.ContextMenuSelectionColor = colors.Hover;
            }

            return listView;
        }

        // Same control, framed/selection-highlighted/reorder-line-colored
        // in the current accent instead of the neutral gray CreateStandard
        // keeps - including this ListView's own built-in right-click menu.
        public static ListView CreatePrimary()
        {
            return CreateStandard(UIColors.PrimarySet);
        }
    }
}
