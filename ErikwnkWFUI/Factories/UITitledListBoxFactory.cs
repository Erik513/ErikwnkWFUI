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
            TitledListBox control = new TitledListBox(
                displayMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);

            return Finish(control, colors == null ? (Func<UIColorSet>)null : () => colors);
        }

        // The Standard one with the accent: the selection and the drop line in the primary color set.
        public static TitledListBox CreatePrimary(
            string headerTitle = null,
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            TitledListBox control = new TitledListBox(
                displayMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);

            return Finish(control, () => UIColors.PrimarySet);
        }

        private static TitledListBox Finish(TitledListBox control, Func<UIColorSet> colors)
        {
            UIListBoxFactory.ApplyColors(control.InnerListBox, colors);

            return control;
        }
    }
}
