using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIPanelFactory
    {
        // The normal surface: the medium one - or, with a color set, the basic color of the set.
        public static Panel CreateStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return CreateMedium();

            return CreatePanel(() => colors.Basic, DockStyle.Fill);
        }

        // A card: the elevated surface, rounded, with a thin line; it does not dock (it is placed where it is wanted).
        // With a color set the card is in its basic color and has the hover color of the set as its line.
        public static Panel CreateCardStandard(UIColorSet colors = null)
        {
            if (colors == null)
                return CreateCard(() => UIColors.BackgroundMediumElevated, null);

            Func<UIColorSet> source = ColorSetSource.Of(colors);

            return CreateCard(() => source().Basic, () => source().Hover);
        }

        // The card in the very dark accent of CreatePrimary, with a line one step lighter.
        public static Panel CreateCardPrimary()
        {
            return CreateCard(() => UIColors.PrimaryDarkDark, () => UIColors.PrimaryDark);
        }

        private static Panel CreateCard(Func<Color> backColor, Func<Color> borderColor)
        {
            ThemedCardPanel card = new ThemedCardPanel
            {
                Dock = DockStyle.None,
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(card, backColor);

            if (borderColor != null)
                card.FollowBorderColor(borderColor);

            return card;
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
            Panel panel = new ThemedPanel
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