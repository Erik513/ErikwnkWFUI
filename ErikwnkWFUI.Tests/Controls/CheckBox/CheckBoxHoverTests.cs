using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>Whether the box looks hovered: it follows the mouse, also across being disabled and enabled again.</summary>
public class CheckBoxHoverTests
{
    private static bool IsHot(Control box) => box.GetPrivateField<bool>("_isHot");

    [Fact]
    public void AHoveredBox_ThatIsDisabledAndEnabledAgain_DoesNotStillLookHovered()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Label", false);
        _ = box.Handle;
        box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.True(IsHot(box));

        // The mouse leaves while the box is disabled: no MouseLeave reaches it.
        box.Enabled = false;
        Assert.False(IsHot(box));

        box.Enabled = true;

        Assert.False(IsHot(box));
    }

    [Fact]
    public void ABoxWithoutAWindow_CanBeDisabledAndEnabled()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Label", false);

        box.Enabled = false;
        box.Enabled = true;

        Assert.False(IsHot(box));
    }
}
