using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// Readability of the colors of the list box and of the title bar, in both themes: the text on the normal, the
/// hovered, the selected and the disabled item, the handle on each of them, the line that shows where a dragged item
/// lands, and how far the selection stands out from its row. The controls are made after the theme is switched (their
/// default colors are read when they are made). [Collection]: the themes are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListBoxColorContrastTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { false };
        yield return new object[] { true };
    }

    public static IEnumerable<object[]> ThemesAndSets()
    {
        foreach (bool light in new[] { false, true })
        {
            yield return new object[] { light, UIStyles.Colors.Sets.Green };
            yield return new object[] { light, UIStyles.Colors.Sets.Yellow };
            yield return new object[] { light, UIStyles.Colors.Sets.Purple };
            yield return new object[] { light, UIStyles.Colors.Sets.Primary };
            yield return new object[] { light, UIStyles.Colors.Sets.Gray };
        }
    }

    private static void WithTheme(bool light, Action test)
    {
        try
        {
            UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
            test();
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    private static double Ratio(Color a, Color b) => ColorContrastHelper.GetContrastRatio(a, b);

    private static void AtLeast(double minimum, Color foreground, Color background, string what)
    {
        double ratio = Ratio(foreground, background);

        Assert.True(ratio >= minimum, $"{what}: {ratio:F2}:1, needs {minimum:F1}:1 ({foreground} on {background})");
    }

    private static Color Handle(WfuiListBox listBox, Color text, Color back) => listBox.InvokePrivate<Color>("GetDragHandleColor", text, back);

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTextOfThePlainAndTheHoveredItem_Reads(bool light)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = new WfuiListBox();
            Color odd = listBox.InvokePrivate<Color>("GetBackColor", 1, false, false, false);
            Color even = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, false);

            AtLeast(ColorContrastHelper.MinimumRatioNormalText, listBox.ItemForeColor, even, "text on an even row");
            AtLeast(ColorContrastHelper.MinimumRatioNormalText, listBox.ItemForeColor, odd, "text on an odd row");
            AtLeast(ColorContrastHelper.MinimumRatioNormalText, listBox.ItemForeColor, listBox.HoverBackColor, "text on the hovered row");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheGraySelection_ReadsAndStandsOutFromItsRow(bool light)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();

            AtLeast(ColorContrastHelper.MinimumRatioNormalText, listBox.SelectedForeColor, listBox.SelectedBackColor, "text on the selection");
            Assert.True(Ratio(listBox.SelectedBackColor, listBox.ItemBackColor) >= 1.3, "the selection does not stand out from its row");
        });
    }

    [Theory]
    [MemberData(nameof(ThemesAndSets))]
    public void ASelectionInAColor_Reads_AndItsHandleToo(bool light, UIColorSet set)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard(colors: set);

            AtLeast(ColorContrastHelper.MinimumRatioNormalText, listBox.SelectedForeColor, listBox.SelectedBackColor, "text on the selection");
            AtLeast(3.0, Handle(listBox, listBox.SelectedForeColor, listBox.SelectedBackColor), listBox.SelectedBackColor, "the handle on the selection");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheHandle_ReadsOnEveryKindOfItem(bool light)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = new WfuiListBox();

            foreach ((string kind, Color text, Color back) in new[]
            {
                ("an even row", listBox.ItemForeColor, listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, false)),
                ("an odd row", listBox.ItemForeColor, listBox.InvokePrivate<Color>("GetBackColor", 1, false, false, false)),
                ("the hovered row", listBox.ItemForeColor, listBox.HoverBackColor),
                ("the selection", listBox.SelectedForeColor, listBox.SelectedBackColor)
            })
            {
                AtLeast(3.0, Handle(listBox, text, back), back, "the handle on " + kind);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ADisabledItem_AndADisabledList_Read(bool light)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = new WfuiListBox();

            // A disabled item of an enabled list: its own background; a disabled list: the row's or the gray one.
            Color itemBack = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, true);
            AtLeast(3.0, listBox.InvokePrivate<Color>("GetTextColorOn", false, true, itemBack), itemBack, "a disabled item");

            listBox.Enabled = false;
            Color rowBack = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, false);
            Color selectedBack = listBox.InvokePrivate<Color>("GetBackColor", 0, true, false, false);
            AtLeast(3.0, listBox.InvokePrivate<Color>("GetTextColorOn", false, false, rowBack), rowBack, "a row of a disabled list");
            AtLeast(3.0, listBox.InvokePrivate<Color>("GetTextColorOn", true, false, selectedBack), selectedBack, "the selection of a disabled list");
            AtLeast(3.0, Handle(listBox, listBox.InvokePrivate<Color>("GetTextColorOn", false, false, rowBack), rowBack), rowBack, "the handle of a disabled list");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheDropLine_ShowsOnTheRows(bool light)
    {
        WithTheme(light, () =>
        {
            using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();

            AtLeast(1.5, listBox.DragIndicatorColor, listBox.ItemBackColor, "the drop line on a row");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTitle_Reads_AndTheBarStandsOutFromTheList(bool light)
    {
        WithTheme(light, () =>
        {
            using TitledListBox control = UIStyles.TitledListBoxes.CreateStandard("Title");

            AtLeast(ColorContrastHelper.MinimumRatioNormalText, control.HeaderForeColor, control.HeaderBackColor, "the title on its bar");
            Assert.NotEqual(control.HeaderBackColor, control.ItemBackColor);
        });
    }
}
