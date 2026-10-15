using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIMultiPanelFactory
    {
        // The arrow buttons are in the style of the panel: neutral on the standard surfaces, in the accent on the primary ones, in the
        // color set on a set. The flat ones have no line around them, but the same line colors as the cards (for the line under the
        // header).

        // The flat panel in the usual surface - or, with a color set, in its basic color.
        public static MultiPanel CreateStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return Create(false, DockStyle.Fill, () => UIColors.BackgroundMedium, null, null);

            Func<UIColorSet> source = ColorSetSource.Of(colors);

            return Create(false, DockStyle.Fill, () => source().Basic, () => source().Hover, source);
        }

        // The flat panel in the very dark accent of Panels.CreatePrimary.
        public static MultiPanel CreatePrimary()
        {
            return Create(false, DockStyle.Fill, () => UIColors.PrimaryDarkDark, () => UIColors.PrimaryDark, () => UIColors.PrimarySet);
        }

        // The card: the elevated surface, rounded, with a thin line - or, with a color set, in its basic color and the hover color as the line.
        public static MultiPanel CreateCardStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return Create(true, DockStyle.None, () => UIColors.BackgroundMediumElevated, null, null);

            Func<UIColorSet> source = ColorSetSource.Of(colors);

            return Create(true, DockStyle.None, () => source().Basic, () => source().Hover, source);
        }

        public static MultiPanel CreateCardPrimary()
        {
            return Create(true, DockStyle.None, () => UIColors.PrimaryDarkDark, () => UIColors.PrimaryDark, () => UIColors.PrimarySet);
        }

        private static MultiPanel Create(bool card, DockStyle dock, Func<Color> backColor, Func<Color> borderColor, Func<UIColorSet> arrowColors)
        {
            MultiPanel panel = new MultiPanel(arrowColors)
            {
                Dock = dock,
                Margin = new Padding(0)
            };

            // The flat variant is the card without the rounding and the line.
            if (!card)
            {
                panel.CornerRadius = 0;
                panel.ShowBorder = false;
            }

            ThemeWatcher.FollowBackColor(panel, backColor);

            if (borderColor != null)
                panel.FollowBorderColor(borderColor);

            return panel;
        }
    }
}
