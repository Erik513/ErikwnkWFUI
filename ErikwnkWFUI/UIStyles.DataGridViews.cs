using System.Drawing;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A themed <see cref="System.Windows.Forms.DataGridView"/> for data bound via <see cref="System.Windows.Forms.DataGridView.DataSource"/> - the control to reach for when rows come from a bound source rather than being added by hand.</summary>
        public static class DataGridViews
        {
            /// <summary>The editable variant - row adding/deleting, an optional delete-row column, clipboard cut/copy/paste, and a right-click context menu. Use <see cref="CreateReadOnlyStandard"/> instead for a grid that only ever shows data.</summary>
            /// <param name="dataSource">The data to bind, or none.</param>
            /// <param name="colors">
            /// A color set to frame and highlight the grid in instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>. The border, the selection, the column drag line and the
            /// context menu's selection then use its color.
            /// </param>
            public static System.Windows.Forms.DataGridView CreateStandard(object dataSource = null, UIColorSet colors = null)
            {
                return UIDataGridViewFactory.CreateStandard(dataSource, colors);
            }

            /// <summary>The display-only variant - column-header sorting, but no editing. Use <see cref="CreateStandard"/> instead for a grid that needs to be editable.</summary>
            /// <param name="dataSource">The data to bind, or none.</param>
            /// <param name="colors">A color set for the border, selection and menu instead of the neutral gray; see <see cref="CreateStandard"/>.</param>
            public static System.Windows.Forms.DataGridView CreateReadOnlyStandard(object dataSource = null, UIColorSet colors = null)
            {
                return UIDataGridViewFactory.CreateReadOnlyStandard(dataSource, colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but framed in the current accent color instead of the fixed neutral border - mirrors <see cref="ListViews.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.DataGridView CreatePrimary(object dataSource = null)
            {
                return UIDataGridViewFactory.CreatePrimary(dataSource);
            }

            /// <summary>Like <see cref="CreateReadOnlyStandard"/> but framed in the current accent color instead of the fixed neutral border - the read-only counterpart to <see cref="CreatePrimary"/>.</summary>
            public static System.Windows.Forms.DataGridView CreateReadOnlyPrimary(object dataSource = null)
            {
                return UIDataGridViewFactory.CreateReadOnlyPrimary(dataSource);
            }
        }
    }
}
