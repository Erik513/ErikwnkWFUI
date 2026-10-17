using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIVersionPanelFactory
    {
        // The strip as it has always been in a settings window: the darkest background of the theme - or, with a color set, its basic color.
        public static VersionPanel CreateStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return Create(() => UIColors.BackgroundBlack);

            Func<UIColorSet> source = ColorSetSource.Of(colors);

            return Create(() => source().Basic);
        }

        public static VersionPanel CreatePrimary()
        {
            return Create(() => UIColors.PrimaryDarkDark);
        }

        private static VersionPanel Create(Func<Color> backColor)
        {
            VersionPanel panel = new VersionPanel { Dock = DockStyle.Bottom };
            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}
