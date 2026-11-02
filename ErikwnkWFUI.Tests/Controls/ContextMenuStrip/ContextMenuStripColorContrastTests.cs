using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// The colors of the gray menu are readable in both themes: the text of an item, the text of
/// the highlighted and of the disabled item, the arrow, and the frame against the menu. The
/// sets (highlight and frame) are in ContextMenuStripColorSetTests.
/// [Collection]: the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripColorContrastTests
{
    private sealed class StateItem : ToolStripMenuItem
    {
        private readonly bool _selected;

        public StateItem(bool selected, bool enabled)
        {
            _selected = selected;
            Enabled = enabled;
        }

        public override bool Selected => _selected;
    }

    private static void Check(List<string> shortfalls, string what, Color foreground, Color background, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        if (ratio < minimum)
            shortfalls.Add(what + " " + ratio.ToString("0.0") + ":1 (needs " + minimum.ToString("0.0") + ")");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheGrayMenu_IsReadable_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
            ToolStripProfessionalRenderer renderer = Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(menu.Renderer);
            Color back = renderer.ColorTable.ToolStripDropDownBackground;
            Color highlight = renderer.ColorTable.MenuItemSelectedGradientBegin;
            Color pressed = renderer.ColorTable.MenuItemPressedGradientBegin;

            Check(shortfalls, "text", menu.Renderer.InvokePrivate<Color>("GetForeColor", new StateItem(false, true)), back, 4.5);
            Check(shortfalls, "highlighted text", menu.Renderer.InvokePrivate<Color>("GetForeColor", new StateItem(true, true)), highlight, 4.5);
            Check(shortfalls, "pressed text", UIColors.GetContrastingForeColor(highlight), pressed, 4.5);
            Check(shortfalls, "disabled text", menu.Renderer.InvokePrivate<Color>("GetForeColor", new StateItem(false, false)), back, 3.0);
            Check(shortfalls, "frame", renderer.ColorTable.MenuBorder, back, 3.0);
            Check(shortfalls, "item border", renderer.ColorTable.MenuItemBorder, highlight, 1.2);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}
