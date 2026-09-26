using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIContextMenuStripFactory
    {
        public static ContextMenuStrip CreateStandard()
        {
            return new ContextMenuStrip();
        }

        // Same control, hover/selection-highlighted in the current accent
        // instead of the fixed neutral gray CreateStandard keeps - mirrors
        // UIListBoxFactory.CreatePrimary.
        public static ContextMenuStrip CreatePrimary()
        {
            return new ContextMenuStrip
            {
                SelectionBackColor = UIColors.Primary
            };
        }
    }
}
