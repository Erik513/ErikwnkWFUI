using System;
using System.Globalization;

// The namespace is ErikwnkWFUI, not ErikwnkWFUI.Localization like the rest of this folder, on
// purpose: programs that use AppCurrencyFormatter (DealOrNoDeal) refer to it by that name.
namespace ErikwnkWFUI
{
    /// <summary>The currencies <see cref="AppCurrencyFormatter"/> writes amounts in.</summary>
    public enum AppCurrency
    {
        /// <summary>Euro: "1.234,50€" (German number format, the symbol after the amount).</summary>
        Euro,

        /// <summary>US dollar: "$1,234.50" (US number format, the symbol before the amount).</summary>
        Dollar
    }

    /// <summary>
    /// Formats amounts for a chosen currency - deliberately independent of
    /// AppLanguage/AppLocalization. Someone reading German text doesn't
    /// necessarily want US number formatting just because they picked
    /// Dollar, and vice versa, so this only controls the currency symbol
    /// and its own number format, not the rest of the UI.
    /// </summary>
    public static class AppCurrencyFormatter
    {
        // Matches AppLocalization's own default language (English).
        private static AppCurrency _currency = AppCurrency.Dollar;

        /// <summary>The currency amounts are written in; the dollar until it is set. Setting another one raises <see cref="CurrencyChanged"/>.</summary>
        public static AppCurrency Currency
        {
            get { return _currency; }
            set
            {
                if (_currency == value)
                    return;

                _currency = value;
                CurrencyChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raised when Currency changes. Like AppLocalization.LanguageChanged,
        /// this only signals that already-displayed amounts should be
        /// re-formatted and re-applied - it doesn't do that automatically.
        /// </summary>
        public static event EventHandler CurrencyChanged;

        /// <summary>
        /// Formats an amount using the currently selected currency's own
        /// symbol, placement and number format (e.g. "1.234,50€" for Euro,
        /// "$1,234.50" for Dollar) - regardless of the app's UI language. A negative
        /// amount has its sign in front of the whole: "-$1,234.50", "-1.234,50€".
        /// </summary>
        public static string Format(decimal amount, string decimalFormat = "#,0.00")
        {
            switch (Currency)
            {
                case AppCurrency.Dollar:
                    CultureInfo american = CultureInfo.GetCultureInfo("en-US");
                    string text = amount.ToString(decimalFormat, american);

                    // (The sign goes before the symbol: "-$5.00", not "$-5.00".)
                    return text.StartsWith(american.NumberFormat.NegativeSign, StringComparison.Ordinal)
                        ? american.NumberFormat.NegativeSign + "$" + text.Substring(american.NumberFormat.NegativeSign.Length)
                        : "$" + text;

                case AppCurrency.Euro:
                default:
                    return amount.ToString(decimalFormat, CultureInfo.GetCultureInfo("de-DE")) + "€";
            }
        }

        /// <summary>
        /// Just the symbol for the currently selected currency, for cases
        /// that build their own formatted string (e.g. appending it to an
        /// already-formatted number).
        /// </summary>
        public static string Symbol
        {
            get { return Currency == AppCurrency.Dollar ? "$" : "€"; }
        }
    }
}
