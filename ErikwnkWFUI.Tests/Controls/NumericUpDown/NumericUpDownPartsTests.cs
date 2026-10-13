using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>
/// The control finds the two children of the standard control (the arrow buttons, which are not public, and the text
/// field) by their type and position. If a version of the framework changes that, the arrows would silently keep the
/// system's look: this test says it at once.
/// </summary>
public class NumericUpDownPartsTests
{
    [Fact]
    public void TheControl_FindsTheArrowButtonsAndTheTextField_OfTheStandardControl()
    {
        using WfuiNumericUpDown box = new WfuiNumericUpDown();

        Control? buttons = box.GetPrivateField<Control>("_buttons");
        Control? edit = box.GetPrivateField<Control>("_edit");

        Assert.NotNull(buttons);
        Assert.NotNull(edit);
        Assert.NotSame(buttons, edit);
        Assert.Equal("UpDownButtons", buttons!.GetType().Name);
        Assert.Contains(buttons, box.Controls.Cast<Control>());
        Assert.Contains(edit, box.Controls.Cast<Control>());
    }

    [Fact]
    public void TheArrowFields_TakeOverTheWindowOfTheButtons_AndLetGoOfItWithIt()
    {
        using WfuiNumericUpDown box = new WfuiNumericUpDown();
        Control buttons = box.GetPrivateField<Control>("_buttons")!;
        NativeWindow fields = box.GetPrivateField<NativeWindow>("_arrowFields")!;

        _ = box.Handle;
        Assert.Equal(buttons.Handle, fields.Handle);

        box.Dispose();
        Assert.Equal(IntPtr.Zero, fields.Handle);
    }
}
