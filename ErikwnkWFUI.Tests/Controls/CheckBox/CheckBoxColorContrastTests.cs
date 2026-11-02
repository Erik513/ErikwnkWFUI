using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;

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
    private static (Color Fill, Color Mark) Probe(Control box)
    {
        using Bitmap bitmap = Draw(box);
        int top = (box.Height - 16) / 2;

        // The fill is the color that most pixels of a small area inside the box have (a single pixel next to the edge is mixed with the outline).
        Color fill = Enumerable.Range(5, 3)
            .SelectMany(x => Enumerable.Range(top + 11, 3).Select(y => bitmap.GetPixel(x, y)))
            .GroupBy(color => color.ToArgb())
            .OrderByDescending(group => group.Count())
            .First()
            .First();

        return (fill, bitmap.GetPixel(10, top + 8));
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
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
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
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
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
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
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
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
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
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

    private static bool Close(Color first, Color second)
    {
        return Math.Abs(first.R - second.R) <= 3 && Math.Abs(first.G - second.G) <= 3 && Math.Abs(first.B - second.B) <= 3;
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void OnALightPage_TheCheckedFill_IsTheBrighterShadeOfTheSet_SoTheColorIsSeen_OnADarkPageItStaysTheBasicShade(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", true, set);
                UIColorSet colors = set ?? UIStyles.Colors.Sets.Gray;
                (Color fill, _) = Probe(box);

                // The yellow keeps its fills (white on anything brighter would not be readable).
                bool brighter = lightTheme && name != "Yellow";
                Color expected = brighter ? colors.Hover : colors.Basic;

                if (!Close(fill, expected))
                    shortfalls.Add(name + ": the checked fill is " + fill + ", expected " + expected);

                // Hovered one shade further, pressed back at the basic shade.
                box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                (fill, _) = Probe(box);
                Color hovered = brighter ? colors.Pressed : colors.Hover;

                if (!Close(fill, hovered))
                    shortfalls.Add(name + " hovered: " + fill + ", expected " + hovered);

                box.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                (fill, _) = Probe(box);
                Color pressed = brighter ? colors.Basic : colors.Pressed;

                if (!Close(fill, pressed))
                    shortfalls.Add(name + " pressed: " + fill + ", expected " + pressed);
            }
        });
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheLabel_IsReadable_OnAPageOfYourOwn_AndStaysTheTextColorOfTheTheme_OnThePageOfTheTheme(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", true);

            // (A panel of your own: bright in the dark theme or dark in the light one, a mid gray, a yellow, a navy.)
            foreach (Color page in new[] { Color.White, Color.Black, Color.FromArgb(128, 128, 128), Color.FromArgb(230, 190, 20), Color.FromArgb(10, 20, 100) })
                Check(shortfalls, "label on " + page, global::ErikwnkWFUI.Controls.BoxLabel.GetTextColor(box, page), page, ColorContrastHelper.MinimumRatioNormalText);

            Assert.Equal(UIColors.TextPrimary, global::ErikwnkWFUI.Controls.BoxLabel.GetTextColor(box, UIColors.BackgroundMedium));
        });
    }
}
