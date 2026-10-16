using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// Colors of the checkbox: the mark has to be readable on the fill in every state, the
/// label on its background, the box against the page, and a disabled box has to stay
/// visible - in both themes. Measured on the picture the control draws of itself.
/// Hovered and pressed fills are the bright shades, shown for a moment: they need
/// less (3:1 hovered, 2:1 pressed) than the resting look.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class CheckBoxColorContrastTests
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
        {
            // The dark set is the theme's own fill: it is not a color for a box.
            if (property.Name == "Dark")
                continue;

            yield return (property.Name, (UIColorSet)property.GetValue(null)!);
        }
    }

    private static (Color Fill, Color Mark) Probe(Control box)
    {
        using Bitmap bitmap = Draw(box);
        int top = (box.Height - 16) / 2;

        return (bitmap.GetPixel(5, top + 13), bitmap.GetPixel(10, top + 8));
    }

    private static void Run(bool lightTheme, Action<List<string>> collect)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            collect(shortfalls);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            UIColors.SetAccent(UIAccentColors.Blue);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light" : "Dark") + " theme:\n" + string.Join("\n", shortfalls));
    }

    private static void Check(List<string> shortfalls, string what, Color foreground, Color background, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        if (ratio < minimum)
            shortfalls.Add($"{what}: {ratio:F2}:1, needs {minimum:F1}:1 (front {foreground}, back {background})");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheMark_IsReadable_OnTheFill_InEveryStateAndSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", true, set);
                box.ThreeState = true;
                box.CheckState = CheckState.Indeterminate;

                // The yellow keeps its bright fills with a white mark (the look of the folder
                // button): held to what its resting look has always had, hover and press aside.
                bool isYellow = name == "Yellow";

                (Color fill, Color mark) = Probe(box);
                Check(shortfalls, name + " at rest", mark, fill, isYellow ? 2.6 : 3.0);

                box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                (fill, mark) = Probe(box);

                if (!isYellow)
                    Check(shortfalls, name + " hovered", mark, fill, 3.0);

                box.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                (fill, mark) = Probe(box);

                if (!isYellow)
                    Check(shortfalls, name + " pressed", mark, fill, 2.0);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThePrimaryBox_IsReadable_WithEveryAccent_InEveryState(bool lightTheme)
    {
        Color[] accents = typeof(UIAccentColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Color))
            .Select(field => (Color)field.GetValue(null)!)
            .ToArray();

        Run(lightTheme, shortfalls =>
        {
            foreach (Color accent in accents)
            {
                UIColors.SetAccent(accent);
                string label = "accent " + accent.R + "," + accent.G + "," + accent.B;
                using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreatePrimary("x", true);
                box.ThreeState = true;
                box.CheckState = CheckState.Indeterminate;

                (Color fill, Color mark) = Probe(box);
                Check(shortfalls, label + " at rest", mark, fill, 3.0);

                box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                (fill, mark) = Probe(box);
                Check(shortfalls, label + " hovered", mark, fill, 3.0);

                box.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                (fill, mark) = Probe(box);
                Check(shortfalls, label + " pressed", mark, fill, 2.0);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheLabel_IsReadable_AndTheDisabledBoxAndLabelAreStillVisible(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            Color page = UIColors.BackgroundMedium;
            Check(shortfalls, "label", UIColors.TextPrimary, page, ColorContrastHelper.MinimumRatioNormalText);

            using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", true);
            box.ThreeState = true;
            box.CheckState = CheckState.Indeterminate;
            box.Enabled = false;

            (Color fill, Color mark) = Probe(box);
            Check(shortfalls, "disabled mark", mark, fill, 3.0);
            Check(shortfalls, "disabled fill against the page", fill, page, 1.8);

            // The disabled label: the contrasting color blended toward the page.
            Color disabledLabel = UIColors.BlendToward(UIColors.GetContrastingForeColor(page), page, 0.35);
            Check(shortfalls, "disabled label", disabledLabel, page, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheOutline_StandsOutFromThePage_EmptyAndFilled(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", false);
            ErikwnkWFUI.Controls.CheckBox control = (ErikwnkWFUI.Controls.CheckBox)box;

            // The outline carries the box (a fill can be close to the page), and 3:1 is
            // the bar for the boundary of a control.
            Check(shortfalls, "outline against the page", control.OutlineColor, UIColors.BackgroundMedium, 3.0);

            using Panel parent = new Panel { BackColor = Color.FromArgb(60, 90, 40), Size = new Size(100, 40) };
            parent.Controls.Add(box);
            Check(shortfalls, "outline against another parent color", control.OutlineColor, parent.BackColor, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void AnEmptyBox_GetsAStrongerOutline_WhenHoveredAndPressed_WithAnySet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", false, set);
                ErikwnkWFUI.Controls.CheckBox control = (ErikwnkWFUI.Controls.CheckBox)box;
                Color page = UIColors.BackgroundMedium;

                Check(shortfalls, name + " at rest", control.BorderColor, page, 3.0);

                box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                Check(shortfalls, name + " hovered", control.BorderColor, page, 4.5);

                box.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                Check(shortfalls, name + " pressed", control.BorderColor, page, 6.0);
            }
        });
    }
}
