using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// The colors of the combo box are readable in both themes and for every color set: the text
/// of the closed box and of a row, the selected row, the arrow, the disabled look and the frame
/// of the open list. The border at rest and hovered is in ComboBoxClosedBoxTests.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxColorContrastTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { false };
        yield return new object[] { true };
    }

    private static IEnumerable<(string Name, UIColorSet? Set)> AllSets()
    {
        yield return ("default gray", null);

        foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            yield return (property.Name, (UIColorSet)property.GetValue(null)!);
    }

    private static void Check(List<string> shortfalls, string what, Color foreground, Color background, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        if (ratio < minimum)
            shortfalls.Add(what + " " + ratio.ToString("0.0") + ":1 (needs " + minimum.ToString("0.0") + ")");
    }

    private static Bitmap DrawRow(System.Windows.Forms.ComboBox box, DrawItemState state)
    {
        Bitmap bitmap = new Bitmap(120, 24);

        using (Graphics g = Graphics.FromImage(bitmap))
            box.InvokePrivate("OnDrawItem", new DrawItemEventArgs(g, box.Font, new Rectangle(0, 0, 120, 24), 0, state));

        return bitmap;
    }

    // The text color of a row: the pixel farthest from the row's own background.
    private static Color TextOf(Bitmap row, Color back)
    {
        Color best = back;
        double most = 0;

        for (int y = 0; y < row.Height; y++)
        {
            for (int x = 0; x < row.Width; x++)
            {
                double ratio = ColorContrastHelper.GetContrastRatio(row.GetPixel(x, y), back);

                if (ratio > most)
                {
                    most = ratio;
                    best = row.GetPixel(x, y);
                }
            }
        }

        return best;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTexts_AreReadable_ForEverySet_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDownList, set);
                box.Items.Add("Item");

                Check(shortfalls, name + " closed text", box.ForeColor, box.BackColor, 4.5);

                // An ordinary row and the selected one.
                using (Bitmap plain = DrawRow(box, DrawItemState.None))
                    Check(shortfalls, name + " row text", TextOf(plain, plain.GetPixel(115, 12)), plain.GetPixel(115, 12), 4.5);

                // The selected row: the set's text on the row, which keeps the set's hue (a
                // lightness shift only) - it must neither be a different color nor lose contrast.
                using (Bitmap selected = DrawRow(box, DrawItemState.Selected))
                {
                    Color rowBack = selected.GetPixel(115, 12);
                    Color text = set != null ? set.HoverText : UIColors.GetContrastingForeColor(rowBack);

                    Check(shortfalls, name + " selected row text", text, rowBack, 4.5);

                    if (set != null && ColorContrastHelper.GetContrastRatio(set.HoverText, set.Hover) >= 4.5 && rowBack != set.Hover)
                        shortfalls.Add(name + " selected row changed although its text reads");

                    if (set != null && Math.Abs(rowBack.GetBrightness() - set.Hover.GetBrightness()) > 0.25)
                        shortfalls.Add(name + " selected row is far from the set's color");
                }

                // The arrow is a mark, not text: 3:1 is enough, also in its hovered color.
                Check(shortfalls, name + " arrow", box.ForeColor, box.BackColor, 3.0);
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheDisabledLook_AndTheListFrame_StayVisible_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDownList, set);
                box.Items.Add("Item");

                // The open list's frame is the strongest border.
                Check(shortfalls, name + " list frame", box.ListBorderColor, box.BackColor, 6.0);

                box.Enabled = false;

                Check(shortfalls, name + " disabled text", UIColors.GetDisabledText(box.BackColor), box.BackColor, 3.0);
                Check(shortfalls, name + " disabled border", box.BorderColor, box.BackColor, 1.8);

                using (Bitmap row = DrawRow(box, DrawItemState.None))
                {
                    Color back = row.GetPixel(115, 12);
                    Check(shortfalls, name + " disabled row text", TextOf(row, back), back, 3.0);
                }
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}
