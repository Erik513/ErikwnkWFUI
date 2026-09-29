using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITabControlFactory
    {
        public static TabControl CreateStandard()
        {
            return new TabControl();
        }

        // Same control, with the selected tab's bar in the current accent
        // instead of the neutral gray CreateStandard keeps - mirrors
        // UIListBoxFactory.CreatePrimary.
        public static TabControl CreatePrimary()
        {
            return new TabControl
            {
                AccentColor = UIColors.Primary
            };
        }
    }
}
