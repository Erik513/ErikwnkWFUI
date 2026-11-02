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
using TextBox = System.Windows.Forms.TextBox;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>
/// The multi panel: focus, keys, and the tooltip of the arrows.
/// </summary>
[Collection(ErikwnkWFUI.Tests.Infrastructure.AccentColorTestCollection.Name)]
public class MultiPanelInputTests : MultiPanelTestBase
{
    [Trait("Focus", "true")]
    [Fact]
    public void FocusInTheLeftPage_GoesOnToTheFirstControlOfTheNewPage()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        TextBox first = new TextBox();
        TextBox second = new TextBox { TabIndex = 1 };
        TextBox other = new TextBox { TabIndex = 0 };
        panel.Pages[0].Controls.Add(first);
        panel.Pages[1].Controls.Add(second);
        panel.Pages[1].Controls.Add(other);
        Form host = panel.FindForm()!;
        first.Focus();
        Assert.Same(first, host.ActiveControl);

        panel.Next();

        Assert.Same(other, host.ActiveControl);

        // The arrows do not take the focus, so the focus is not moved by them either.
        Assert.All(Arrows(panel), arrow => Assert.False(arrow.CanSelect));
    }

    [Fact]
    public void CtrlPageDownAndCtrlPageUp_SwitchThePages_AndLeaveTheKeyToTheParentAtTheEnds()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        Assert.False(PressKey(panel, Keys.Control | Keys.PageUp));
        Assert.Equal(0, panel.SelectedIndex);

        Assert.True(PressKey(panel, Keys.Control | Keys.PageDown));
        Assert.True(PressKey(panel, Keys.Control | Keys.Tab));
        Assert.Equal(2, panel.SelectedIndex);
        Assert.False(PressKey(panel, Keys.Control | Keys.PageDown));
        Assert.False(PressKey(panel, Keys.Control | Keys.Tab));

        Assert.True(PressKey(panel, Keys.Control | Keys.PageUp));
        Assert.True(PressKey(panel, Keys.Control | Keys.Shift | Keys.Tab));
        Assert.Equal(0, panel.SelectedIndex);

        // Other keys are not taken.
        Assert.False(PressKey(panel, Keys.PageDown));
        Assert.False(PressKey(panel, Keys.Tab));
        Assert.Equal(0, panel.SelectedIndex);
    }

    [Fact]
    public void TheArrows_DoNotTakeTheFocus_SoItDoesNotJumpToAPageWhenAnArrowIsDisabled()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        // A click with the mouse focuses a button that can be selected - and when that one is disabled by the click (the next arrow on
        // the last page), the focus jumped on to the first control of the page. So the arrows cannot be selected.
        Button[] arrows = Arrows(panel);
        Assert.All(arrows, arrow => Assert.False(arrow.CanSelect));
        Assert.All(arrows, arrow => Assert.False(arrow.TabStop));

        Click(arrows[1]);
        Click(arrows[1]);

        Assert.Equal(2, panel.SelectedIndex);
        Assert.False(arrows[1].Enabled);
        Assert.NotSame(arrows[0], panel.FindForm()!.ActiveControl);
    }

    [Fact]
    public void TheTooltipOfAnArrow_IsTheTitleOfThePageItLeadsTo_AndNothingWithoutOne()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        Button[] arrows = Arrows(panel);

        panel.AddPage("First");
        Assert.Equal("", ButtonToolTipReader.Text(arrows[0]));
        Assert.Equal("", ButtonToolTipReader.Text(arrows[1]));

        panel.AddPage("Second");
        panel.AddPage("");
        panel.AddPage("Fourth");

        Assert.Equal("", ButtonToolTipReader.Text(arrows[0]));
        Assert.Equal("Second", ButtonToolTipReader.Text(arrows[1]));

        panel.Next();
        Assert.Equal("First", ButtonToolTipReader.Text(arrows[0]));
        Assert.Equal("", ButtonToolTipReader.Text(arrows[1]));

        panel.Next();
        Assert.Equal("Second", ButtonToolTipReader.Text(arrows[0]));
        Assert.Equal("Fourth", ButtonToolTipReader.Text(arrows[1]));

        panel.Next();
        Assert.Equal("", ButtonToolTipReader.Text(arrows[0]));
        Assert.Equal("", ButtonToolTipReader.Text(arrows[1]));

        // A title that is changed or removed later.
        panel.SelectedIndex = 0;
        panel.Pages[1].Title = "Renamed";
        Assert.Equal("Renamed", ButtonToolTipReader.Text(arrows[1]));
        panel.Pages[1].Title = "";
        Assert.Equal("", ButtonToolTipReader.Text(arrows[1]));

        Assert.Equal(UIStrings.Get("MultiPanel.NextPage"), arrows[1].AccessibleName);
        Assert.Equal(UIStrings.Get("MultiPanel.PreviousPage"), arrows[0].AccessibleName);
    }
}
