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
        /// <summary>A themed, owner-drawn, optionally drag-reorderable <see cref="Controls.ListBox"/> - a heavier alternative to the native ListBox with icons, custom item colors, and enumeration support.</summary>
        public static class ListBoxes
        {
            public static Controls.ListBox CreateStandard(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreateStandard(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }

            /// <summary>Like <see cref="CreateStandard"/> but selection/drag-indicator-colored in the current accent instead of the fixed neutral gray - mirrors <see cref="ListViews.CreatePrimary"/>.</summary>
            public static Controls.ListBox CreatePrimary(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreatePrimary(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }
        }
    }
}
