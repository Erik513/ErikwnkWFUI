using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// Every color a control needs to show one colored look in all of its
    /// states: the fill at rest, hovered, pressed and disabled, and the text
    /// color that reads best on each of them. Get one from the ready-made
    /// <c>UIStyles.Colors.Sets</c> (Orange, Teal, Purple ...) or build one
    /// from any color with <c>UIStyles.Colors.CreateSet</c>; a control that
    /// accepts a set then needs nothing else.
    /// </summary>
    /// <remarks>
    /// A set does not follow the theme or the accent: it looks the same in
    /// Dark and Light. Only <c>UIStyles.Colors.Sets.Primary</c> follows the
    /// accent, because it is made from it.
    /// </remarks>
    public sealed class UIColorSet
    {
        /// <summary>The fill at rest.</summary>
        public Color Basic { get; }

        /// <summary>The fill while the mouse is over the control.</summary>
        public Color Hover { get; }

        /// <summary>The fill while the control is pressed.</summary>
        public Color Pressed { get; }

        /// <summary>The fill of a disabled control.</summary>
        public Color Disabled { get; }

        /// <summary>The text color on <see cref="Basic"/>.</summary>
        public Color BasicText { get; }

        /// <summary>The text color on <see cref="Hover"/>.</summary>
        public Color HoverText { get; }

        /// <summary>The text color on <see cref="Pressed"/>.</summary>
        public Color PressedText { get; }

        /// <summary>The (muted) text color on <see cref="Disabled"/>.</summary>
        public Color DisabledText { get; }

        /// <summary>
        /// Lets any <see cref="Color"/> be passed where a set is expected; the set
        /// is made with <c>UIStyles.Colors.CreateSet(color)</c>.
        /// </summary>
        public static implicit operator UIColorSet(Color color)
        {
            return UIColors.CreateSet(color);
        }

        internal UIColorSet(
            Color basic,
            Color hover,
            Color pressed,
            Color disabled,
            Color basicText,
            Color hoverText,
            Color pressedText,
            Color disabledText)
        {
            Basic = basic;
            Hover = hover;
            Pressed = pressed;
            Disabled = disabled;
            BasicText = basicText;
            HoverText = hoverText;
            PressedText = pressedText;
            DisabledText = disabledText;
        }
    }
}
