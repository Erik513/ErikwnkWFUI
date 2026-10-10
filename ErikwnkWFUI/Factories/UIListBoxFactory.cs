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

        // The set's hover color is the color of the look (it is the accent itself for the primary set): the
        // selected item (moved in lightness where its text would not read on it, as the combo box and the menu
        // do) and the line that shows where a dragged item lands. The text on the selection is the set's own.
        internal static void ApplyColors(ListBox listBox, UIColorSet colors)
        {
            if (colors == null)
                return;

            listBox.SelectedBackColor = UIColors.GetReadableFill(colors.Hover, colors.HoverText);
            listBox.SelectedForeColor = colors.HoverText;
            listBox.DragIndicatorColor = colors.Hover;
        }
    }
}
