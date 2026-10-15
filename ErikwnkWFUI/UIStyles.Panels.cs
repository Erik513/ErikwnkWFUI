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
        /// Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards. They are
        /// the standard panel in every other way (<c>AutoScroll</c>, <c>AutoSize</c>, <c>BackgroundImage</c>, <c>Padding</c> and so
        /// on): the scroll bars take the style of the theme, a color set by hand stays when the theme changes. Left to the
        /// system: the frame of <c>BorderStyle.FixedSingle</c> and <c>Fixed3D</c> (drawn in the system's colors; the panels have
        /// none by default). Right-to-left layouts are not supported.
        /// </summary>
        public static class Panels
        {
            /// <summary>
            /// The usual panel: the medium background of the theme, the same as <see cref="CreateMedium"/>. There is no color set for a
            /// panel (it has no states that a set could color); set <see cref="Control.BackColor"/> for another color, it stays when
            /// the theme changes.
            /// </summary>
            public static Panel CreateStandard()
            {
                return UIPanelFactory.CreateStandard();
            }

            /// <summary>
            /// A card: a panel in the elevated background with smoothly rounded corners and a thin line around it, for a group of
            /// controls on a page; it shows what is behind it in its corners. It does not dock. Cast the result to
            /// <c>ErikwnkWFUI.Controls.ThemedCardPanel</c> for <c>CornerRadius</c>, <c>ShowBorder</c> and <c>BorderColor</c>; the card keeps
            /// the controls inside away from the corners with a padding of its own.
            /// </summary>
            public static Panel CreateCard()
            {
                return UIPanelFactory.CreateCard();
            }

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
