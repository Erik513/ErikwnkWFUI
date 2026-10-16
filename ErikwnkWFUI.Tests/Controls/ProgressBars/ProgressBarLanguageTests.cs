using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>
/// The progress bars have no translated text of their own: the one thing the language changes is how the percentage is written
/// ("42%" in English, "42 %" in German), also while a bar is shown. The text of the user, and the tooltip with it, stay as they are.
/// [Collection]: the language is static state of the process.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ProgressBarLanguageTests
{
    private static string Expected(UILanguage language, int value)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(language == UILanguage.German ? "de-DE" : "en-US");

        return (value / 100.0).ToString("P0", culture);
    }

    [Fact]
    public void ThePercentage_IsWrittenTheWayOfEveryLanguage()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                foreach (int value in new[] { 0, 7, 42, 100 })
                    Assert.Equal(Expected(language, value), ProgressBarPainting.GetPercentText(0, 100, value));
            });
        }

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("42%", ProgressBarPainting.GetPercentText(0, 100, 42)));
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            string german = ProgressBarPainting.GetPercentText(0, 100, 42);

            Assert.EndsWith("%", german);
            Assert.True(char.IsWhiteSpace(german[german.Length - 2]), "German writes a space before the percent sign: '" + german + "'");
        });
    }

    [Fact]
    public void ThePercentage_IsNotChangedByTheCultureOfTheComputer()
    {
        CultureInfo before = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("42%", ProgressBarPainting.GetPercentText(0, 100, 42)));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    private static Form Host(Control bar, int width, int height)
    {
        Form host = OffscreenForm.Create(new Size(width, height + 10), Color.Black);
        bar.SetBounds(0, 5, width, height);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();

        return host;
    }

    private static Bitmap Draw(Control bar)
    {
        Bitmap picture = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(picture, new Rectangle(Point.Empty, bar.Size));

        return picture;
    }

    private static bool Same(Bitmap a, Bitmap b)
    {
        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
                if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
                    return false;

        return true;
    }

    [Fact]
    public void AShownBar_WritesThePercentageAgain_WhenTheLanguageIsSwitched()
    {
        foreach (bool thick in new[] { true, false })
        {
            Control bar;

            if (thick)
            {
                ErikwnkWFUI.Controls.ProgressBar progress = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
                progress.Value = 42;
                progress.ShowPercentage = true;
                bar = progress;
            }
            else
            {
                SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
                slim.Value = 42;
                slim.ShowPercentage = true;
                slim.TextPosition = SlimProgressBarTextPosition.Above;
                bar = slim;
            }

            using Form host = Host(bar, 300, thick ? 23 : 26);

            LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
            {
                Application.DoEvents();
                using Bitmap english = Draw(bar);

                int invalidated = 0;
                bar.Invalidated += (sender, e) => invalidated++;

                UIStrings.Language = UILanguage.German;
                Application.DoEvents();

                Assert.True(invalidated > 0, (thick ? "the bar" : "the slim bar") + " was not painted again by the switch of the language");

                using Bitmap german = Draw(bar);

                Assert.False(Same(english, german), (thick ? "the bar" : "the slim bar") + " did not write the percentage again");

                UIStrings.Language = UILanguage.English;
                Application.DoEvents();
                using Bitmap back = Draw(bar);

                Assert.True(Same(english, back), (thick ? "the bar" : "the slim bar") + " is not the same in English again");
            });
        }
    }

    [Fact]
    public void ASlimBarThatGrowsWithItsText_FollowsTheLanguage()
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.ShowPercentage = true;
        slim.TextPosition = SlimProgressBarTextPosition.Right;
        slim.AutoSize = true;
        using Form host = Host(slim, 300, 26);

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            slim.Size = slim.GetPreferredSize(Size.Empty);
            int english = slim.Width;

            UIStrings.Language = UILanguage.German;
            Application.DoEvents();

            Assert.True(slim.Width > english, "the German percentage is wider (a space): " + slim.Width + " after " + english);
        });
    }

    [Fact]
    public void TheTextOfTheUser_AndItsTooltip_AreNotTranslated()
    {
        const string text = "Verbinde mit dem Server und lade den ersten Teil der Daten";
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Text = text;
        using Form host = Host(bar, 150, 23);

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Application.DoEvents();
                using Bitmap picture = Draw(bar);

                Assert.Equal(text, bar.Text);
            });
        }
    }

    [Fact]
    public void ABarThatWasDisposed_LeavesTheLanguageEvent_AndIsNotKeptAlive()
    {
        System.Reflection.FieldInfo field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        int Listeners() => ((Delegate?)field.GetValue(null))?.GetInvocationList().Length ?? 0;
        int before = Listeners();

        ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        int whileAlive = Listeners();
        thick.Dispose();
        slim.Dispose();

        Assert.Equal(before + 2, whileAlive);
        Assert.Equal(before, Listeners());
    }
}
