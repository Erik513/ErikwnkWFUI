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
        /// <summary>
        /// Themed <see cref="TableLayoutPanel"/>s, pre-sized to the given column/row count. Otherwise the standard control
        /// (<c>ColumnStyles</c>, <c>RowStyles</c>, <c>GrowStyle</c>, <c>AutoScroll</c> with scroll bars in the style of the theme ...);
        /// <c>ShowBorder</c> (a thin frame in the color of the theme), <c>BorderColor</c>, <c>CellBorderStyle</c>, <c>CellBorderColor</c> and
        /// <c>CellBorderLines</c> (all lines, only the ones around the table or only the ones between the cells) are on <c>ErikwnkWFUI.Controls.ThemedTableLayoutPanel</c>: cast the result. There is no frame and there
        /// are no lines by default. They do not dock (<c>Dock</c> is left to the caller). Right-to-left layouts are not supported.
        /// </summary>
        public static class TableLayoutPanels
        {
            /// <summary>A table layout panel in the elevated background of the theme (<c>UIColors.BackgroundMediumElevated</c>, the same as <see cref="Panels.CreateElevated"/>), without margin and padding. Follows a change of the theme.</summary>
            /// <param name="columnCount">The number of columns.</param>
            /// <param name="rowCount">The number of rows.</param>
            /// <param name="colors">A color set for the background (its basic color) instead of the elevated one: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>.</param>
            public static TableLayoutPanel CreateStandard(
                int columnCount,
                int rowCount,
                UIColorSet colors = null)
            {
                return UITableLayoutPanelFactory.CreateStandard(
                    columnCount,
                    rowCount,
                    colors);
            }

            /// <summary>A table layout panel without a background of its own (what lies behind it shows through), otherwise like <see cref="CreateStandard"/>.</summary>
            /// <param name="columnCount">The number of columns.</param>
            /// <param name="rowCount">The number of rows.</param>
            public static TableLayoutPanel CreateTransparent(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateTransparent(
                    columnCount,
                    rowCount);
            }

            /// <summary>A table layout panel in a very dark shade of the accent (<c>UIColors.PrimaryDarkDark</c>, the same as <see cref="Panels.CreatePrimary"/>), without margin and padding. Follows a change of the theme and of the accent.</summary>
            /// <param name="columnCount">The number of columns.</param>
            /// <param name="rowCount">The number of rows.</param>
            public static TableLayoutPanel CreatePrimary(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreatePrimary(
                    columnCount,
                    rowCount);
            }

            /// <summary>A table layout panel in the darkest background of the theme (<c>UIColors.BackgroundDark</c>, the same as <see cref="Panels.CreateDark"/>), without margin and padding. Follows a change of the theme.</summary>
            /// <param name="columnCount">The number of columns.</param>
            /// <param name="rowCount">The number of rows.</param>
            public static TableLayoutPanel CreateDark(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateDark(
                    columnCount,
                    rowCount);
            }
        }
    }
}
