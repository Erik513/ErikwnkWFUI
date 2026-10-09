using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The label has no text of its own - the text comes from the caller, the separators of the endless mode are
/// signs - so there is nothing to translate in it. What a language switch can still touch: the text that was handed
/// over stays as it was, the signs are the same in every language, and the longer German text (and its
/// umlauts) is measured and painted as well as the English one when it moves. The animation setting of Windows
/// has nothing to do with the language. [Collection]: the tests switch the language.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class LabelLanguageTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private ThemedLabel Show(ThemedLabel label, int width = 120)
    {
        label.Bounds = new Rectangle(5, 5, width, 24);

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(40, 40, 40),
            ClientSize = new Size(width + 10, 40)
        };
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);

        return label;
    }

    [Fact]
    public void TheText_ThatWasHandedOver_StaysAsItWas_WhenTheLanguageChanges()
    {
        string english = "";
        ThemedLabel label = null!;

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            english = UIStrings.Get("MessageBox.Cancel");
            label = UIStyles.Labels.CreateStandard(english);
        });

        using (label)
        {
            LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
            {
                Assert.Equal(english, label.Text);
                Assert.NotEqual(UIStrings.Get("MessageBox.Cancel"), label.Text);
            });
        }
    }

    [Fact]
    public void EveryKind_TakesTheTextOfEveryLanguage_AsItIs()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string text = "Text für " + language + " & Co.";

                foreach (Label label in new Label[]
                {
                    UIStyles.Labels.CreateStandard(text), UIStyles.Labels.CreatePrimary(text),
                    UIStyles.Labels.CreateTitle(text), UIStyles.Labels.CreateMuted(text)
                })
                {
                    using (label)
                        Assert.Equal(text, label.Text);
                }
            });
        }
    }

    [Fact]
    public void TheSeparatorSigns_AreTheSameInEveryLanguage()
    {
        foreach (LabelScrollSeparator separator in Enum.GetValues(typeof(LabelScrollSeparator)))
        {
            string english = "";
            LanguageTestHelper.RunWithLanguage(UILanguage.English, () => english = separator.ToText());

            foreach (UILanguage language in LanguageTestHelper.AllLanguages)
                LanguageTestHelper.RunWithLanguage(language, () => Assert.Equal(english, separator.ToText()));
        }
    }

    [Fact]
    public void ALongGermanText_WithUmlauts_MovesLikeAnEnglishOne_BothWays()
    {
        string german = "Die Geschwindigkeitsbegrenzung auf Bundesautobahnen ist ein häufig diskutiertes Thema in der Verkehrspolitik";
        string english = "The speed limit on federal motorways is a frequently discussed subject in the politics of transport";

        foreach (LabelScroll scroll in new[] { LabelScroll.BackAndForth(), LabelScroll.Endless() })
        {
            foreach (string text in new[] { german, english })
            {
                using ThemedLabel label = Show(UIStyles.Labels.CreateStandard(text, scroll: scroll));

                Assert.True(label.GetPrivateField<bool>("_animating"), "the text did not start to move: " + scroll.Mode + " " + text.Substring(0, 12));
                Assert.True(label.GetPrivateField<int>("_textWidth") > 200);
            }
        }
    }

    [Fact]
    public void AGermanCompoundWord_AsOneUnbreakableWord_MovesToo()
    {
        using ThemedLabel label = Show(UIStyles.Labels.CreateStandard(
            "Donaudampfschifffahrtsgesellschaftskapitänsmützenabzeichenhersteller", scroll: LabelScroll.BackAndForth()));

        Assert.True(label.GetPrivateField<bool>("_animating"));
        Assert.True(label.GetPrivateField<int>("_maxOffset") > 100);
    }

    [Fact]
    public void TheUmlauts_ArePaintedInTheMovingPicture()
    {
        using ThemedLabel label = Show(UIStyles.Labels.CreateStandard("ÄÖÜäöüß - Umlaute und noch mehr Text zum Bewegen", scroll: LabelScroll.BackAndForth()));
        using Bitmap bitmap = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(bitmap, new Rectangle(0, 0, label.Width, label.Height));

        bool ink = false;

        for (int y = 0; y < bitmap.Height && !ink; y++)
        {
            for (int x = 0; x < bitmap.Width && !ink; x++)
                ink = bitmap.GetPixel(x, y).GetBrightness() > 0.5f;
        }

        Assert.True(ink, "no text painted");
    }

    [Fact]
    public void TheAnimationSettingOfWindows_DoesNotDependOnTheLanguage()
    {
        Func<bool> original = ThemedLabel.AnimationsEnabled;

        try
        {
            ThemedLabel.AnimationsEnabled = () => false;

            foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            {
                LanguageTestHelper.RunWithLanguage(language, () =>
                {
                    using ThemedLabel label = Show(UIStyles.Labels.CreateStandard("A text that is much longer than the label it is shown in", scroll: LabelScroll.Endless()));

                    Assert.False(label.GetPrivateField<bool>("_animating"));
                });
            }
        }
        finally
        {
            ThemedLabel.AnimationsEnabled = original;
        }
    }
}
