using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Xunit;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>
/// The multi panel: the pages, the arrows and the page count.
/// </summary>
[Collection(ErikwnkWFUI.Tests.Infrastructure.AccentColorTestCollection.Name)]
public class MultiPanelTests : MultiPanelTestBase
{
    [Fact]
    public void TheFirstPageAdded_IsShown_TheOthersAreHidden()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        Assert.Equal(3, panel.PageCount);
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Same(panel.Pages[0], panel.SelectedPage);
        Assert.True(panel.Pages[0].Visible);
        Assert.False(panel.Pages[1].Visible);
        Assert.False(panel.Pages[2].Visible);
    }

    [Fact]
    public void ThePages_FillTheAreaBelowTheHeader_InsideThePadding()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateCardStandard(), 2);

        Assert.Equal(panel.DisplayRectangle, panel.Pages[0].Bounds);
        Assert.True(panel.DisplayRectangle.Top > panel.Padding.Top);
        Assert.Equal(panel.Height - panel.Padding.Bottom, panel.DisplayRectangle.Bottom);
    }

    [Fact]
    public void NextAndPrevious_SwitchThePages_AndStopAtTheEnds_AndRaiseTheEventOnce()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        panel.Previous();
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Equal(0, raised);

        panel.Next();
        panel.Next();
        panel.Next();

        Assert.Equal(2, panel.SelectedIndex);
        Assert.Equal(2, raised);
        Assert.True(panel.Pages[2].Visible);
        Assert.False(panel.Pages[0].Visible);

        panel.SelectedIndex = 2;
        Assert.Equal(2, raised);
    }

    [Fact]
    public void TheArrowButtons_SwitchThePages_AndAreDisabledAtTheEnds()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        Button[] arrows = Arrows(panel);

        Assert.Equal(2, arrows.Length);
        Assert.False(arrows[0].Enabled);
        Assert.True(arrows[1].Enabled);

        Click(arrows[1]);
        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(arrows[0].Enabled);
        Assert.True(arrows[1].Enabled);

        Click(arrows[1]);
        Assert.False(arrows[1].Enabled);

        Click(arrows[0]);
        Assert.Equal(1, panel.SelectedIndex);
    }

    [Fact]
    public void WithoutPagesOrWithOne_BothArrowsAreDisabled_AndTheIndexIsMinusOneUntilThereIsAPage()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        using (panel)
        {
            Assert.Equal(-1, panel.SelectedIndex);
            Assert.Null(panel.SelectedPage);
            Assert.All(Arrows(panel), button => Assert.False(button.Enabled));

            panel.AddPage("Only");

            Assert.Equal(0, panel.SelectedIndex);
            Assert.All(Arrows(panel), button => Assert.False(button.Enabled));
        }
    }

    [Fact]
    public void ASelectedIndexOutOfRange_Throws()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.AddPage("A");
        panel.AddPage("B");

        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = -2);
    }

    [Fact]
    public void RemovingPages_KeepsTheShownPage_OrShowsTheOneThatTookItsPlace()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        MultiPanelPage a = panel.AddPage("A");
        MultiPanelPage b = panel.AddPage("B");
        MultiPanelPage c = panel.AddPage("C");
        panel.SelectedIndex = 1;
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        Assert.True(panel.RemovePage(a));
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Same(b, panel.SelectedPage);
        Assert.Equal(0, raised);

        Assert.True(panel.RemovePage(b));
        Assert.Same(c, panel.SelectedPage);
        Assert.True(c.Visible);
        Assert.Equal(1, raised);

        Assert.True(panel.RemovePage(c));
        Assert.Equal(-1, panel.SelectedIndex);
        Assert.False(panel.RemovePage(c));
        Assert.Equal(2, raised);
        Assert.False(a.IsDisposed);
    }

    [Fact]
    public void APageThatIsDisposedByItsOwner_LeavesTheMultiPanel_AndTheNextOneIsShown()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;
        MultiPanelPage first = panel.Pages[0];
        MultiPanelPage second = panel.Pages[1];

        first.Dispose();

        Assert.Equal(2, panel.PageCount);
        Assert.Same(second, panel.SelectedPage);
        Assert.True(second.Visible);
        Assert.Equal(1, raised);

        // A page that is not shown: nothing else changes.
        panel.Pages[1].Dispose();
        panel.Pages[0].Dispose();
        Assert.Equal(0, panel.PageCount);
        Assert.Equal(-1, panel.SelectedIndex);
        Assert.All(Arrows(panel), button => Assert.False(button.Enabled));
    }

    [Fact]
    public void APageThatIsPutOnAnotherControl_LeavesTheMultiPanel_AndIsShownThere()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        using Panel other = new Panel();
        MultiPanelPage shown = panel.Pages[0];
        MultiPanelPage hidden = panel.Pages[2];

        other.Controls.Add(hidden);

        Assert.Equal(2, panel.PageCount);
        Assert.Equal(0, panel.SelectedIndex);
        Assert.True(hidden.Visible);

        other.Controls.Add(shown);

        Assert.Equal(1, panel.PageCount);
        Assert.Equal("Page 2", panel.SelectedPage.Title);
        Assert.True(panel.SelectedPage.Visible);
    }

    [Fact]
    public void ARemovedPage_DoesNotKeepTheMultiPanelAlive()
    {
        (WeakReference panel, MultiPanelPage page) = MakeAndRemove();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(panel.IsAlive);
        GC.KeepAlive(page);
        page.Dispose();
    }

    [Fact]
    public void DisposingTheMultiPanel_DoesNotRaiseTheEvent_AndDoesNotThrow()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        panel.Dispose();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void AControlAddedDirectlyToTheMultiPanel_IsRefused()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        using Label label = new Label();

        Assert.Throws<InvalidOperationException>(() => panel.Controls.Add(label));
        Assert.DoesNotContain(label, panel.Controls.Cast<Control>());

        panel.AddPage("A").Controls.Add(label);
        Assert.Same(panel.Pages[0], label.Parent);
    }

    [Fact]
    public void APageCannotBeMadeOutsideTheMultiPanel_ItComesFromAddPage()
    {
        Assert.Empty(typeof(MultiPanelPage).GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
    }
}
