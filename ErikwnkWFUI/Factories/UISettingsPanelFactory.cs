using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UISettingsPanelFactory
    {
        // The strip as it has always been in a settings window: the darkest background of the theme - or, with a color set, its basic color.
        public static SettingsPanel CreateStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return Create(() => UIColors.BackgroundBlack);

            Func<UIColorSet> source = ColorSetSource.Of(colors);

            return Create(() => source().Basic);
        }

        public static SettingsPanel CreatePrimary()
        {
            return Create(() => UIColors.PrimaryDarkDark);
        }

        private static SettingsPanel Create(Func<Color> backColor)
        {
            SettingsPanel panel = new SettingsPanel { Dock = DockStyle.Bottom };
            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}
