using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>Where the two scroll bars of a scrolling panel meet, the system leaves a light square: it is found, and only when it exists on a dark theme.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelScrollBarCornerTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    public static IEnumerable<object[]> Panels()
    {
        yield return new object[] { "Panel", (Func<ScrollableControl>)(() => new ThemedPanel()) };
        yield return new object[] { "Table", (Func<ScrollableControl>)(() => new ThemedTableLayoutPanel()) };
        yield return new object[] { "Flow", (Func<ScrollableControl>)(() => new ThemedFlowLayoutPanel { AutoSize = false, WrapContents = false }) };
    }

    private ScrollableControl Show(Func<ScrollableControl> make, Size content)
    {
        ScrollableControl panel = make();
        panel.AutoScroll = true;
        panel.Size = new Size(150, 100);

        // (A table lays its cell out by its own rules: one cell as big as the content.)
        if (panel is TableLayoutPanel table)
        {
            table.ColumnCount = 1;
            table.RowCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, content.Width + 6));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, content.Height + 6));
        }

        panel.Controls.Add(new Panel { Size = content });
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 200) };
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return panel;
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void WithBothBars_OnADarkTheme_TheSquareIsFound_AtTheLowerRightCorner(string name, Func<ScrollableControl> make)
    {
        ScrollableControl panel = Show(make, new Size(600, 400));

        Assert.True(NativeControlTheme.TryGetScrollBarCorner(panel, out Rectangle corner));
        Assert.Equal(SystemInformation.VerticalScrollBarWidth * panel.DeviceDpi / 96, corner.Width);
        Assert.Equal(SystemInformation.HorizontalScrollBarHeight * panel.DeviceDpi / 96, corner.Height);
        Assert.InRange(corner.Right, panel.Width - 4, panel.Width);
        Assert.InRange(corner.Bottom, panel.Height - 4, panel.Height);
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void WithOneBarOnly_OrOnALightTheme_ThereIsNoSquare(string name, Func<ScrollableControl> make)
    {
        ScrollableControl wide = Show(make, new Size(600, 20));
        Assert.False(NativeControlTheme.TryGetScrollBarCorner(wide, out _));

        ScrollableControl both = Show(make, new Size(600, 400));
        UIColors.ApplyTheme(UIThemes.Light);
        Assert.False(NativeControlTheme.TryGetScrollBarCorner(both, out _));
    }
}
