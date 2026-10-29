using System;
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
            return Create(headerTitle, displayMember, allowReorder, showEnumeration, headerTextAlign, ColorSetSource.Of(colors));
        }

        // The Standard one with the accent: the selection and the drop line in the primary color set.
        public static TitledListBox CreatePrimary(
            string headerTitle = null,
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            return Create(headerTitle, displayMember, allowReorder, showEnumeration, headerTextAlign, () => UIColors.PrimarySet);
        }

        private static TitledListBox Create(
            string headerTitle,
            string displayMember,
            bool allowReorder,
            bool showEnumeration,
            ContentAlignment headerTextAlign,
            Func<UIColorSet> colors)
        {
            TitledListBox control = new TitledListBox(displayMember, allowReorder, showEnumeration, headerTitle, headerTextAlign);

            UIListBoxFactory.ApplyColors(control.InnerListBox, colors);
            control.ApplyBorderColor();

            return control;
        }
    }
}
