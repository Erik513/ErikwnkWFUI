using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A <see cref="Controls.ListBox"/> wrapped with an optional header bar - see <see cref="ListBoxes"/> for the list box on its own.</summary>
        public static class TitledListBoxes
        {
            public static TitledListBox CreateStandard(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UITitledListBoxFactory.CreateStandard(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }

            /// <summary>Like <see cref="CreateStandard"/> but selection/drag-indicator-colored in the current accent instead of the fixed neutral gray - mirrors <see cref="ListBoxes.CreatePrimary"/>.</summary>
            public static TitledListBox CreatePrimary(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UITitledListBoxFactory.CreatePrimary(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }
        }
    }
}
