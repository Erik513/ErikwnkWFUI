using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITitledListBoxFactory
    {
        public static TitledListBox CreateStandard(
            string headerTitle = null,
            string displayTextMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            return new TitledListBox(
                displayTextMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);
        }

        // Same control, selection/drag-indicator-colored in the current
        // accent instead of the fixed neutral gray CreateStandard keeps for
        // both - mirrors UIListBoxFactory.CreatePrimary.
        public static TitledListBox CreatePrimary(
            string headerTitle = null,
            string displayTextMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            TitledListBox control = new TitledListBox(
                displayTextMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);

            control.SelectedBackColor = UIColors.Primary;
            control.DragIndicatorColor = UIColors.Primary;

            return control;
        }
    }
}