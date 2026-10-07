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
        /// <summary>A themed <see cref="Controls.TabControl"/> - behaves like the standard one, drawn in the library's own look.</summary>
        public static class TabControls
        {
            /// <summary>The editable variant - right-click a tab to add, rename or close tabs. Use <see cref="CreateReadOnlyStandard"/> instead for tabs the user cannot edit.</summary>
            /// <param name="colors">
            /// A color set for the bar under the selected tab and the selection of the
            /// right-click menu instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>,
            /// <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>.
            /// </param>
            public static Controls.TabControl CreateStandard(UIColorSet colors = null)
            {
                return UITabControlFactory.CreateStandard(colors);
            }

            /// <summary>The display-only variant - tabs can be switched, not added, renamed or closed. Use <see cref="CreateStandard"/> instead for editable tabs.</summary>
            /// <param name="colors">A color set for the bar under the selected tab instead of the neutral gray; see <see cref="CreateStandard"/>.</param>
            public static Controls.ReadOnlyTabControl CreateReadOnlyStandard(UIColorSet colors = null)
            {
                return UITabControlFactory.CreateReadOnlyStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the selected tab's bar and the menu selection in the current accent instead of neutral gray - mirrors <see cref="DataGridViews.CreatePrimary"/>.</summary>
            public static Controls.TabControl CreatePrimary()
            {
                return UITabControlFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreateReadOnlyStandard"/> but with the selected tab's bar in the current accent - the read-only counterpart to <see cref="CreatePrimary"/>.</summary>
            public static Controls.ReadOnlyTabControl CreateReadOnlyPrimary()
            {
                return UITabControlFactory.CreateReadOnlyPrimary();
            }
        }
    }
}
