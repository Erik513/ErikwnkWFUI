using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The defaults the designer knows of the popups are the ones they have, and the color set of the info popup is not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesThePopupsHave_AndTheColorSetIsHidden()
    {
        using InfoPopupForm popup = new InfoPopupForm();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(popup);

        foreach (PropertyDescriptor property in properties)
        {
            // (Opacity follows Transparency and is hidden from the designer.)
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute
                && property.Name != "Opacity" && typeof(PopupFormBase).IsAssignableFrom(property.ComponentType))
                Assert.True(Equals(attribute.Value, property.GetValue(popup)), property.Name + " is not its default");
        }

        Assert.False(properties["Colors"]!.IsBrowsable);
    }
}
