using System.ComponentModel;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The defaults the designer knows of the options window are the ones it has, and its choices and parts are not written.</summary>
[Collection(AccentColorTestCollection.Name)]
public class OptionsFormDefaultsTests
{
    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheOptionsWindowHas_AndTheChoicesAreHidden()
    {
        using OptionsForm form = new OptionsForm();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(form);

        foreach (PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && property.ComponentType == typeof(OptionsForm))
                Assert.True(Equals(attribute.Value, property.GetValue(form)), property.Name + " is not its default");
        }

        foreach (string name in new[] { "PropertyTable", "SelectedTheme", "SelectedAccentColor", "SelectedLanguage", "SelectedStartWithWindows", "SelectedAlwaysOnTop" })
            Assert.False(properties[name]!.IsBrowsable, name);
    }
}
