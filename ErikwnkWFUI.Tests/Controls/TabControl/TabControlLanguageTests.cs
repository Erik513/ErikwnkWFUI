using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ErikwnkWFUI.Localization;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;


/// <summary>
/// Same two checks as the other controls' language tests, with the shared
/// LanguageTestHelper: every key translated, and the menu texts following a
/// language change live.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class TabControlLanguageTests
{
    [Fact]
    public void EveryKey_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[]
        {
            "TabControl.AddTab",
            "TabControl.RenameTab",
            "TabControl.CloseTab",
            "TabControl.NewTabTitle",
            "TabControl.LockedRenameAndClose",
            "TabControl.LockedRename",
            "TabControl.LockedClose",
            "TabControl.ScrollLeft",
            "TabControl.ScrollRight",
            "TabControl.ScrollUp",
            "TabControl.ScrollDown"
        });
    }

    [Fact]
    public void MenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
        string?[] english = menu.Items.Cast<ToolStripItem>().Select(item => item.Text).ToArray();

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.Equal(3, menu.Items.Count);

            for (int i = 0; i < english.Length; i++)
            {
                Assert.NotEqual(english[i], menu.Items[i].Text);
            }
        });
    }

    [Fact]
    public void NewTabTitle_FollowsTheLanguageAtTheTimeOfAdding()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        _ = tabs.Handle;
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            ((ToolStripMenuItem)menu.Items[0]).PerformClick();
        });

        Assert.Equal("Neuer Tab", tabs.TabPages[0].Text);
    }

    [Fact]
    public void MenuItems_HaveTheExpectedWording_InEveryLanguage()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

        string[] Texts() => menu.Items.Cast<ToolStripItem>().Select(item => item.Text ?? "").ToArray();

        Assert.Equal(new[] { "Add tab", "Rename tab", "Close tab" }, Texts());

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.Equal(new[] { "Tab hinzufügen", "Tab umbenennen", "Tab schließen" }, Texts());
        });

        // And back: the texts follow the language in both directions.
        Assert.Equal(new[] { "Add tab", "Rename tab", "Close tab" }, Texts());
    }

    [Fact]
    public void AMenuThatIsBuiltWhileGerman_IsGermanFromTheStart()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false, AllowUserToRenameTabs = false };

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            tabs.AllowUserToAddTabs = true;

            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            Assert.Equal("Tab hinzufügen", menu.Items[0].Text);
        });
    }

    [Fact]
    public void TheToolTipOverALockedTab_FollowsALanguageChangeOnTheNextMouseMove()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("a");
        tabs.TabPages.Add("b");
        tabs.SetTabClosable(tabs.TabPages[1], false);

        tabs.InvokePrivate("UpdateTabToolTip", 1);
        Assert.Equal("This tab cannot be closed", tabs.GetPrivateField<string>("_tabToolTipText"));

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            tabs.InvokePrivate("UpdateTabToolTip", 1);
            Assert.Equal("Dieser Tab kann nicht geschlossen werden", tabs.GetPrivateField<string>("_tabToolTipText"));
        });
    }

    // The language event is static: a control that does not unhook itself
    // stays alive, and keeps being called, for as long as the app runs.
    [Fact]
    public void AControl_UnhooksFromTheLanguageEvent_WhenItIsDisposed()
    {
        System.Reflection.FieldInfo field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        int Listeners() => ((System.Delegate?)field.GetValue(null))?.GetInvocationList().Length ?? 0;
        int before = Listeners();

        WfuiTabControl tabs = new WfuiTabControl();
        int whileAlive = Listeners();
        tabs.Dispose();

        Assert.Equal(before + 1, whileAlive);
        Assert.Equal(before, Listeners());
    }

    [Fact]
    public void ADisposedControl_IgnoresALanguageChange()
    {
        WfuiTabControl tabs = new WfuiTabControl();
        tabs.Dispose();

        Exception? thrown = Record.Exception(() =>
            LanguageTestHelper.RunWithLanguage(UILanguage.German, () => { }));

        Assert.Null(thrown);
    }
}
