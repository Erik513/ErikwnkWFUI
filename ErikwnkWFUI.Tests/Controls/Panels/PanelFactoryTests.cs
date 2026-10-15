using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
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

    public static IEnumerable<object[]> ScrollingPanels()
    {
        yield return new object[] { "Panel", (Func<Control>)(() => UIStyles.Panels.CreateStandard()) };
        yield return new object[] { "Transparent", (Func<Control>)(() => UIStyles.Panels.CreateTransparent()) };
        yield return new object[] { "Table", (Func<Control>)(() => UIStyles.TableLayoutPanels.CreateStandard(1, 1)) };
        yield return new object[] { "Flow", (Func<Control>)(() => UIStyles.FlowLayoutPanels.CreateStandard()) };
    }

    // The test process has no visual styles to look at, so what is asked of the system is recorded.
    [Theory]
    [MemberData(nameof(ScrollingPanels))]
    public void TheScrollBarsOfAPanel_TakeTheStyleOfTheTheme_AndFollowItsChange(string name, Func<Control> make)
    {
        var log = new System.Collections.Concurrent.ConcurrentQueue<(IntPtr Handle, string Style)>();
        Action<IntPtr, string> original = NativeControlTheme.SetStyle;
        NativeControlTheme.SetStyle = (handle, style) =>
        {
            log.Enqueue((handle, style));
            original(handle, style);
        };

        Control panel = make();

        try
        {
            using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(200, 100) };
            host.Controls.Add(panel);
            host.Show();
            Application.DoEvents();

            Assert.Equal("DarkMode_Explorer", log.Last(entry => entry.Handle == panel.Handle).Style);

            UIColors.ApplyTheme(UIThemes.Light);
            Application.DoEvents();

            Assert.Equal("Explorer", log.Last(entry => entry.Handle == panel.Handle).Style);
        }
        finally
        {
            NativeControlTheme.SetStyle = original;
            UIColors.ApplyTheme(UIThemes.Dark);
            panel.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ScrollingPanels))]
    public void ThePanels_AreTheThemedClasses_AndAreDrawnWithoutFlicker(string name, Func<Control> make)
    {
        using Control control = make();

        Type expected = control switch
        {
            TableLayoutPanel => typeof(ErikwnkWFUI.Controls.ThemedTableLayoutPanel),
            FlowLayoutPanel => typeof(ErikwnkWFUI.Controls.ThemedFlowLayoutPanel),
            _ => typeof(ErikwnkWFUI.Controls.ThemedPanel)
        };

        Assert.IsType(expected, control);
        Assert.True((bool)typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control)!);
    }

    [Fact]
    public void WithAColorSet_APanelIsInTheBasicColorOfTheSet_AndWithoutOneNothingChanges()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;

        using Control standard = UIStyles.Panels.CreateStandard(green);
        using Control table = UIStyles.TableLayoutPanels.CreateStandard(2, 2, green);
        using Control flow = UIStyles.FlowLayoutPanels.CreateStandard(green);
        using Control plainStandard = UIStyles.Panels.CreateStandard();
        using Control plainTable = UIStyles.TableLayoutPanels.CreateStandard(2, 2);
        using Control plainFlow = UIStyles.FlowLayoutPanels.CreateStandard();

        Assert.Equal(green.Basic, standard.BackColor);
        Assert.Equal(green.Basic, table.BackColor);
        Assert.Equal(green.Basic, flow.BackColor);
        Assert.Equal(DockStyle.Fill, standard.Dock);
        Assert.Equal(UIColors.BackgroundMedium, plainStandard.BackColor);
        Assert.Equal(UIColors.BackgroundMediumElevated, plainTable.BackColor);
        Assert.Equal(Color.Transparent, plainFlow.BackColor);
    }

    [Fact]
    public void ACardWithAColorSet_IsInItsBasicColor_AndHasTheHoverColorAsItsLine()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;

        using ErikwnkWFUI.Controls.ThemedCardPanel card = (ErikwnkWFUI.Controls.ThemedCardPanel)UIStyles.Panels.CreateCard(green);
        using ErikwnkWFUI.Controls.ThemedCardPanel plain = (ErikwnkWFUI.Controls.ThemedCardPanel)UIStyles.Panels.CreateCard();

        Assert.Equal(green.Basic, card.BackColor);
        Assert.Equal(green.Hover, card.BorderColor);
        Assert.Equal(UIColors.BackgroundMediumElevated, plain.BackColor);
        Assert.Equal(UIColors.BorderMedium, plain.BorderColor);

        card.BorderColor = Color.Red;
        Assert.Equal(Color.Red, card.BorderColor);
    }
}
