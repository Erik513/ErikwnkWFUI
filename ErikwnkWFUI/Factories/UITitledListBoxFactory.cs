using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITitledListBoxFactory
    {
        public static TitledListBox CreateStandard(
            string headerTitle = null,
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft,
            UIColorSet colors = null)
        {
            TitledListBox control = new TitledListBox(
                displayMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);

            UIListBoxFactory.ApplyColors(control.InnerListBox, colors);

            return control;
        }

        // The Standard one with the accent: the selection and the drop line in the primary color set.
        public static TitledListBox CreatePrimary(
            string headerTitle = null,
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            return CreateStandard(headerTitle, displayMember, allowReorder, showEnumeration, headerTextAlign, UIColors.PrimarySet);
        }
    }
}
