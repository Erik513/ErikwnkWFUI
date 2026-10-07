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
        /// <summary>A themed <see cref="ComboBox"/>: the closed box, the rows and the frame of the open list follow the theme (the scroll bars of the list are the system's dark ones in a dark theme).</summary>
        public static class ComboBoxes
        {
            /// <summary>A themed combo box; the selected (or hovered) row of the list is the neutral gray.</summary>
            /// <param name="comboBoxStyle">The standard WinForms style; a drop-down list the user only picks from by default.</param>
            /// <param name="colors">
            /// A color set for the selected (or hovered) row of the list instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>.
            /// </param>
            public static System.Windows.Forms.ComboBox CreateStandard(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList,
                UIColorSet colors = null)
            {
                return UIComboBoxFactory.CreateStandard(
                    comboBoxStyle,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selected row in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.ComboBox CreatePrimary(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
            {
                return UIComboBoxFactory.CreatePrimary(
                    comboBoxStyle);
            }

            /// <summary>
            /// Clears <paramref name="comboBox"/>'s items and repopulates it
            /// from <paramref name="items"/> - e.g. because a language switch
            /// changed the item text - restoring whatever was selected
            /// before by its POSITION in the list (not by value/identity, so
            /// this assumes the same N choices in the same order, just
            /// possibly relabeled). Saves callers from having to remember
            /// SelectedIndex and re-set it by hand every time they rebuild a
            /// combo box's items.
            /// </summary>
            public static void ReplaceItems(
                System.Windows.Forms.ComboBox comboBox,
                System.Collections.Generic.IEnumerable<object> items,
                bool keepSelection = true)
            {
                UIComboBoxFactory.ReplaceItems(
                    comboBox,
                    items,
                    keepSelection);
            }
        }
    }
}
