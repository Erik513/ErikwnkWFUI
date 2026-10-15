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
        /// <summary>Themed <see cref="TableLayoutPanel"/>s, pre-sized to the given column/row count.</summary>
        public static class TableLayoutPanels
        {
            /// <summary>A table layout panel in the elevated background of the theme (<c>UIColors.BackgroundMediumElevated</c>, the same as <see cref="Panels.CreateElevated"/>), without margin and padding. Follows a change of the theme.</summary>
            /// <param name="columnCount">The number of columns.</param>
            /// <param name="rowCount">The number of rows.</param>
            public static TableLayoutPanel CreateStandard(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateStandard(
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
