using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIPanelFactory
    {
        // The normal surface: the medium one.
        public static Panel CreateStandard()
        {
            return CreateMedium();
        }

        public static Panel CreateDark()
        {
            return CreatePanel(
                () => UIColors.BackgroundDark,
                DockStyle.Fill);
        }

        public static Panel CreateMedium()
        {
            return CreatePanel(
                () => UIColors.BackgroundMedium,
                DockStyle.Fill);
        }

        public static Panel CreateElevated()
        {
            return CreatePanel(
                () => UIColors.BackgroundMediumElevated,
                DockStyle.Fill);
        }

        public static Panel CreateTransparent()
        {
            return CreatePanel(
                () => Color.Transparent,
                DockStyle.None);
        }

        public static Panel CreatePrimary()
        {
            return CreatePanel(
                () => UIColors.PrimaryDarkDark,
                DockStyle.Fill);
        }

        private static Panel CreatePanel(
            Func<Color> backColor,
            DockStyle dock)
        {
            Panel panel = new Panel
            {
                Dock = dock,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}