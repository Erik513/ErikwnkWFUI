using System.Collections.Generic;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Same two checks as DataGridViewLanguageTests, using the same shared
/// LanguageTestHelper - see its own remarks on why both matter (a key can
/// be translated but still shown stale if nothing re-reads it after
/// Language changes).
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ListViewLanguageTests
{
    [Fact]
    public void EveryKey_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[]
        {
            "ListView.CopySelection",
            "ListView.CopyAll",
            "ListView.CopySelectionWithHeader",
            "ListView.CopyAllWithHeader",
            "ListView.SelectAll",
            "ListView.RowCopied",
            "ListView.RowsCopied",
            "ListView.WithHeaderSuffix"
        });
    }

    [Fact]
    public void ContextMenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        using WfuiListView listView = new WfuiListView();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)listView.ContextMenuStrip!;

        // Collected recursively so this doesn't need to know the menu's
        // shape by hand.
        List<(ToolStripItem Item, string EnglishText)> textItems = CollectTextItems(menu.Items);

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            foreach ((ToolStripItem item, string englishText) in textItems)
            {
                Assert.NotEqual(englishText, item.Text);
            }
        });
    }

    private static List<(ToolStripItem, string)> CollectTextItems(ToolStripItemCollection items)
    {
        List<(ToolStripItem, string)> result = new List<(ToolStripItem, string)>();

        foreach (ToolStripItem item in items)
        {
            if (!string.IsNullOrEmpty(item.Text))
            {
                result.Add((item, item.Text));
            }

            if (item is ToolStripDropDownItem dropDown)
            {
                result.AddRange(CollectTextItems(dropDown.DropDownItems));
            }
        }

        return result;
    }
}
