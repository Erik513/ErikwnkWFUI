using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// Readability of the label text in both themes against every background of the theme, by WCAG's contrast
/// math (see ColorContrastHelper). Three bars: the text of the standard, the primary and the title label has to
/// meet 4.5:1; the muted label is quiet by purpose, so it only has to stay readable (3:1); a disabled label, which
/// WCAG exempts, must still be visible (3:1). Every case that falls short is listed in one message, so a failing
/// run shows the whole picture. [Collection]: the colors read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelColorContrastTests
{
    private const double Normal = 4.5;
    private const double Quiet = 3.0;

    private static void RunWithTheme(UIColorTheme theme, Action<List<string>> collect)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(theme);
            collect(shortfalls);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, string.Join("; ", shortfalls));
    }

    // The backgrounds a label can sit on: every surface of the theme.
    private static (string Name, Color Color)[] Backgrounds() => new[]
    {
        ("Black", UIColors.BackgroundBlack), ("Dark", UIColors.BackgroundDark),
        ("DarkElevated", UIColors.BackgroundDarkElevated), ("Medium", UIColors.BackgroundMedium),
        ("MediumElevated", UIColors.BackgroundMediumElevated), ("Light", UIColors.BackgroundLight),
        ("Lighter", UIColors.BackgroundLighter)
    };

    private static Color TextOf(Label label) => label.InvokePrivate<Color>("GetTextColor");

    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Dark" };
        yield return new object[] { "Light" };
    }

    private static UIColorTheme ThemeNamed(string name) => name == "Light" ? UIThemes.Light : UIThemes.Dark;

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTextOfTheStandardPrimaryAndTitleLabel_ReadsOnEverySurface(string themeName)
    {
        RunWithTheme(ThemeNamed(themeName), shortfalls =>
        {
            foreach ((string name, Color background) in Backgrounds())
            {
                foreach ((string kind, Label label) in new (string, Label)[]
                {
                    ("Standard", UIStyles.Labels.CreateStandard("x")),
                    ("Primary", UIStyles.Labels.CreatePrimary("x")),
                    ("Title", UIStyles.Labels.CreateTitle("x"))
                })
                {
                    using (label)
                    {
                        using Panel page = new Panel { BackColor = background };
                        page.Controls.Add(label);
                        double ratio = ColorContrastHelper.GetContrastRatio(TextOf(label), background);

                        if (ratio < Normal)
                            shortfalls.Add(themeName + " " + kind + " on " + name + ": " + ratio.ToString("0.0"));
                    }
                }
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTextOfTheMutedLabel_StaysReadable_OnEverySurface(string themeName)
    {
        RunWithTheme(ThemeNamed(themeName), shortfalls =>
        {
            foreach ((string name, Color background) in Backgrounds())
            {
                using Label label = UIStyles.Labels.CreateMuted("x");
                using Panel page = new Panel { BackColor = background };
                page.Controls.Add(label);
                double ratio = ColorContrastHelper.GetContrastRatio(TextOf(label), background);

                if (ratio < Quiet)
                    shortfalls.Add(themeName + " Muted on " + name + ": " + ratio.ToString("0.0"));
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTextOfADisabledLabel_IsStillVisible_OnEverySurface(string themeName)
    {
        RunWithTheme(ThemeNamed(themeName), shortfalls =>
        {
            foreach ((string name, Color background) in Backgrounds())
            {
                using Label label = UIStyles.Labels.CreateStandard("x");
                label.Enabled = false;
                using Panel page = new Panel { BackColor = background };
                page.Controls.Add(label);
                double ratio = ColorContrastHelper.GetContrastRatio(TextOf(label), background);

                if (ratio < Quiet)
                    shortfalls.Add(themeName + " disabled on " + name + ": " + ratio.ToString("0.0"));
            }
        });
    }

    // ---- what is really on the screen ----

    // The painted picture of a label of the kind, in a shown window (out of sight) on the medium surface.
    private static Bitmap PaintOnMedium(Label label, List<Form> forms)
    {
        label.Size = new Size(160, 26);
        label.Location = new Point(5, 5);

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = UIColors.BackgroundMedium,
            ClientSize = new Size(170, 36)
        };
        form.Controls.Add(label);
        form.Show();
        forms.Add(form);

        using Bitmap whole = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(whole, new Rectangle(0, 0, form.Width, form.Height));

        return whole.Clone(new Rectangle(5, 5, 160, 26), whole.PixelFormat);
    }

    // The pixel that differs most from the surface: the text at its strongest.
    private static double StrongestContrast(Bitmap bitmap)
    {
        Color surface = bitmap.GetPixel(0, 0);
        double strongest = 1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
                strongest = Math.Max(strongest, ColorContrastHelper.GetContrastRatio(bitmap.GetPixel(x, y), surface));
        }

        return strongest;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheTextOnTheScreen_ReadsInBothThemes_AlsoWhileItMoves(string themeName)
    {
        List<Form> forms = new List<Form>();

        try
        {
            RunWithTheme(ThemeNamed(themeName), shortfalls =>
            {
                foreach ((string kind, Label label) in new (string, Label)[]
                {
                    ("Standard", UIStyles.Labels.CreateStandard("Readable text on the screen")),
                    ("Title", UIStyles.Labels.CreateTitle("Readable text on the screen")),
                    ("Muted", UIStyles.Labels.CreateMuted("Readable text on the screen")),
                    ("Standard, moving", UIStyles.Labels.CreateStandard("Readable text on the screen, long enough to move around", scroll: LabelScroll.BackAndForth())),
                    ("Primary, endless", UIStyles.Labels.CreatePrimary("Readable text on the screen, long enough to move around", LabelScroll.Endless()))
                })
                {
                    using (label)
                    {
                        using Bitmap painted = PaintOnMedium(label, forms);
                        double strongest = StrongestContrast(painted);

                        // (The smoothed letters never reach the full contrast of the color: a pixel here and there.)
                        if (strongest < 2.5)
                            shortfalls.Add(themeName + " " + kind + ": the strongest pixel only " + strongest.ToString("0.0"));
                    }
                }
            });
        }
        finally
        {
            foreach (Form form in forms)
                form.Dispose();
        }
    }
}
