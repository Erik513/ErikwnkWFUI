using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>
/// The NumericUpDown has no text of its own: the number is a number, the prefix and the suffix come from the caller.
/// So there is nothing to translate in it. What a language switch can still touch: the prefix and the suffix stay as they
/// were handed over (umlauts, an ampersand, a currency sign), the number and its limits are not changed by it and no
/// ValueChanged is raised, and a control that is shown carries on through a switch. [Collection]: the tests switch the
/// language.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class NumericUpDownLanguageTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiNumericUpDown Show(WfuiNumericUpDown box)
    {
        box.Location = new Point(10, 10);
        box.Width = 160;
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(190, 60) };
        form.Controls.Add(box);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return box;
    }

    [Fact]
    public void ThePrefixAndTheSuffix_StayAsTheyWereHandedOver_WhenTheLanguageChanges()
    {
        WfuiNumericUpDown box = null!;
        string cancelEnglish = "";

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            cancelEnglish = UIStrings.Get("MessageBox.Cancel");
            box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(0, 100, 5, 40);
            box.Prefix = cancelEnglish + " ";
            box.Suffix = " Prozent";
        });

        using (box)
        {
            LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
            {
                Assert.Equal(cancelEnglish + " ", box.Prefix);
                Assert.Equal(" Prozent", box.Suffix);
                Assert.Equal(cancelEnglish + " 40 Prozent", box.Text);
                Assert.NotEqual(UIStrings.Get("MessageBox.Cancel") + " ", box.Prefix);
            });
        }
    }

    [Fact]
    public void EveryLanguage_TakesPrefixAndSuffixAsTheyAre_AlsoWhenReadingBack()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string suffix = " für " + language + " & Söhne ÄÖÜ ß";

                using WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(0, 1000, 1, 12);
                box.Prefix = "€ ";
                box.Suffix = suffix;
                Assert.Equal("€ 12" + suffix, box.Text);

                box.Text = "€ 34" + suffix;
                box.InvokePrivate("ValidateEditText");

                Assert.Equal(34, box.Value);
                Assert.Equal("€ 34" + suffix, box.Text);
            });
        }
    }

    [Fact]
    public void ALanguageSwitch_ChangesNeitherTheNumberNorTheLimits_AndRaisesNoEvent()
    {
        WfuiNumericUpDown box = Show((WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(-5, 55, 5, 25));
        box.Suffix = " m";
        int raised = 0;
        box.ValueChanged += (sender, e) => raised++;

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Application.DoEvents();

                Assert.Equal(25, box.Value);
                Assert.Equal(-5, box.Minimum);
                Assert.Equal(55, box.Maximum);
                Assert.Equal("25 m", box.Text);
            });
        }

        Assert.Equal(0, raised);
    }

    [Fact]
    public void TheNumberFormat_IsTheOneOfTheCulture_NotOfTheLibraryLanguage()
    {
        // The library language is for the texts of the library; the digits follow the culture of the application, as in
        // the standard control.
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                using WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(0, 100000, 1, 1234);
                box.ThousandsSeparator = true;
                box.DecimalPlaces = 1;

                Assert.Equal(1234m.ToString("N1"), box.Text);
            });
        }
    }

    [Fact]
    public void TheNumericUpDown_HasNoKeyOfItsOwn_InTheTranslations()
    {
        // The library's own texts live in the two tables of UIStrings, keyed by control name; none belongs to this control.
        foreach (string table in new[] { "English", "German" })
        {
            FieldInfo field = typeof(UIStrings).GetField(table, BindingFlags.NonPublic | BindingFlags.Static)!;
            Dictionary<string, string> keys = (Dictionary<string, string>)field.GetValue(null)!;

            Assert.NotEmpty(keys);
            Assert.DoesNotContain(keys.Keys, key => key.StartsWith("NumericUpDown."));
        }
    }

    [Fact]
    public void TheControlHasNoToolTipOrAccessibleText_ThatWouldNeedATranslation()
    {
        using WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard();

        Assert.True(string.IsNullOrEmpty(box.AccessibleName));
        Assert.True(string.IsNullOrEmpty(box.AccessibleDescription));
    }
}
