using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIInfoPopupFactory
    {
        public static InfoPopupForm CreateStandard(UIColorSet colors = null, Size? fixedSize = null)
        {
            InfoPopupForm popup = new InfoPopupForm { Colors = colors };

            if (fixedSize.HasValue)
                popup.FixedSize = fixedSize.Value;

            return popup;
        }

        // The Standard one in the current accent - the primary color set.
        public static InfoPopupForm CreatePrimary(Size? fixedSize = null)
        {
            return CreateStandard(UIColors.PrimarySet, fixedSize);
        }
    }
}
