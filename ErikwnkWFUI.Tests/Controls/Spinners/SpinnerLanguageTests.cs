using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>
/// The spinner has no translated text of its own. The language changes one thing: how the percentage is written for a screen reader
/// ("42%" in English, "42 %" in German), also while the spinner is shown. The digits in the ring and the text of the user stay as they are.
/// [Collection]: the language is static state of the process.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class SpinnerLanguageTests
{
    private static string Expected(UILanguage language, int value)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(language == UILanguage.German ? "de-DE" : "en-US");

        return (value / 100.0).ToString("P0", culture);
    }

    [Fact]
    public void ThePercentageForAScreenReader_FollowsTheLanguage_AlsoWhileTheSpinnerIsShown()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary();
        spinner.IsIndeterminate = false;
        spinner.Value = 42;

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () => Assert.Equal(Expected(language, 42), spinner.AccessibilityObject.Value));
        }

        // The same spinner, the language switched while it exists: nothing is kept from the language it was made in.
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            string german = spinner.AccessibilityObject.Value!;

            Assert.True(char.IsWhiteSpace(german[german.Length - 2]), "German writes a space before the percent sign: '" + german + "'");
        });
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("42%", spinner.AccessibilityObject.Value));
    }

    [Fact]
    public void TheDigitsInTheRingAndTheText_AreTheSameInEveryLanguage()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressStatus();
        spinner.IsIndeterminate = false;
        spinner.IsSpinning = false;
        spinner.Value = 73;
        spinner.Text = "Daten laden";
        spinner.SetBounds(0, 0, 200, 40);
        using Form host = OffscreenForm.Create(new Size(200, 40), Color.Black);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();

        Bitmap? first = null;

        try
        {
            foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            {
                LanguageTestHelper.RunWithLanguage(language, () =>
                {
                    Bitmap picture = Draw(spinner);

                    if (first == null)
                    {
                        first = picture;

                        return;
                    }

                    using (picture)
                        Assert.True(SamePixels(first, picture), "the picture changes with the language " + language);
                });
            }

            Assert.Equal("Daten laden", spinner.AccessibilityObject.Name);
        }
        finally
        {
            first?.Dispose();
        }
    }

    [Fact]
    public void AnUnknownLoad_HasNoValueForAScreenReader_InAnyLanguage()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary();
        spinner.Value = 42;

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            LanguageTestHelper.RunWithLanguage(language, () => Assert.Null(spinner.AccessibilityObject.Value));
    }
}
