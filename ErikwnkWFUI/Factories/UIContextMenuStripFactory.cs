using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIContextMenuStripFactory
    {
        public static ContextMenuStrip CreateStandard(UIColorSet colors = null)
        {
            return new ContextMenuStrip { Colors = colors };
        }

        // The Standard one with the accent: the highlight in the primary color set.
        public static ContextMenuStrip CreatePrimary()
        {
            ContextMenuStrip menu = CreateStandard();
            menu.FollowColors(() => UIColors.PrimarySet);

            return menu;
        }
    }
}
