using System.Drawing;
using System.Reflection;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The text of the popup reads: on its own background in every theme and for every color set,
/// and, because the whole window is see-through, against what is behind it - measured over
/// the two worst pages, white and black. [Collection]: the sets read the live accent, and the
/// tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupColorContrastTests
{
    private static IEnumerable<(string Name, UIColorSet Set)> AllSets()
    {
        foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            yield return (property.Name, (UIColorSet)property.GetValue(null)!);
    }

    // What shows on the screen: the window (and so the text on it) blended with the page behind it.
    private static double Effective(Color text, Color back, double opacity, Color page)
    {
        int alpha = (int)Math.Round(opacity * 255);
        Color seenBack = ColorContrastHelper.Composite(Color.FromArgb(alpha, back), page);
        Color seenText = ColorContrastHelper.Composite(Color.FromArgb(alpha, text), page);

        return ColorContrastHelper.GetContrastRatio(seenText, seenBack);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheText_ReadsOnItsBackground_ForEverySet_InBothThemes(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach ((string name, UIColorSet set) in AllSets())
            {
                using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(set, transparency: PopupTransparency.Opaque);
                Color text = popup.InvokePrivate<Color>("GetForeColor");
                double ratio = ColorContrastHelper.GetContrastRatio(text, popup.BackColor);

                if (ratio < 4.5)
                    shortfalls.Add(name + " " + ratio.ToString("0.0"));
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }

    [Fact]
    public void ThePopupInTheGrayAndTheAccent_ReadsOverAnyPage_ExceptAtTheStrongestTransparency()
    {
        List<string> shortfalls = new List<string>();

        foreach (UIColorSet set in new[] { UIStyles.Colors.Sets.Gray, UIColors.PrimarySet })
        {
            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(set, transparency: level);
                Color text = popup.InvokePrivate<Color>("GetForeColor");

                foreach (Color page in new[] { Color.White, Color.Black })
                {
                    double ratio = Effective(text, popup.BackColor, popup.Opacity, page);
                    // The strongest level gives up some contrast on purpose; it still has to be readable.
                    double needed = level == PopupTransparency.High ? 3.0 : 4.5;

                    if (ratio < needed)
                        shortfalls.Add(level + " over " + page.Name + " " + ratio.ToString("0.0"));
                }
            }
        }

        Assert.True(shortfalls.Count == 0, string.Join(", ", shortfalls));
    }

    [Fact]
    public void EverySetAtTheMediumDefault_ReadsOverAWhitePageAndABlackOne()
    {
        List<string> shortfalls = new List<string>();

        foreach ((string name, UIColorSet set) in AllSets())
        {
            using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(set);
            Color text = popup.InvokePrivate<Color>("GetForeColor");

            foreach (Color page in new[] { Color.White, Color.Black })
            {
                double ratio = Effective(text, popup.BackColor, popup.Opacity, page);

                if (ratio < 3.0)
                    shortfalls.Add(name + " over " + page.Name + " " + ratio.ToString("0.0"));
            }
        }

        Assert.True(shortfalls.Count == 0, string.Join(", ", shortfalls));
    }
}
