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
                listView.FollowColors(() => colors);

            return listView;
        }

        // Same control, framed/selection-highlighted/reorder-line-colored
        // in the current accent instead of the neutral gray CreateStandard
        // keeps - including this ListView's own built-in right-click menu.
        public static ListView CreatePrimary()
        {
            Controls.ListView listView = new Controls.ListView();
            listView.FollowColors(() => UIColors.PrimarySet);

            return listView;
        }
    }
}
