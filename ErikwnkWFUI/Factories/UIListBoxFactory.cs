using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIListBoxFactory
    {
        public static ListBox CreateStandard(
            string displayTextMember = null,
            bool allowReorder = true,
            bool showEnumeration = false)
        {
            return new ListBox
            {
                DisplayTextMember = displayTextMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration
            };
        }

        // Same control, selection/drag-indicator-colored in the current
        // accent instead of the fixed neutral gray CreateStandard keeps for
        // both - mirrors UIListViewFactory.CreatePrimary.
        public static ListBox CreatePrimary(
            string displayTextMember = null,
            bool allowReorder = true,
            bool showEnumeration = false)
        {
            return new ListBox
            {
                DisplayTextMember = displayTextMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration,
                SelectedBackColor = UIColors.Primary,
                DragIndicatorColor = UIColors.Primary
            };
        }
    }
}