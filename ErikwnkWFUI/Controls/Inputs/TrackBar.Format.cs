using System;
using System.ComponentModel;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // How a value is written (the popup, a screen reader).
    public partial class TrackBar
    {
        private Func<double, string> _formatValue;

        /// <summary>
        /// How a value is written for the popup and for a screen reader. By default as a number with up to two decimals, the decimal
        /// sign by the language of the library (not the culture of the computer); <see cref="FormatPercent"/> writes a value from 0 to 1 as
        /// whole percent. Null gives the default back.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, string> FormatValue
        {
            get { return _formatValue ?? FormatNumber; }
            set
            {
                _formatValue = value;
                _valuePopup?.MarkWidthStale();
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", UIStrings.NumberCulture);
        }

        /// <summary>
        /// A value from 0 to 1 as whole percent, written the way of the language of the library ("10%" in English, "10 %" in German): for
        /// <see cref="FormatValue"/> of a volume or the like.
        /// </summary>
        public static string FormatPercent(double value)
        {
            return (Math.Round(value * 100) / 100.0).ToString("P0", UIStrings.NumberCulture);
        }
    }
}
