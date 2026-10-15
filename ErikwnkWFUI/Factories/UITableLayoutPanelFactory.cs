using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITableLayoutPanelFactory
    {
        // The elevated surface - or, with a color set, the basic color of the set.
        public static TableLayoutPanel CreateStandard(
            int columnCount,
            int rowCount,
            UIColorSet colors = null)
        {
            return CreateTableLayoutPanel(
                columnCount,
                rowCount,
                colors == null ? (Func<Color>)(() => UIColors.BackgroundMediumElevated) : () => colors.Basic);
        }

        public static TableLayoutPanel CreateDark(
            int columnCount,
            int rowCount)
        {
            return CreateTableLayoutPanel(
                columnCount,
                rowCount,
                () => UIColors.BackgroundDark);
        }

        private static TableLayoutPanel CreateTableLayoutPanel(
            int columnCount,
            int rowCount,
            Func<Color> backColor)
        {
            TableLayoutPanel panel = new ThemedTableLayoutPanel
            {
                ColumnCount = Math.Max(0, columnCount),
                RowCount = Math.Max(0, rowCount),
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}