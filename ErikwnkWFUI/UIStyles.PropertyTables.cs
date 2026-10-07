using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A themed key/value <see cref="Controls.PropertyTable"/> (label + editor per row, grouped into sections) - typical use is a settings/options panel.</summary>
        public static class PropertyTables
        {
            public static PropertyTable CreateStandard()
            {
                return UIPropertyTableFactory.CreateStandard();
            }
        }
    }
}
