using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIToastFactory
    {
        public static void ShowStandard(
            string message,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            ToastForm.ShowToast(message, owner, colors, transparency, duration, delay);
        }

        public static void ShowStandard(
            InfoPopupSection content,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            ToastForm.ShowToast(content, owner, colors, transparency, duration, delay);
        }

        public static void ShowPrimary(
            InfoPopupSection content,
            Form owner,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            ShowStandard(content, owner, UIColors.PrimarySet, transparency, duration, delay);
        }

        public static Toaster CreateStandard(
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            return new Toaster { Colors = colors, Transparency = transparency, Duration = duration, Delay = delay };
        }

        // The Standard one in the current accent - the primary color set.
        public static Toaster CreatePrimary(
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            return CreateStandard(UIColors.PrimarySet, transparency, duration, delay);
        }

        // The Standard one in the current accent - the primary color set.
        public static void ShowPrimary(
            string message,
            Form owner,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            ShowStandard(message, owner, UIColors.PrimarySet, transparency, duration, delay);
        }
    }
}
