using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIInfoPopupFactory
    {
        public static InfoPopupForm CreateStandard(string title = "", UIColorSet colors = null)
        {
            return new InfoPopupForm(title) { Colors = colors };
        }

        // The Standard one in the current accent - the primary color set.
        public static InfoPopupForm CreatePrimary(string title = "")
        {
            return CreateStandard(title, UIColors.PrimarySet);
        }

        // A fixed-size popup for one short value, like the percentage over a slider.
        public static InfoPopupForm CreateStandardCompact(Size? size = null, UIColorSet colors = null)
        {
            InfoPopupForm popup = new InfoPopupForm { Colors = colors, Compact = true };

            if (size.HasValue)
                popup.CompactSize = size.Value;

            return popup;
        }

        public static InfoPopupForm CreatePrimaryCompact(Size? size = null)
        {
            return CreateStandardCompact(size, UIColors.PrimarySet);
        }
    }
}
