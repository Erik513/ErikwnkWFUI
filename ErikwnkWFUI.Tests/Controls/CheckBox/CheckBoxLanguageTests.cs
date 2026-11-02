using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// The checkbox has no text of its own - the label is the caller's - so nothing is to be
/// translated. What a language or culture could still change is how it looks and how it
/// is used: its size, its picture, and the keyboard mnemonic ("&amp;Item" and the letter i
/// is the classic trap under the Turkish culture).
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class CheckBoxLanguageTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();
    private readonly CultureInfo _before = CultureInfo.CurrentUICulture;

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _before;

        foreach (Form host in _hosts)
            host.Dispose();
    }

    private System.Windows.Forms.CheckBox Show(string text)
    {
        System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard(text, false);
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 60),
            BackColor = UIColors.BackgroundMedium
        };

        box.Location = new Point(10, 10);
        host.Controls.Add(box);
        host.Show();
        _hosts.Add(host);

        return box;
    }

    [Fact]
    public void TheSizeThePictureAndTheMnemonic_AreTheSame_InEveryLanguage_AndUnderAnyCulture()
    {
        // Baseline: English, the machine's own culture.
        Size baselineSize;
        Bitmap baselinePicture;

        using (System.Windows.Forms.CheckBox baseline = Show("&Item list"))
        {
            baselineSize = baseline.Size;
            baselinePicture = Draw(baseline);
        }

        using (baselinePicture)
        {
            foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            {
                foreach (string culture in new[] { "de-DE", "tr-TR", "ar-SA" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(culture);

                    LanguageTestHelper.RunWithLanguage(language, () =>
                    {
                        System.Windows.Forms.CheckBox box = Show("&Item list");
                        using Bitmap picture = Draw(box);

                        Assert.Equal(baselineSize, box.Size);
                        Assert.True(SamePixels(baselinePicture, picture), language + " / " + culture + ": the picture differs");

                        // Alt + I presses it, also under the Turkish culture (dotted and dotless i).
                        bool handled = box.InvokePrivate<bool>("ProcessMnemonic", 'i');
                        Assert.True(handled, language + " / " + culture + ": the mnemonic did not work");
                        Assert.True(box.Checked);
                    });
                }
            }
        }
    }
}
