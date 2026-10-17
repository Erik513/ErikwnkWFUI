using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The progress text of the update dialog writes the percentage as the progress bars do. [Collection]: the language is static state of the process.</summary>
[Collection(LanguageTestCollection.Name)]
public class UpdateAvailableTextTests
{
    [Fact]
    public void TheDownloadText_EndsWithThePercentage_AsTheProgressBarsWriteIt_InEveryLanguage()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string text = string.Format(UIStrings.Get("UpdateAvailable.Downloading"), 42);

                Assert.EndsWith(ProgressBarPainting.GetPercentText(0, 100, 42), text);
            });
        }
    }
}
