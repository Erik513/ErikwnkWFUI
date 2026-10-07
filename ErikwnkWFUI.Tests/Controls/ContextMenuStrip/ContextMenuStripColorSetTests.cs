using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// The color set of the menu: without one the neutral gray as before, with one the
/// highlighted item takes its hover color and text and an item with an open submenu the
/// pressed ones; CreatePrimary is CreateStandard with the primary set.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripColorSetTests
{
    // An item that says it is highlighted (that flag is read-only).
    private sealed class StateItem : ToolStripMenuItem
    {
        private readonly bool _selected;

        public StateItem(bool selected)
        {
            _selected = selected;
        }

        public override bool Selected => _selected;
    }

    private static Color Outline => UIColors.GetOutlineColor(UIColors.BackgroundMediumElevated);

    private static ToolStripProfessionalRenderer Renderer(WfuiContextMenuStrip menu)
    {
        return Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(menu.Renderer);
    }

    private static Color TextOf(WfuiContextMenuStrip menu, bool selected)
    {
        return menu.Renderer.InvokePrivate<Color>("GetForeColor", new StateItem(selected));
    }

    [Fact]
    public void WithoutASet_TheLookIsTheNeutralGray_AsBefore()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();

        Assert.Null(menu.Colors);
        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
        Assert.Equal(UIColors.Darken(UIColors.BorderLight, 30), Renderer(menu).ColorTable.MenuItemPressedGradientBegin);
        Assert.Equal(UIColors.GetContrastingForeColor(UIColors.BorderLight), TextOf(menu, true));
    }

    [Fact]
    public void WithASet_TheHighlightIsItsHoverColor_AndThePressedItemItsPressedColor()
    {
        // A set whose colors already carry their text: nothing is shifted.
        UIColorSet set = UIStyles.Colors.CreateSet(
            Color.FromArgb(10, 70, 30), Color.FromArgb(10, 80, 40), Color.FromArgb(10, 90, 50), Color.FromArgb(255, 210, 0));
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(set);

        Assert.Same(set, menu.Colors);
        Assert.Equal(set.Hover, menu.SelectionBackColor);
        Assert.Equal(set.Hover, Renderer(menu).ColorTable.MenuItemSelectedGradientBegin);
        Assert.Equal(set.Pressed, Renderer(menu).ColorTable.MenuItemPressedGradientBegin);
        Assert.Equal(set.HoverText, TextOf(menu, true));
        Assert.Equal(UIColors.TextPrimary, TextOf(menu, false));
    }

    [Fact]
    public void TheFrame_IsTheOutlineWithoutASet_AndTheSetsStrongColorWithOne()
    {
        using WfuiContextMenuStrip gray = UIStyles.ContextMenuStrips.CreateStandard();
        using WfuiContextMenuStrip green = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);
        Color expected = UIColors.EnsureContrast(UIStyles.Colors.Sets.Green.Pressed, UIColors.BackgroundMediumElevated, 6.0);

        Assert.Equal(Outline, Renderer(gray).ColorTable.MenuBorder);
        Assert.Equal(expected, Renderer(green).ColorTable.MenuBorder);

        // The box of a check mark has the same border.
        Assert.Equal(expected, Renderer(green).ColorTable.ButtonSelectedBorder);
        Assert.Equal(Outline, Renderer(gray).ColorTable.ButtonSelectedBorder);
    }

    [Fact]
    public void AnExplicitSelectionColor_KeepsTheOutlineFrame()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);

        menu.SelectionBackColor = Color.Red;

        Assert.Equal(Outline, Renderer(menu).ColorTable.MenuBorder);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFrame_StandsOutFromTheMenu_ForEverySet_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard((UIColorSet)property.GetValue(null)!);
                double ratio = ColorContrastHelper.GetContrastRatio(Renderer(menu).ColorTable.MenuBorder, UIColors.BackgroundMediumElevated);

                if (ratio < 3.0)
                    shortfalls.Add(property.Name + " " + ratio.ToString("0.0"));
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }

    [Fact]
    public void AnExplicitSelectionColor_WinsOverTheSet()
    {
        Color explicitColor = Color.FromArgb(200, 200, 200);
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);

        menu.SelectionBackColor = explicitColor;

        Assert.Equal(explicitColor, Renderer(menu).ColorTable.MenuItemSelectedGradientBegin);
        Assert.Equal(UIColors.Darken(explicitColor, 30), Renderer(menu).ColorTable.MenuItemPressedGradientBegin);
        Assert.Equal(UIColors.GetContrastingForeColor(explicitColor), TextOf(menu, true));
    }

    [Fact]
    public void ASetChangedLater_IsFollowedByTheMenu()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripProfessionalRenderer renderer = Renderer(menu);

        menu.Colors = UIStyles.Colors.Sets.Red;

        Assert.NotEqual(UIColors.BorderLight, renderer.ColorTable.MenuItemSelectedGradientBegin);
        Assert.Equal(menu.SelectionBackColor, renderer.ColorTable.MenuItemSelectedGradientBegin);
    }

    [Fact]
    public void CreatePrimary_IsTheStandardWithThePrimarySet_OfTheAccentAtCreation()
    {
        Color accent = UIColors.Primary;

        try
        {
            using WfuiContextMenuStrip blue = UIStyles.ContextMenuStrips.CreatePrimary();
            Assert.Equal(UIColors.GetReadableFill(UIColors.PrimarySet.Hover, UIColors.PrimarySet.HoverText), blue.SelectionBackColor);

            UIColors.SetAccent(UIAccentColors.Purple);
            using WfuiContextMenuStrip purple = UIStyles.ContextMenuStrips.CreatePrimary();

            Assert.Equal(UIColors.GetReadableFill(UIColors.PrimarySet.Hover, UIColors.PrimarySet.HoverText), purple.SelectionBackColor);
            Assert.NotEqual(blue.SelectionBackColor, purple.SelectionBackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EverySet_GivesAHighlightWhoseTextReads_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard((UIColorSet)property.GetValue(null)!);
                ToolStripProfessionalRenderer renderer = Renderer(menu);

                double selected = ColorContrastHelper.GetContrastRatio(TextOf(menu, true), renderer.ColorTable.MenuItemSelectedGradientBegin);
                double pressed = ColorContrastHelper.GetContrastRatio(menu.Colors!.PressedText, renderer.ColorTable.MenuItemPressedGradientBegin);

                if (selected < 4.5)
                    shortfalls.Add(property.Name + " highlight " + selected.ToString("0.0"));

                if (pressed < 4.5)
                    shortfalls.Add(property.Name + " pressed " + pressed.ToString("0.0"));
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}
