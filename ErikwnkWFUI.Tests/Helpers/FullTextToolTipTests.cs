using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The tooltip with the whole text of a control: made only when there is a text, and what it does with the same answer twice.</summary>
public class FullTextToolTipTests
{
    [Fact]
    public void NoTooltipIsMade_WhileThereIsNothingToShow_AndTheSameTextTwiceChangesNothing()
    {
        using Control owner = new Control();
        FullTextToolTip tip = new FullTextToolTip(owner);

        tip.Set("");
        tip.Update("a long text", cut: false);
        Assert.Equal("", tip.Text);
        Assert.Null(tip.GetPrivateField<ToolTip?>("_toolTip"));

        tip.Update("a long text", cut: true);
        ToolTip? made = tip.GetPrivateField<ToolTip?>("_toolTip");
        Assert.Equal("a long text", tip.Text);
        Assert.NotNull(made);

        tip.Set("a long text");
        Assert.Same(made, tip.GetPrivateField<ToolTip?>("_toolTip"));

        tip.Dispose();
        Assert.Equal("", tip.Text);
        Assert.Null(tip.GetPrivateField<ToolTip?>("_toolTip"));
    }

    [Fact]
    public void SwitchingItOff_TakesTheTextAway_AndSwitchingItOffAgainChangesNothing()
    {
        using Control owner = new Control();
        FullTextToolTip tip = new FullTextToolTip(owner);
        tip.Update("a long text", cut: true);

        tip.Enabled = false;
        tip.Enabled = false;
        Assert.Equal("", tip.Text);

        tip.Update("a long text", cut: true);
        Assert.Equal("", tip.Text);

        tip.Enabled = true;
        tip.Update("a long text", cut: true);
        Assert.Equal("a long text", tip.Text);

        tip.Dispose();
    }
}
