using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control: every color can be set and read back, and which color a tab gets.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlColorTests : TabControlTestBase
{
    // ---- every color can be set and read back ----

    [Theory]
    [MemberData(nameof(ColorProperties))]
    public void EveryColorProperty_KeepsTheValueItWasGiven_ForThatInstanceOnly(string name)
    {
        using WfuiTabControl tabs = CreateTabs();
        using WfuiTabControl untouched = CreateTabs();
        System.Reflection.PropertyInfo property = typeof(WfuiTabControl).GetProperty(name)!;
        Color chosen = Color.FromArgb(201, 77, 13);
        Color before = (Color)property.GetValue(untouched)!;

        property.SetValue(tabs, chosen);

        Assert.Equal(chosen, (Color)property.GetValue(tabs)!);
        Assert.Equal(before, (Color)property.GetValue(untouched)!);
    }

    // ---- which color a tab gets ----

    [Fact]
    public void TabBackColor_SelectedWinsOverHoveredWinsOverNormal()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabBackColor = Color.FromArgb(1, 1, 1);
        tabs.HoverTabBackColor = Color.FromArgb(2, 2, 2);
        tabs.SelectedTabBackColor = Color.FromArgb(3, 3, 3);

        Assert.Equal(Color.FromArgb(3, 3, 3), tabs.InvokePrivate<Color>("GetTabBackColor", true, true));
        Assert.Equal(Color.FromArgb(3, 3, 3), tabs.InvokePrivate<Color>("GetTabBackColor", true, false));
        Assert.Equal(Color.FromArgb(2, 2, 2), tabs.InvokePrivate<Color>("GetTabBackColor", false, true));
        Assert.Equal(Color.FromArgb(1, 1, 1), tabs.InvokePrivate<Color>("GetTabBackColor", false, false));
    }

    [Fact]
    public void TabForeColor_DisabledWinsOverSelectedWinsOverNormal()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabForeColor = Color.FromArgb(1, 1, 1);
        tabs.SelectedTabForeColor = Color.FromArgb(2, 2, 2);
        tabs.DisabledTabForeColor = Color.FromArgb(3, 3, 3);

        Assert.Equal(Color.FromArgb(3, 3, 3), tabs.InvokePrivate<Color>("GetTabForeColor", true, false));
        Assert.Equal(Color.FromArgb(3, 3, 3), tabs.InvokePrivate<Color>("GetTabForeColor", false, false));
        Assert.Equal(Color.FromArgb(2, 2, 2), tabs.InvokePrivate<Color>("GetTabForeColor", true, true));
        Assert.Equal(Color.FromArgb(1, 1, 1), tabs.InvokePrivate<Color>("GetTabForeColor", false, true));
    }

    [Fact]
    public void ADisabledPage_GetsTheDisabledTextColor_OnItsTab()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabBackColor = Color.Black;
        tabs.TabForeColor = Color.White;
        tabs.DisabledTabForeColor = Color.FromArgb(255, 0, 0);
        tabs.TabPages[2].Enabled = false;
        Rectangle plain = tabs.GetTabRect(1);
        Rectangle disabled = tabs.GetTabRect(2);

        using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));

        // Text pixels are either white (normal) or reddish (disabled) -
        // the two tabs must not mix.
        Assert.True(CountColored(bitmap, plain, reddish: false) > 10);
        Assert.Equal(0, CountColored(bitmap, plain, reddish: true));
        Assert.True(CountColored(bitmap, disabled, reddish: true) > 10);
    }
}
