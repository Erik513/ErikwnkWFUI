using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>What the tests of the check box and the radio button ask of them: whether the box looks hovered or pressed.</summary>
internal static class BoxProbe
{
    private static BoxState StateOf(Control box)
    {
        return box switch
        {
            global::ErikwnkWFUI.Controls.CheckBox checkBox => checkBox.State,
            global::ErikwnkWFUI.Controls.RadioButton radioButton => radioButton.State,
            _ => throw new ArgumentException("not a box of the library: " + box.GetType().Name, nameof(box))
        };
    }

    public static bool IsHot(this Control box) => StateOf(box).IsHot;

    public static bool IsPressed(this Control box) => StateOf(box).IsPressed;
}
