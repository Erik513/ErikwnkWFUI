using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The defaults the designer knows of the input window are the ones it has, and its parts and results are not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class InputBoxDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheInputWindowHas_AndItsPartsAreHidden()
    {
        using InputBoxForm form = new InputBoxForm(null!, "");
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(form);

        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(InputBoxForm))
                Assert.True(Equals(attribute.Value, property.GetValue(form)), property.Name + " is not its default");
        }

        foreach (string name in new[] { "Value", "InputTextBox", "Validator" })
            Assert.False(properties[name]!.IsBrowsable, name);
    }
}
