using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A narrow strip with the version of the program (the entry assembly, like "v1.2.3"; <c>VersionText</c> changes it) at its right
        /// end and, while an update is known, a small green button next to it. It is the version strip of a settings window as an element of
        /// its own, for any window or page: put it in, and announce an update with <c>ShowUpdate(appUpdater, result)</c> or let
        /// <c>await CheckForUpdateAsync(appUpdater, timeout)</c> look for one. A click on the button shows the update prompt of the
        /// <c>AppUpdater</c> over the window the strip is in. Docks at the bottom (<c>Dock</c> can be changed); follows a change of the
        /// theme and of the accent.
        /// </summary>
        public static class VersionPanels
        {
            /// <summary>The strip in the darkest background of the theme (the same as the version strip of a settings window), or with a color set in its basic color.</summary>
            /// <param name="colors">A color set for the background: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; null (the default) is the darkest background.</param>
            public static VersionPanel CreateStandard(UIColorSet colors = null)
            {
                return UIVersionPanelFactory.CreateStandard(colors);
            }

            /// <summary>The strip in a very dark shade of the accent, the same as <see cref="Panels.CreatePrimary"/>.</summary>
            public static VersionPanel CreatePrimary()
            {
                return UIVersionPanelFactory.CreatePrimary();
            }
        }
    }
}
