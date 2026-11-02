using System.ComponentModel;
using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>What the designer writes of a window of the library: only the frame color that was set, nothing of its parts, and the defaults.</summary>
[Collection(AccentColorTestCollection.Name)]
public class AppFormDesignerTests
{
    [Fact]
    public void TheDesigner_WritesTheFrameColorOnlyWhereItWasSet_AndDoesNotSeeThePartsOfTheWindow()
    {
        using AppForm form = new AppForm();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(form);
        PropertyDescriptor frame = properties["BorderColor"]!;

        Assert.False(frame.ShouldSerializeValue(form));
        frame.SetValue(form, Color.Magenta);
        Assert.True(frame.ShouldSerializeValue(form));
        frame.ResetValue(form);
        Assert.False(frame.ShouldSerializeValue(form));

        foreach (string name in new[] { "ContentPanel", "VersionStrip", "VersionLabel", "TitleBar", "FormTitle", "FormIcon" })
            Assert.False(properties[name]!.IsBrowsable, name);

        Assert.True(form.CloseEnabled);
        Assert.False(properties["CloseEnabled"]!.ShouldSerializeValue(form));
        Assert.False(properties["RememberBoundsKey"]!.ShouldSerializeValue(form));
    }
}
