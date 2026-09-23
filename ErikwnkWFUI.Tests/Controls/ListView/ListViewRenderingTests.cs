using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// GetEffectiveColor - the disabled-state dimming every paint call (header
/// text, sort glyph, row text, selection overlay) routes through. Invoked
/// directly via reflection, the same reasoning as ListBoxRenderingTests:
/// it's a pure function of Enabled plus the color passed in, so calling it
/// directly exercises the exact same logic the real paint would.
/// </summary>
public class ListViewRenderingTests
{
    [Fact]
    public void GetEffectiveColor_ReturnsTheColorUnchanged_WhenEnabled()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100));

        Color result = listView.InvokePrivate<Color>("GetEffectiveColor", Color.Red);

        Assert.Equal(Color.Red, result);
    }

    [Fact]
    public void GetEffectiveColor_ReturnsDisabledGray_WhenDisabled()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100));
        listView.Enabled = false;

        Color result = listView.InvokePrivate<Color>("GetEffectiveColor", Color.Red);

        Assert.Equal(UIColors.DisabledGray, result);
    }
}
