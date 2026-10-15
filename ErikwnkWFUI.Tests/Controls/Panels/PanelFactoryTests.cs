using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The panels of <c>UIStyles</c>: which background each has, no frame and no room around them, and the sizes of a table.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelFactoryTests
{
    public static IEnumerable<object[]> Panels()
    {
        yield return new object[] { "Standard", (Func<Panel>)(() => UIStyles.Panels.CreateStandard()), (Func<Color>)(() => UIColors.BackgroundMedium), DockStyle.Fill };
        yield return new object[] { "Dark", (Func<Panel>)(() => UIStyles.Panels.CreateDark()), (Func<Color>)(() => UIColors.BackgroundDark), DockStyle.Fill };
        yield return new object[] { "Medium", (Func<Panel>)(() => UIStyles.Panels.CreateMedium()), (Func<Color>)(() => UIColors.BackgroundMedium), DockStyle.Fill };
        yield return new object[] { "Elevated", (Func<Panel>)(() => UIStyles.Panels.CreateElevated()), (Func<Color>)(() => UIColors.BackgroundMediumElevated), DockStyle.Fill };
        yield return new object[] { "Primary", (Func<Panel>)(() => UIStyles.Panels.CreatePrimary()), (Func<Color>)(() => UIColors.PrimaryDarkDark), DockStyle.Fill };
        yield return new object[] { "Transparent", (Func<Panel>)(() => UIStyles.Panels.CreateTransparent()), (Func<Color>)(() => Color.Transparent), DockStyle.None };
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void EachPanel_HasItsBackground_ItsDock_NoFrame_AndNoRoomAround(string name, Func<Panel> make, Func<Color> background, DockStyle dock)
    {
        using Panel panel = make();

        Assert.Equal(background(), panel.BackColor);
        Assert.Equal(dock, panel.Dock);
        Assert.Equal(BorderStyle.None, panel.BorderStyle);
        Assert.Equal(Padding.Empty, panel.Padding);
        Assert.Equal(Padding.Empty, panel.Margin);
    }

    [Fact]
    public void TheStandardPanel_IsTheMediumOne()
    {
        using Panel standard = UIStyles.Panels.CreateStandard();
        using Panel medium = UIStyles.Panels.CreateMedium();

        Assert.Equal(medium.BackColor, standard.BackColor);
        Assert.Equal(medium.Dock, standard.Dock);
    }

    [Fact]
    public void TheTablePanels_HaveTheirBackgrounds_ThePlacesAsGiven_AndNoRoomAround()
    {
        using TableLayoutPanel standard = UIStyles.TableLayoutPanels.CreateStandard(3, 2);
        using TableLayoutPanel dark = UIStyles.TableLayoutPanels.CreateDark(1, 4);

        Assert.Equal(UIColors.BackgroundMediumElevated, standard.BackColor);
        Assert.Equal(UIColors.BackgroundDark, dark.BackColor);
        Assert.Equal((3, 2), (standard.ColumnCount, standard.RowCount));
        Assert.Equal((1, 4), (dark.ColumnCount, dark.RowCount));
        Assert.Equal(Padding.Empty, standard.Padding);
        Assert.Equal(Padding.Empty, standard.Margin);
    }

    [Fact]
    public void ANegativeNumberOfColumnsOrRows_IsZero()
    {
        using TableLayoutPanel table = UIStyles.TableLayoutPanels.CreateStandard(-2, -5);

        Assert.Equal(0, table.ColumnCount);
        Assert.Equal(0, table.RowCount);
    }

    [Fact]
    public void TheFlowPanel_IsTransparent_AsBigAsItsContent_AndDoesNotWrap()
    {
        using FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();

        Assert.Equal(Color.Transparent, flow.BackColor);
        Assert.True(flow.AutoSize);
        Assert.False(flow.WrapContents);
        Assert.Equal(Padding.Empty, flow.Padding);
        Assert.Equal(Padding.Empty, flow.Margin);
    }

    [Fact]
    public void ThePrimaryPanel_IsMadeInTheAccentOfTheMoment()
    {
        Color accent = UIColors.Primary;

        try
        {
            UIColors.SetAccent(Color.OrangeRed);
            using Panel panel = UIStyles.Panels.CreatePrimary();

            Assert.Equal(UIColors.PrimaryDarkDark, panel.BackColor);
            Assert.NotEqual(Color.FromArgb(0, 30, 60), panel.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }
}
