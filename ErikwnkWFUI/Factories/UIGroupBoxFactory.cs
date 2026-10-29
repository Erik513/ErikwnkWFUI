using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIGroupBoxFactory
    {
        public static GroupBox CreateStandard(
            string text = "",
            UIColorSet colors = null)
        {
            return Create(text, colors);
        }

        // The Standard one in the current accent - the primary color set colors the frame.
        public static GroupBox CreatePrimary(
            string text = "")
        {
            Controls.GroupBox groupBox = Create(text, null);
            groupBox.FollowColors(() => UIColors.PrimarySet);

            return groupBox;
        }

        private static Controls.GroupBox Create(string text, UIColorSet colors)
        {
            return new Controls.GroupBox
            {
                Text = text ?? "",
                Font = UIFonts.Normal,
                Colors = colors
            };
        }
    }
}
