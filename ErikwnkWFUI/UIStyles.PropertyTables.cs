using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A key/value <see cref="Controls.PropertyTable"/> (label + editor per row, grouped into sections) - typical use is a settings/options panel.</summary>
        public static class PropertyTables
        {
            /// <summary>A neutral table, or one with a line in the color of a color set at the left edge of every section header.</summary>
            /// <param name="colors">A color set for the lines at the section headers: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; its <c>Hover</c> color is the main color. Null (the default) is no line. The labels, the cells and the controls in them stay as they are.</param>
            public static PropertyTable CreateStandard(UIColorSet colors = null)
            {
                return UIPropertyTableFactory.CreateStandard(colors);
            }

            /// <summary>Same, but the lines at the section headers have the theme accent (and follow a change of it).</summary>
            public static PropertyTable CreatePrimary()
            {
                return UIPropertyTableFactory.CreatePrimary();
            }
        }
    }
}
