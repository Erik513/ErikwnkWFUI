using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The defaults the designer knows of the progress window are the ones it has, and its state and results are not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressFormDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheProgressWindowHas_AndTheStateIsHidden()
    {
        using ProgressForm form = new ProgressForm(null!);
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(form);

        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(ProgressForm))
                Assert.True(Equals(attribute.Value, property.GetValue(form)), property.Name + " is not its default");
        }

        foreach (string name in new[] { "BarColors", "IsCancelled", "CancellationToken", "State", "Status" })
            Assert.False(properties[name]!.IsBrowsable, name);
    }
}
