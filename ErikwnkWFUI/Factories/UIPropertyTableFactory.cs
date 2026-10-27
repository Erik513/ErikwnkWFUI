using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIPropertyTableFactory
    {
        public static PropertyTable CreateStandard(UIColorSet colors = null)
        {
            PropertyTable table = new PropertyTable();
            table.Colors = colors;

            return table;
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static PropertyTable CreatePrimary()
        {
            PropertyTable table = new PropertyTable();
            table.FollowColors(() => UIColors.PrimarySet);

            return table;
        }
    }
}
