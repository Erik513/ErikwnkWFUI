using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The progress text of the update download is only the text: the percentage stands on the bar of the progress dialog. [Collection]: the language is static state of the process.</summary>
[Collection(LanguageTestCollection.Name)]
public class UpdateAvailableTextTests
{
    [Fact]
    public void TheDownloadText_HasNoPercentage_InEveryLanguage_BecauseTheBarWritesIt()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string text = UIStrings.Get("UpdateAvailable.Downloading");

                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("{0}", text);
                Assert.DoesNotContain("%", text);
            });
        }
    }
}
