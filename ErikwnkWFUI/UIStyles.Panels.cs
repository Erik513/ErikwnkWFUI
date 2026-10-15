using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards. They are
        /// the standard panel in every other way (<c>AutoScroll</c>, <c>AutoSize</c>, <c>BackgroundImage</c>, <c>Padding</c> and so
        /// on): the scroll bars take the style of the theme, a color set by hand stays when the theme changes. They have no frame; set
        /// <c>BorderStyle = FixedSingle</c> (a thin frame in the color of the theme) and cast the result to <c>ErikwnkWFUI.Controls.ThemedPanel</c> for
        /// <c>BorderColor</c> (another color). Right-to-left layouts are not supported.
        /// </summary>
        public static class Panels
        {
            /// <summary>
            /// The usual panel: the medium background of the theme, the same as <see cref="CreateMedium"/>. With a color set the panel is in
            /// the basic color of the set (a panel has no states that a set could color further); set <see cref="Control.BackColor"/> for
            /// another color, it stays when the theme changes. <see cref="CreatePrimary"/> is not the accent set but a very dark
            /// shade of the accent.
            /// </summary>
            /// <param name="colors">A color set for the background: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; null (the default) is the medium background.</param>
            public static Panel CreateStandard(UIColorSet colors = null)
            {
                return UIPanelFactory.CreateStandard(colors);
            }

            /// <summary>
            /// A card: a panel in the elevated background with smoothly rounded corners and a thin line around it, for a group of
            /// controls on a page; it shows what is behind it in its corners. With a color set it is in the basic color of the set and
            /// its line has the hover color of the set. It does not dock. Cast the result to
            /// <c>ErikwnkWFUI.Controls.ThemedCardPanel</c> for <c>CornerRadius</c>, <c>BorderStyle</c> (None or FixedSingle) and <c>BorderColor</c>; the card keeps
            /// the controls inside away from the corners with a padding of its own.
            /// </summary>
            public static Panel CreateCardStandard(UIColorSet colors = null)
            {
                return UIPanelFactory.CreateCardStandard(colors);
            }

            /// <summary>The card (see <see cref="CreateCardStandard"/>) in the very dark shade of the accent of <see cref="CreatePrimary"/>, with a line one step lighter (<c>UIColors.PrimaryDark</c>). Follows a change of the theme and of the accent.</summary>
            public static Panel CreateCardPrimary()
            {
                return UIPanelFactory.CreateCardPrimary();
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

            /// <summary>A panel one step lighter than the medium one (<c>UIColors.BackgroundMediumElevated</c>, raised against a medium surface). Fills its parent; follows a change of the theme.</summary>
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
