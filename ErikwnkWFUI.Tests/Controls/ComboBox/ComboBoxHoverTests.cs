using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>Whether the box looks hovered: it follows the mouse, also across being disabled and enabled again.</summary>
public class ComboBoxHoverTests
{
    private static bool IsHot(Control box) => box.GetPrivateField<bool>("_isHot");

    [Fact]
    public void AHoveredBox_ThatIsDisabledAndEnabledAgain_DoesNotStillLookHovered()
    {
        using System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard();
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
        using System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard();

        box.Enabled = false;
        box.Enabled = true;

        Assert.False(IsHot(box));
    }

    [Fact]
    public void AHoveredBox_ThatIsHiddenAndShownAgain_DoesNotStillLookHovered()
    {
        using System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard();
        using Form host = OffscreenForm.Show(box, new System.Drawing.Size(200, 60), System.Drawing.Color.Black, new System.Drawing.Point(5, 5));
        box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.True(IsHot(box));

        box.Visible = false;
        Assert.False(IsHot(box));

        // (The mouse is not over the box - the window is off the screen.)
        box.Visible = true;
        Assert.False(IsHot(box));
    }

    [Fact]
    public void TheColorSet_IsCode_AndTheDefaultsOfTheDesignerAreTheOnesTheBoxHas()
    {
        using System.Windows.Forms.ComboBox box = new ErikwnkWFUI.Controls.ComboBox();
        System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(box);

        Assert.False(properties["Colors"]!.IsBrowsable);
        Assert.Equal(System.ComponentModel.DesignerSerializationVisibility.Hidden, properties["Colors"]!.Attributes.OfType<System.ComponentModel.DesignerSerializationVisibilityAttribute>().Single().Visibility);

        foreach (System.ComponentModel.PropertyDescriptor property in properties)
        {
            // (DrawMode and FlatStyle are set by the library, owner-drawn and flat.)
            if (property.Name != "DrawMode" && property.Name != "FlatStyle" && property.Attributes[typeof(System.ComponentModel.DefaultValueAttribute)] is System.ComponentModel.DefaultValueAttribute attribute)
                Assert.True(Equals(attribute.Value, property.GetValue(box)), property.Name + " is not its default");
        }
    }
}
