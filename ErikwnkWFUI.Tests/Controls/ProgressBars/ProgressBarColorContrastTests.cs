using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>
/// The colors of the progress bars, measured on the picture a bar draws of itself: the fill has to stand out from the track (3:1, the
/// state the bar shows), the text has to be readable on the fill and on the track (4.5:1), a disabled bar has to stay visible - in both
/// themes, for every color set, the accent and the status colors.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarColorContrastTests
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
            // The dark set is the fill of the theme itself: it is not a color for a bar.
            if (property.Name == "Dark")
                continue;

            yield return (property.Name, (UIColorSet)property.GetValue(null)!);
        }
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

        Assert.True(shortfalls.Count == 0, string.Join(Environment.NewLine, shortfalls));
    }

    private static Bitmap Draw(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    private static Form Host(Control bar, int width, int height)
    {
        Form host = OffscreenForm.Create(new Size(width, height + 10), UIColors.BackgroundLight);
        bar.SetBounds(0, 5, width, height);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();

        return host;
    }

    private static void Check(List<string> shortfalls, string what, Color first, Color second, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(first, second);

        if (ratio < minimum)
            shortfalls.Add(what + ": " + ratio.ToString("0.00") + ":1, needs " + minimum.ToString("0.0"));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheFill_StandsOutFromTheTrack_ForEverySet_InBothBars(bool light)
    {
        Run(light, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(set);
                thick.Value = 50;
                using Form thickHost = Host(thick, 200, 23);
                using (Bitmap picture = Draw(thick))
                    Check(shortfalls, "bar " + name + ": fill on track", picture.GetPixel(40, 11), picture.GetPixel(160, 11), ProgressBarPainting.FillContrast);

                SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard(set);
                slim.Value = 50;
                using Form slimHost = Host(slim, 200, 4);
                using (Bitmap picture = Draw(slim))
                    Check(shortfalls, "slim bar " + name + ": fill on track", picture.GetPixel(40, 2), picture.GetPixel(160, 2), ProgressBarPainting.FillContrast);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThePrimaryFill_StandsOutFromTheTrack_ForEveryAccent(bool light)
    {
        Run(light, shortfalls =>
        {
            foreach (PropertyInfo property in typeof(UIAccentColors).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Color)))
            {
                UIColors.SetAccent((Color)property.GetValue(null)!);

                ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
                thick.Value = 50;
                using Form host = Host(thick, 200, 23);
                using Bitmap picture = Draw(thick);

                Check(shortfalls, "accent " + property.Name + ": fill on track", picture.GetPixel(40, 11), picture.GetPixel(160, 11), ProgressBarPainting.FillContrast);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheStatusFill_StandsOutFromTheTrack_AtEveryValue_InBothBars(bool light)
    {
        Run(light, shortfalls =>
        {
            // (At 0 there is no fill to look at: the color it would have is checked.)
            Check(shortfalls, "status 0: fill on track", ProgressBarPainting.GetStatusFill(0, 0, 100, UIColors.BackgroundMedium), UIColors.BackgroundMedium, ProgressBarPainting.FillContrast);

            for (int value = 5; value <= 100; value += 5)
            {
                ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStatus();
                thick.Value = value;
                using Form thickHost = Host(thick, 200, 23);
                using (Bitmap picture = Draw(thick))
                    Check(shortfalls, "status " + value + ": fill on track", picture.GetPixel(2, 11), thick.BackColor, ProgressBarPainting.FillContrast);

                SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStatus();
                slim.Value = value;
                using Form slimHost = Host(slim, 200, 4);
                using (Bitmap picture = Draw(slim))
                    Check(shortfalls, "slim status " + value + ": fill on track", picture.GetPixel(0, 2), slim.BackColor, ProgressBarPainting.FillContrast);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheText_CanBeReadOnTheFillAndOnTheTrack_ForEverySet(bool light)
    {
        Run(light, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(set);
                thick.Value = 50;
                using Form host = Host(thick, 200, 23);
                using Bitmap picture = Draw(thick);
                Color fill = picture.GetPixel(40, 11);
                Color track = picture.GetPixel(160, 11);

                Check(shortfalls, "bar " + name + ": text on fill", ProgressBarPainting.GetTextColorOn(fill), fill, ProgressBarPainting.TextContrast);
                Check(shortfalls, "bar " + name + ": text on track", ProgressBarPainting.GetTextColorOn(track), track, ProgressBarPainting.TextContrast);
            }

            // The colors the status bar goes through, and a disabled bar.
            for (int value = 0; value <= 100; value += 5)
            {
                Color fill = ProgressBarPainting.GetStatusFill(value, 0, 100, UIColors.BackgroundMedium);
                Check(shortfalls, "status " + value + ": text on fill", ProgressBarPainting.GetTextColorOn(fill), fill, ProgressBarPainting.TextContrast);
            }

            Check(shortfalls, "disabled: text on fill", ProgressBarPainting.GetTextColorOn(UIColors.DisabledGray), UIColors.DisabledGray, ProgressBarPainting.TextContrast);
            Check(shortfalls, "disabled: text on track", UIColors.GetDisabledText(UIColors.BackgroundMedium), UIColors.BackgroundMedium, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheSlimText_CanBeReadOnThePanel_AndTheDisabledOne(bool light)
    {
        Run(light, shortfalls =>
        {
            foreach (Color panel in new[] { UIColors.BackgroundLight, UIColors.BackgroundMedium, UIColors.BackgroundMediumElevated, UIColors.BackgroundDark })
            {
                Check(shortfalls, "slim text on " + panel, ProgressBarPainting.GetTextColorOn(panel), panel, ProgressBarPainting.TextContrast);
                Check(shortfalls, "slim disabled text on " + panel, UIColors.GetDisabledText(panel), panel, 3.0);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ADisabledBar_StaysVisible_AgainstItsTrack(bool light)
    {
        Run(light, shortfalls =>
        {
            ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            thick.Value = 50;
            thick.Enabled = false;
            using Form host = Host(thick, 200, 23);
            using Bitmap picture = Draw(thick);

            Check(shortfalls, "disabled fill on track", picture.GetPixel(40, 11), picture.GetPixel(160, 11), ProgressBarPainting.FillContrast);
        });
    }
}
