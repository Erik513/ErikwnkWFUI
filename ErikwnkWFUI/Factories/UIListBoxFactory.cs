using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIListBoxFactory
    {
        public static ListBox CreateStandard(
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            UIColorSet colors = null)
        {
            ListBox listBox = new ListBox
            {
                DisplayMember = displayMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration
            };

            ApplyColors(listBox, colors);

            return listBox;
        }

        // The Standard one with the accent: the selection and the drop line in the primary color set.
        public static ListBox CreatePrimary(
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false)
        {
            return CreateStandard(displayMember, allowReorder, showEnumeration, UIColors.PrimarySet);
        }

        // The set's hover color is the color of the look (it is the accent itself for the primary set): the tint of the
        // selected item (a see-through one over the row, like the list view's, so the text of the item stays as it is)
        // and the line that shows where a dragged item lands.
        internal static void ApplyColors(ListBox listBox, UIColorSet colors)
        {
            if (colors == null)
                return;

            listBox.SelectedBackColor = colors.Hover;
            listBox.DragIndicatorColor = colors.Hover;
        }
    }
}
