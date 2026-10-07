using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="System.Windows.Forms.ContextMenuStrip"/> themed to match the rest of
    /// ErikwnkWFUI instead of the plain system popup menu: the same clicks, keyboard and items
    /// as the standard control, only the look changes. Get one through
    /// <see cref="UIStyles.ContextMenuStrips"/>.
    /// </summary>
    /// <remarks>
    /// Themed: the menu items (text, icon, check mark, shortcut text, disabled look, submenu
    /// arrow), separators, the frame and every submenu, which gets the look when the menu
    /// opens (so items can still be added to a menu that is built already). The image and
    /// check margins follow the menu into its submenus.
    ///
    /// Not themed, and left as the standard control has them: controls hosted in the menu
    /// (<c>ToolStripComboBox</c>, <c>ToolStripTextBox</c>, <c>ToolStripProgressBar</c>) and a
    /// <c>ToolStripButton</c>; they keep the system's look. An application that sets
    /// <c>Renderer</c> or <c>RenderMode</c> itself gets its own look and the library's is gone.
    /// <c>RightToLeft</c> is not supported.
    /// </remarks>
    public class ContextMenuStrip : System.Windows.Forms.ContextMenuStrip
    {
        private readonly ThemeColor _selectionBackColor;
        private UIColorSet _colors;

        public ContextMenuStrip()
        {
            // The highlighted item: the set's hover color (brought to where its text reads), or
            // the neutral gray without a set.
            _selectionBackColor = new ThemeColor(() => _colors != null
                ? UIColors.GetReadableFill(_colors.Hover, _colors.HoverText)
                : UIColors.BorderLight);

            Renderer = CreateRenderer();
        }

        /// <summary>
        /// The colors of the menu: the hovered item takes the set's hover color and text, an item
        /// whose submenu is open the pressed ones. Left null, the neutral gray;
        /// <c>UIStyles.ContextMenuStrips.CreatePrimary</c> makes one in the accent color. A
        /// <see cref="SelectionBackColor"/> set explicitly wins over the set's hover color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set { _colors = value; }
        }

        /// <summary>
        /// Background of a hovered/selected menu item. Defaults to the hover color of
        /// <see cref="Colors"/>, or without a set to a fixed, neutral gray
        /// (<see cref="UIColors.BorderLight"/>); set this explicitly to use any color. The
        /// menus of the grids, the list view and the tab control set it to follow their own
        /// highlight color.
        /// </summary>
        public Color SelectionBackColor
        {
            get => _selectionBackColor.Value;
            set => _selectionBackColor.Set(value);
        }

        // The frame of the menu: the set's strong color (brought to 6:1 against the menu), or
        // the neutral gray without a set or with an explicit selection color.
        internal Color FrameColor
        {
            get
            {
                if (_colors == null || _selectionBackColor.IsSet)
                    return UIColors.BorderLight;

                return UIColors.EnsureContrast(_colors.Pressed, UIColors.BackgroundMediumElevated, 6.0);
            }
        }

        // A submenu is a drop-down of its own that does not take over the image and check margins
        // of this menu. They are set on the whole tree at every opening, not once when an item
        // is added, because items can still come until the menu shows. After the Opening event,
        // not before: that is where applications fill a menu on demand.
        protected override void OnOpening(CancelEventArgs e)
        {
            base.OnOpening(e);

            if (!e.Cancel)
                ApplyThemeToSubmenus(Items);
        }

        private void ApplyThemeToSubmenus(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                // Every menu item, not just those with children now: an application can fill a
                // submenu when it is about to drop down (DropDownOpening), after this ran.
                if (item is ToolStripMenuItem menuItem
                    && menuItem.DropDown is ToolStripDropDownMenu dropDown)
                {
                    dropDown.Renderer = CreateRenderer();
                    dropDown.ShowImageMargin = ShowImageMargin;
                    dropDown.ShowCheckMargin = ShowCheckMargin;

                    ApplyThemeToSubmenus(menuItem.DropDownItems);
                }
            }
        }

        // The renderer reads this menu's colors live; submenus get one too, so they do not
        // depend on how the framework passes the owner's renderer down.
        private ToolStripRenderer CreateRenderer()
        {
            return new ThemedRenderer(this);
        }

        private sealed class ThemedRenderer : ToolStripProfessionalRenderer
        {
            private readonly ContextMenuStrip _menu;

            public ThemedRenderer(ContextMenuStrip menu) : base(new ThemedColorTable(menu))
            {
                _menu = menu;
                RoundedEdges = false;
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = GetForeColor(e.Item);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = GetForeColor(e.Item);
                base.OnRenderArrow(e);
            }

            private Color GetForeColor(ToolStripItem item)
            {
                if (!item.Enabled)
                    return UIColors.TextDisabled;

                bool highlighted = item.Selected || item.Pressed;

                if (!highlighted)
                    return UIColors.TextPrimary;

                // The set's own text, on its own fill; an explicit selection color gets the best
                // text for that color.
                if (_menu._colors != null && !_menu._selectionBackColor.IsSet)
                    return item.Pressed ? _menu._colors.PressedText : _menu._colors.HoverText;

                return UIColors.GetContrastingForeColor(_menu.SelectionBackColor);
            }
        }

        // Every color is read from UIColors and the menu on each access, not kept from the
        // construction, so a long-lived menu follows a later change of theme or colors.
        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            private readonly ContextMenuStrip _menu;

            public ThemedColorTable(ContextMenuStrip menu)
            {
                _menu = menu;
            }

            private Color Selection => _menu.SelectionBackColor;

            public override Color ToolStripDropDownBackground => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientBegin => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientMiddle => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientEnd => UIColors.BackgroundMediumElevated;

            // The frame and the border of a check mark's box: the set's strong color, or without
            // a set BorderLight (not BorderMedium: more contrast against the menu in both
            // themes, 1.95:1 against 1.57:1 in Dark and 1.37:1 against 1.24:1 in Light).
            public override Color MenuBorder => _menu.FrameColor;
            public override Color ButtonSelectedBorder => _menu.FrameColor;

            public override Color SeparatorDark => UIColors.BorderLight;
            public override Color SeparatorLight => UIColors.BorderLight;

            // Shifted away from the current fill (darker on a light one, lighter on a dark one),
            // not a fixed color: BorderLight is also the default fill, and a border in the
            // fill's own color is invisible.
            public override Color MenuItemBorder
            {
                get
                {
                    Color fill = Selection;
                    bool fillIsLight = UIColors.IsLight(fill);
                    return fillIsLight ? UIColors.Darken(fill, 40) : UIColors.Lighten(fill, 40);
                }
            }

            public override Color MenuItemSelectedGradientBegin => Selection;
            public override Color MenuItemSelectedGradientEnd => Selection;

            public override Color MenuItemPressedGradientBegin => PressedFill;
            public override Color MenuItemPressedGradientMiddle => PressedFill;
            public override Color MenuItemPressedGradientEnd => PressedFill;

            public override Color CheckBackground => UIColors.BackgroundMediumElevated;
            public override Color CheckSelectedBackground => Selection;
            public override Color CheckPressedBackground => PressedFill;

            // One value backing all four "pressed" roles above: the set's pressed color (readable
            // for its text), else the selection fill darkened.
            private Color PressedFill
            {
                get
                {
                    if (_menu._colors != null && !_menu._selectionBackColor.IsSet)
                        return UIColors.GetReadableFill(_menu._colors.Pressed, _menu._colors.PressedText);

                    return UIColors.Darken(Selection, 30);
                }
            }
        }
    }
}
