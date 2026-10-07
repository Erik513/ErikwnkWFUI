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
            public static TableLayoutPanel CreateStandard(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateStandard(
                    columnCount,
                    rowCount);
            }

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
