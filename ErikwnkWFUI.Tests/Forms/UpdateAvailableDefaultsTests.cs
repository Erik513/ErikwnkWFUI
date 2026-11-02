using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The defaults the designer knows of the update window are the ones it has, and its results are not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UpdateAvailableDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheUpdateWindowHas_AndTheResultsAreHidden()
    {
        using UpdateAvailableForm form = new UpdateAvailableForm("1", "2");
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(form);

        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(UpdateAvailableForm))
                Assert.True(Equals(attribute.Value, property.GetValue(form)), property.Name + " is not its default");
        }

        foreach (string name in new[] { "UpdateChosen", "SkipChosen" })
            Assert.False(properties[name]!.IsBrowsable, name);
    }
}
