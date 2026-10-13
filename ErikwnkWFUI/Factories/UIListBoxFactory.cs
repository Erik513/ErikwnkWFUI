using System;
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
            return Create(displayMember, allowReorder, showEnumeration, ColorSetSource.Of(colors));
        }

        // The Standard one with the accent: the selection and the drop line in the primary color set - which is read
        // again whenever it is needed, so the list follows a change of the accent.
        public static ListBox CreatePrimary(
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false)
        {
            return Create(displayMember, allowReorder, showEnumeration, () => UIColors.PrimarySet);
        }

        private static ListBox Create(string displayMember, bool allowReorder, bool showEnumeration, Func<UIColorSet> colors)
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

        // The set's hover color is the color of the look (it is the accent itself for the primary set): the tint of the
        // selected item (a see-through one over the row, like the list view's, so the text of the item stays as it is)
        // and the line that shows where a dragged item lands.
        internal static void ApplyColors(ListBox listBox, Func<UIColorSet> colors)
        {
            if (colors == null)
                return;

            listBox.FollowColors(colors);
        }
    }
}
