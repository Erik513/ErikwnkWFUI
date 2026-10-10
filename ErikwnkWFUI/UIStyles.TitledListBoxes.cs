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
        /// <summary>A <see cref="Controls.ListBox"/> with a title above it - see <see cref="ListBoxes"/> for the list box on its own. Everything else is the same as the list box.</summary>
        public static class TitledListBoxes
        {
            /// <summary>The titled list box in neutral gray.</summary>
            /// <param name="headerTitle">The title above the list; the bar is left out while it is empty.</param>
            /// <param name="displayTextMember">Name of the property each item's text is read from; the item's own text (<c>ToString()</c>) when left out.</param>
            /// <param name="allowReorder">Whether the user can drag an item to another place; off by default.</param>
            /// <param name="showEnumeration">Whether each item starts with its number; off by default.</param>
            /// <param name="headerTextAlign">Where the title stands in its bar; left and centered vertically by default.</param>
            /// <param name="colors">A color set for the selected item and the drop line, as for <see cref="ListBoxes.CreateStandard"/>.</param>
            public static TitledListBox CreateStandard(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft,
                UIColorSet colors = null)
            {
                return UITitledListBoxFactory.CreateStandard(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selection and the drop line in the current accent instead of the neutral gray - mirrors <see cref="ListBoxes.CreatePrimary"/>.</summary>
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
