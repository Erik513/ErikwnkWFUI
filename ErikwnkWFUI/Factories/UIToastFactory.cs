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
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            ToastForm.ShowToast(message, owner, colors, transparency, duration, delay, icon, symbol, position);
        }

        public static void ShowStandard(
            InfoPopupSection content,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            ToastForm.ShowToast(content, owner, colors, transparency, duration, delay, icon, symbol, position);
        }

        public static void ShowPrimary(
            InfoPopupSection content,
            Form owner,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            ShowStandard(content, owner, UIColors.PrimarySet, transparency, duration, delay, icon, symbol, position);
        }

        public static Toaster CreateStandard(
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            return new Toaster { Colors = colors, Transparency = transparency, Duration = duration, Delay = delay, Icon = icon, CustomSymbol = symbol, Position = position };
        }

        // The Standard one in the current accent - the primary color set.
        public static Toaster CreatePrimary(
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            return CreateStandard(UIColors.PrimarySet, transparency, duration, delay, icon, symbol, position);
        }

        // The Standard one in the current accent - the primary color set.
        public static void ShowPrimary(
            string message,
            Form owner,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            ShowStandard(message, owner, UIColors.PrimarySet, transparency, duration, delay, icon, symbol, position);
        }
    }
}
