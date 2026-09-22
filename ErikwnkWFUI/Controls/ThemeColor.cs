using System;
using System.Drawing;

namespace ErikwnkWFUI.Controls
{
    // Backs a public Color property that follows a theme default (usually
    // the current accent, UIColors.Primary, or a fixed neutral) live until
    // a consumer explicitly sets it - BorderColor, ColumnReorderIndicatorColor,
    // SelectionBackColor/SelectionOverlayColor on both ReadOnlyDataGridView
    // and ListView all used to hand-write the same override-or-fallback
    // pair of fields and ternary getter six times over. The default is a
    // delegate, not a plain Color, specifically so it can keep reading
    // UIColors.Primary/BorderMedium live (an app can change its accent at
    // runtime) rather than freezing whatever that resolved to at
    // construction time - the exact bug ListBox's own DragIndicatorColor
    // and this control's SelectionOverlayColor/SelectionBackColor were each
    // fixed for previously, before either used this.
    internal sealed class ThemeColor
    {
        private readonly Func<Color> _defaultColor;
        private Color _override;
        private bool _isOverridden;

        public ThemeColor(Func<Color> defaultColor)
        {
            _defaultColor = defaultColor;
        }

        public Color Value => _isOverridden ? _override : _defaultColor();

        public void Set(Color value)
        {
            _override = value;
            _isOverridden = true;
        }
    }
}
