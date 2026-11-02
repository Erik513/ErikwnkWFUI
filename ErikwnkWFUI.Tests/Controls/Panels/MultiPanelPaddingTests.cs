using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>A multi panel that is given no padding has one, so that its title and its arrows are not at the edge; a padding that is given is kept.</summary>
public class MultiPanelPaddingTests
{
    public static IEnumerable<object[]> Panels()
    {
        yield return new object[] { "CreateStandard", (Func<MultiPanel>)(() => UIStyles.MultiPanels.CreateStandard()) };
        yield return new object[] { "CreatePrimary", (Func<MultiPanel>)(() => UIStyles.MultiPanels.CreatePrimary()) };
        yield return new object[] { "CreateCardStandard", (Func<MultiPanel>)(() => UIStyles.MultiPanels.CreateCardStandard()) };
        yield return new object[] { "CreateCardPrimary", (Func<MultiPanel>)(() => UIStyles.MultiPanels.CreateCardPrimary()) };
        yield return new object[] { "CreateStandard(colors)", (Func<MultiPanel>)(() => UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Yellow)) };
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void WithoutAPaddingOfItsOwn_EveryVariantHasEightPixelsAllAround_TheHeaderIsInsideThem(string name, Func<MultiPanel> make)
    {
        using MultiPanel panel = make();
        panel.AddPage("Title");
        panel.AddPage("Other");
        panel.SetBounds(0, 0, 400, 150);
        panel.PerformLayout();

        Assert.Equal(new Padding(8), panel.Padding);

        // The arrows end at the right padding and the pages start at the left one.
        Button next = panel.GetPrivateField<Button>("_nextButton")!;
        Assert.Equal(panel.ClientSize.Width - 8, next.Right);
        Assert.Equal(8, panel.Pages[0].Left);
    }

    [Fact]
    public void APaddingThatIsGiven_IsKept_AlsoNone()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.AddPage("Title");
        panel.SetBounds(0, 0, 400, 150);

        panel.Padding = new Padding(0);
        panel.PerformLayout();

        Assert.Equal(new Padding(0), panel.Padding);
        Assert.Equal(panel.ClientSize.Width, panel.GetPrivateField<Button>("_nextButton")!.Right);

        panel.Padding = new Padding(20, 10, 20, 0);
        panel.PerformLayout();

        Assert.Equal(panel.ClientSize.Width - 20, panel.GetPrivateField<Button>("_nextButton")!.Right);
    }
}