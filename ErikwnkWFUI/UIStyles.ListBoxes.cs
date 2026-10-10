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
        /// <summary>A themed, owner-drawn, optionally drag-reorderable <see cref="Controls.ListBox"/> - a heavier alternative to the native ListBox with icons, custom item colors, and enumeration support. See <see cref="TitledListBoxes"/> for one with a title above it.</summary>
        public static class ListBoxes
        {
            /// <summary>The list box in neutral gray.</summary>
            /// <param name="displayMember">Name of the property each item's text is read from (<c>DisplayMember</c>); the item's own text (<c>ToString()</c>) when left out.</param>
            /// <param name="allowReorder">Whether the user can drag an item to another place (a handle appears on the right of each item); off by default.</param>
            /// <param name="showEnumeration">Whether each item starts with its number ("1.", "2." ...); off by default.</param>
            /// <param name="colors">
            /// A color set for the selected item and the line that shows where a dragged item lands, instead of the
            /// neutral gray: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>. The text on the selection is chosen to read on it.
            /// </param>
            public static Controls.ListBox CreateStandard(
                string displayMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                UIColorSet colors = null)
            {
                return UIListBoxFactory.CreateStandard(
                    displayMember,
                    allowReorder,
                    showEnumeration,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selection and the drop line in the current accent instead of the neutral gray - mirrors <see cref="ListViews.CreatePrimary"/>.</summary>
            public static Controls.ListBox CreatePrimary(
                string displayMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreatePrimary(
                    displayMember,
                    allowReorder,
                    showEnumeration);
            }
        }
    }
}
