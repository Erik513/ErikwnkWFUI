using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The defaults the designer knows of a toast are the ones it has, and its color set is not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastFormDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesAToastHas_AndTheColorSetIsHidden()
    {
        using ErikwnkWFUI.Forms.ToastForm toast = new ErikwnkWFUI.Forms.ToastForm();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(toast);

        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(ErikwnkWFUI.Forms.ToastForm))
                Assert.True(Equals(attribute.Value, property.GetValue(toast)), property.Name + " is not its default");
        }

        Assert.False(properties["Colors"]!.IsBrowsable);
    }
}
