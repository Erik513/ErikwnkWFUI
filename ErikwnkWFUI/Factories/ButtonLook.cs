using System.Drawing;

namespace ErikwnkWFUI.Factories
{
    // Everything that makes up a button's look, so each variant is one
    // value instead of seven loose arguments.
    internal readonly struct ButtonLook
    {
        public readonly Color BackColor;
        public readonly Color ForeColor;
        public readonly Color BorderColor;
        public readonly int BorderSize;
        public readonly Color MouseOverBackColor;
        public readonly Color MouseDownBackColor;

        // Set when the look comes from a UIColorSet, which already knows
        // the text on each state and the disabled look. Left empty, the
        // button works them out as it always did.
        public readonly Color? HoverForeColor;
        public readonly Color? PressedForeColor;
        public readonly Color? DisabledBackColor;
        public readonly Color? DisabledForeColor;

        public ButtonLook(
            Color backColor,
            Color foreColor,
            Color borderColor,
            int borderSize,
            Color mouseOverBackColor,
            Color mouseDownBackColor,
            Color? hoverForeColor = null,
            Color? pressedForeColor = null,
            Color? disabledBackColor = null,
            Color? disabledForeColor = null)
        {
            BackColor = backColor;
            ForeColor = foreColor;
            BorderColor = borderColor;
            BorderSize = borderSize;
            MouseOverBackColor = mouseOverBackColor;
            MouseDownBackColor = mouseDownBackColor;
            HoverForeColor = hoverForeColor;
            PressedForeColor = pressedForeColor;
            DisabledBackColor = disabledBackColor;
            DisabledForeColor = disabledForeColor;
        }
    }
}
