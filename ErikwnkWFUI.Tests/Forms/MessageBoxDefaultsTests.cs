using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The defaults the designer knows of the message window are the ones it has.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheMessageWindowHas()
    {
        using MessageBoxForm form = new MessageBoxForm("Text", "Title", ErikwnkWFUI.Forms.MessageBoxButtons.OK, ErikwnkWFUI.Forms.MessageBoxIcon.Info);

        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(form))
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(MessageBoxForm))
                Assert.True(Equals(attribute.Value, property.GetValue(form)), property.Name + " is not its default");
        }

        Assert.False(form.ShowHelpButton);
        Assert.False(form.DetailsExpanded);
    }
}
