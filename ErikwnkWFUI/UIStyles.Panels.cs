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
        /// <summary>Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards.</summary>
        public static class Panels
        {
            /// <summary>A panel in the darkest background of the theme (<c>UIColors.BackgroundDark</c>, the page behind the other surfaces). Fills its parent; follows a change of the theme.</summary>
            public static Panel CreateDark()
            {
                return UIPanelFactory.CreateDark();
            }

            /// <summary>A panel in the medium background of the theme (<c>UIColors.BackgroundMedium</c>, the usual surface of a window). Fills its parent; follows a change of the theme.</summary>
            public static Panel CreateMedium()
            {
                return UIPanelFactory.CreateMedium();
            }

            /// <summary>A panel one step lighter than the medium one (<c>UIColors.BackgroundMediumElevated</c>, a card on a medium surface). Fills its parent; follows a change of the theme.</summary>
            public static Panel CreateElevated()
            {
                return UIPanelFactory.CreateElevated();
            }

            /// <summary>A panel without a background of its own: what lies behind it shows through. Does not dock (<c>DockStyle.None</c>).</summary>
            public static Panel CreateTransparent()
            {
                return UIPanelFactory.CreateTransparent();
            }

            /// <summary>A panel in a very dark shade of the accent (<c>UIColors.PrimaryDarkDark</c>). Fills its parent; follows a change of the theme and of the accent.</summary>
            public static Panel CreatePrimary()
            {
                return UIPanelFactory.CreatePrimary();
            }
        }
    }
}
