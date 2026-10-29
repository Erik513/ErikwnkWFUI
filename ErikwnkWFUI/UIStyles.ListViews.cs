using System.Drawing;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A dark-themed, multi-column <see cref="Controls.ListView"/> (Details view) with spreadsheet-style cell-range selection, hand-rolled column reordering/resizing, and Ctrl+C/Ctrl+Shift+C copy - see the class itself for the full behavior.</summary>
        public static class ListViews
        {
            /// <summary>The themed list view in neutral gray.</summary>
            /// <param name="colors">
            /// A color set to frame and highlight the list in instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>. The border, the selection, the column drag line and the
            /// context menu's selection then use its color.
            /// </param>
            public static System.Windows.Forms.ListView CreateStandard(UIColorSet colors = null)
            {
                return UIListViewFactory.CreateStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/>, but the border, the selection, the column drag line and the selection of the context menu follow the accent.</summary>
            public static System.Windows.Forms.ListView CreatePrimary()
            {
                return UIListViewFactory.CreatePrimary();
            }
        }
    }
}
