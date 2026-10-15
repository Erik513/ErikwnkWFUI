using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Panels with several pages, of which one is shown at a time, like the pages of a book. A header on top holds the number of
        /// the page (<c>ShowPageCount</c>, like "3/12", off by default), the title of the page (<c>TitleAlignment</c>: left, the default,
        /// center or right) and two small arrows to the previous and the next page. Make one, add the pages with <c>AddPage(title)</c>
        /// and put the controls of each page on the page that comes back; <c>SelectedIndex</c>, <c>Next()</c>, <c>Previous()</c> and
        /// <c>SelectedIndexChanged</c> switch and follow the pages. The flat ones and the cards are the same control; the cards are
        /// rounded with a thin line (see <see cref="Panels.CreateCardStandard"/>). Follow a change of the theme and of the accent.
        /// </summary>
        public static class MultiPanels
        {
            /// <summary>A flat multi panel in the medium background of the theme, or with a color set in its basic color. Fills its parent.</summary>
            /// <param name="colors">A color set for the background: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; null (the default) is the medium background.</param>
            public static MultiPanel CreateStandard(UIColorSet colors = null)
            {
                return UIMultiPanelFactory.CreateStandard(colors);
            }

            /// <summary>A flat multi panel in a very dark shade of the accent, the same as <see cref="Panels.CreatePrimary"/>. Fills its parent.</summary>
            public static MultiPanel CreatePrimary()
            {
                return UIMultiPanelFactory.CreatePrimary();
            }

            /// <summary>A multi panel as a card: the elevated background, smoothly rounded corners and a thin line; with a color set in its basic color and the hover color of the set as the line. Does not dock.</summary>
            /// <param name="colors">A color set for the background: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>.</param>
            public static MultiPanel CreateCardStandard(UIColorSet colors = null)
            {
                return UIMultiPanelFactory.CreateCardStandard(colors);
            }

            /// <summary>A multi panel as a card in the very dark shade of the accent, with a line one step lighter. Does not dock.</summary>
            public static MultiPanel CreateCardPrimary()
            {
                return UIMultiPanelFactory.CreateCardPrimary();
            }
        }
    }
}
