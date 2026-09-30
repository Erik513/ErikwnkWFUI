using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The padlock on tabs that were locked against renaming or closing: room
/// reserved in front of the name (through a transparent slot image, since the
/// native control sizes a tab from its text and image only), the padlock
/// itself, the tooltip saying what is locked, and staying out of the way of
/// an image list the application brings.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class TabControlLockIconTests
{
    private static WfuiTabControl CreateTabs()
    {
        WfuiTabControl tabs = new WfuiTabControl { Size = new Size(400, 200) };
        tabs.TabPages.Add("Home");
        tabs.TabPages.Add("Reports");
        tabs.TabPages.Add("Notes");
        _ = tabs.Handle;
        return tabs;
    }

    [Fact]
    public void TheLockIcon_IsOnByDefault_OnlyOnTheEditableControl()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.True(tabs.ShowLockIcon);
        Assert.Null(typeof(ErikwnkWFUI.Controls.ReadOnlyTabControl).GetProperty("ShowLockIcon"));
    }

    // ---- room for it ----

    [Fact]
    public void ALockedTab_IsWiderThanAnUnlockedOne_WithTheSameName()
    {
        using WfuiTabControl plain = CreateTabs();
        using WfuiTabControl locked = CreateTabs();
        locked.SetTabClosable(locked.TabPages[0], false);

        Assert.True(locked.GetTabRect(0).Width > plain.GetTabRect(0).Width);
        Assert.Equal(plain.GetTabRect(2).Width, locked.GetTabRect(2).Width);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void EitherLock_ReservesTheRoom(bool renameLocked, bool closeLocked)
    {
        using WfuiTabControl tabs = CreateTabs();

        tabs.SetTabRenamable(tabs.TabPages[1], !renameLocked);
        tabs.SetTabClosable(tabs.TabPages[1], !closeLocked);

        Assert.NotNull(tabs.ImageList);
        Assert.Equal(0, tabs.TabPages[1].ImageIndex);
        Assert.Equal(-1, tabs.TabPages[0].ImageIndex);
    }

    [Fact]
    public void UnlockingTheLastLockedTab_GivesTheRoomBack()
    {
        using WfuiTabControl tabs = CreateTabs();
        int normalWidth = tabs.GetTabRect(0).Width;
        tabs.SetTabRenamable(tabs.TabPages[0], false);

        tabs.SetTabRenamable(tabs.TabPages[0], true);

        Assert.Null(tabs.ImageList);
        Assert.Equal(-1, tabs.TabPages[0].ImageIndex);
        Assert.Equal(normalWidth, tabs.GetTabRect(0).Width);
    }

    [Fact]
    public void UnlockingOneOfTwo_KeepsTheOtherLockedTabsRoom()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabRenamable(tabs.TabPages[0], false);
        tabs.SetTabClosable(tabs.TabPages[1], false);

        tabs.SetTabRenamable(tabs.TabPages[0], true);

        Assert.NotNull(tabs.ImageList);
        Assert.Equal(-1, tabs.TabPages[0].ImageIndex);
        Assert.Equal(0, tabs.TabPages[1].ImageIndex);
    }

    [Fact]
    public void ALockSetBeforeThePageIsAdded_StillGetsItsRoom()
    {
        using WfuiTabControl tabs = CreateTabs();
        TabPage page = new TabPage("Late");
        tabs.SetTabRenamable(page, false);

        tabs.TabPages.Add(page);

        Assert.Equal(0, page.ImageIndex);
        Assert.NotNull(tabs.ImageList);
    }

    [Fact]
    public void SwitchingTheIconOff_TakesTheRoomAway_AndBackOnPutsItBack()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[0], false);

        tabs.ShowLockIcon = false;
        Assert.Null(tabs.ImageList);
        Assert.Equal(-1, tabs.TabPages[0].ImageIndex);

        tabs.ShowLockIcon = true;
        Assert.NotNull(tabs.ImageList);
        Assert.Equal(0, tabs.TabPages[0].ImageIndex);
    }

    [Fact]
    public void TheLockItself_IsKept_WhenTheIconIsOff()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.ShowLockIcon = false;

        tabs.SetTabClosable(tabs.TabPages[0], false);

        Assert.False(tabs.IsTabClosable(tabs.TabPages[0]));
        Assert.Null(tabs.ImageList);
    }

    [Fact]
    public void TheControlWideSwitches_DoNotShowAPadlock()
    {
        using WfuiTabControl tabs = CreateTabs();

        tabs.AllowUserToCloseTabs = false;
        tabs.AllowUserToRenameTabs = false;

        Assert.Null(tabs.ImageList);
    }

    // ---- an image list of the application's own ----

    [Fact]
    public void AnApplicationsImageList_IsLeftAlone()
    {
        using WfuiTabControl tabs = CreateTabs();
        using ImageList own = new ImageList();
        using Bitmap icon = new Bitmap(16, 16);
        own.Images.Add(icon);
        own.Images.Add(icon);
        tabs.ImageList = own;
        tabs.TabPages[0].ImageIndex = 1;

        tabs.SetTabClosable(tabs.TabPages[0], false);
        tabs.SetTabRenamable(tabs.TabPages[1], false);

        Assert.Same(own, tabs.ImageList);
        Assert.Equal(1, tabs.TabPages[0].ImageIndex);
        Assert.Equal(-1, tabs.TabPages[1].ImageIndex);
    }

    // ---- the padlock ----

    [Fact]
    public void DrawPadlock_PaintsInTheGivenColor_AtTheLeftEndOfItsSlot()
    {
        using Bitmap bitmap = new Bitmap(30, 30);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
            typeof(WfuiTabControl).GetMethod(
                "DrawPadlock",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { graphics, new Rectangle(9, 8, 12, 14), Color.White });
        }

        int painted = 0;
        int outside = 0;

        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                if (bitmap.GetPixel(x, y).R > 40)
                {
                    painted++;

                    if (x < 7 || x >= 21 || y < 6 || y >= 22)
                    {
                        outside++;
                    }
                }
            }
        }

        Assert.True(painted > 12);
        Assert.Equal(0, outside);
    }

    [Fact]
    public void ALockedTabsRender_ShowsPaintedPixelsWhereTheUnlockedOneHasNone()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabBackColor = Color.FromArgb(40, 41, 42);
        tabs.SetTabClosable(tabs.TabPages[1], false);
        Rectangle locked = tabs.GetTabRect(1);
        Rectangle plain = tabs.GetTabRect(2);

        using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));

        // The first few pixels inside a tab: the padlock's place on the
        // locked one, just padding on the other.
        Assert.True(CountBright(bitmap, locked.Left + 3, locked.Top + 4, 14, locked.Height - 8) > 5);
        Assert.Equal(0, CountBright(bitmap, plain.Left + 2, plain.Top + 4, 4, plain.Height - 8));
    }

    private static int CountBright(Bitmap bitmap, int x, int y, int width, int height)
    {
        int count = 0;

        for (int i = x; i < x + width; i++)
        {
            for (int j = y; j < y + height; j++)
            {
                Color pixel = bitmap.GetPixel(i, j);

                if (pixel.R > 120 && pixel.G > 120 && pixel.B > 120)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // ---- renaming a tab that carries the padlock ----

    [Fact]
    public void TheRenameBox_StartsAfterTheSlot_OnATabThatIsOnlyCloseLocked()
    {
        StaThread.Run(() =>
        {
            using Form host = new Form
            {
                ClientSize = new Size(400, 200),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000),
                ShowInTaskbar = false
            };
            WfuiTabControl tabs = new WfuiTabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add("Home");
            tabs.TabPages.Add("Reports");
            host.Controls.Add(tabs);
            host.Show();
            Application.DoEvents();
            tabs.SetTabClosable(tabs.TabPages[1], false);
            Application.DoEvents();

            tabs.BeginRenameTab(1);
            Application.DoEvents();

            TextBox box = tabs.GetPrivateField<TextBox>("_renameBox")!;
            Rectangle tab = tabs.GetTabRect(1);
            Assert.True(box.Left >= tab.Left + 4 + 7);
        });
    }

    // ---- the tooltip ----

    [Theory]
    [InlineData(true, true, "This tab can't be renamed or closed")]
    [InlineData(true, false, "This tab can't be renamed")]
    [InlineData(false, true, "This tab can't be closed")]
    [InlineData(false, false, "")]
    public void TheToolTip_SaysWhatIsLocked(bool renameLocked, bool closeLocked, string expected)
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabRenamable(tabs.TabPages[1], !renameLocked);
        tabs.SetTabClosable(tabs.TabPages[1], !closeLocked);

        Assert.Equal(expected, tabs.InvokePrivate<string>("GetLockedToolTipText", 1));
    }

    [Fact]
    public void TheToolTip_IsEmpty_ForNoTab_AndWhenTheIconIsOff()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[1], false);

        Assert.Equal("", tabs.InvokePrivate<string>("GetLockedToolTipText", -1));
        Assert.Equal("", tabs.InvokePrivate<string>("GetLockedToolTipText", 9));

        tabs.ShowLockIcon = false;
        Assert.Equal("", tabs.InvokePrivate<string>("GetLockedToolTipText", 1));
    }

    [Fact]
    public void EveryKey_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[]
        {
            "TabControl.LockedRenameAndClose",
            "TabControl.LockedRename",
            "TabControl.LockedClose"
        });
    }

    [Fact]
    public void TheToolTipText_FollowsTheLanguage()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[1], false);
        string? english = tabs.InvokePrivate<string>("GetLockedToolTipText", 1);

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.NotEqual(english, tabs.InvokePrivate<string>("GetLockedToolTipText", 1));
        });
    }

    // ---- cleaning up ----

    [Fact]
    public void RemovingTheLockedTab_GivesTheRoomBack_WhenNoneIsLeft()
    {
        using WfuiTabControl tabs = CreateTabs();
        TabPage locked = tabs.TabPages[1];
        tabs.SetTabClosable(locked, false);

        tabs.TabPages.Remove(locked);

        Assert.Null(tabs.ImageList);
    }

    [Fact]
    public void DisposingTheControl_ReleasesTheSlotImages()
    {
        WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[0], false);
        ImageList slots = tabs.ImageList!;
        bool released = false;
        slots.Disposed += (s, e) => released = true;

        tabs.Dispose();

        Assert.True(released);
    }
}
