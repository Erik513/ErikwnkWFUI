using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Labels;

/// <summary>The tooltip with the whole text of a label whose text moves because it does not fit.</summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelFullTextToolTipTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private ThemedLabel Create(string text, LabelScroll scroll)
    {
        ThemedLabel label = UIStyles.Labels.CreateStandard(text);
        label.Size = new Size(120, 24);
        label.Scroll = scroll;

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(200, 60)
        };
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);
        Application.DoEvents();

        return label;
    }

    private static string Shown(ThemedLabel label)
    {
        FullTextToolTip tip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!;

        return tip.Text;
    }

    // What the tooltip window of the label really has for it (not only what the label told it).
    private static string Real(ThemedLabel label)
    {
        ToolTip? toolTip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!.GetPrivateField<ToolTip>("_toolTip");

        return toolTip == null ? "" : toolTip.GetToolTip(label);
    }

    [Fact]
    public void AMovingTextThatDoesNotFit_HasATooltipWithAllOfIt()
    {
        foreach (LabelScroll scroll in new[] { LabelScroll.BackAndForth(), LabelScroll.Endless() })
        {
            ThemedLabel label = Create(LongText, scroll);

            Assert.Equal(LongText, Shown(label));
            Assert.Equal(LongText, Real(label));
        }
    }

    [Fact]
    public void ATextThatFits_OrThatDoesNotMove_HasNone()
    {
        Assert.Equal("", Shown(Create("Short", LabelScroll.BackAndForth())));
        Assert.Equal("", Shown(Create("Short", LabelScroll.Endless())));

        // Not moving: a standard label with an ellipsis has a tooltip of its own.
        Assert.Equal("", Shown(Create(LongText, LabelScroll.None)));
    }

    [Fact]
    public void TheTooltip_FollowsTheTextAndTheSize()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());
        Assert.Equal(LongText, Shown(label));

        label.Text = "Short";
        Assert.Equal("", Shown(label));
        Assert.Equal("", Real(label));

        label.Text = LongText + " and more";
        Assert.Equal(LongText + " and more", Shown(label));

        label.Width = 600;
        Application.DoEvents();
        Assert.Equal("", Shown(label));
    }

    [Fact]
    public void ADisabledLabel_HasNone_AndTheTooltipComesBackWhenItIsEnabled()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());

        label.Enabled = false;
        Assert.Equal("", Shown(label));

        label.Enabled = true;
        Assert.Equal(LongText, Shown(label));
    }

    [Fact]
    public void TheTooltip_CanBeSwitchedOff_AndOnAgain()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());
        Assert.True(label.ShowFullTextToolTip);

        label.ShowFullTextToolTip = false;
        Assert.Equal("", Shown(label));
        Assert.Equal("", Real(label));

        label.ShowFullTextToolTip = true;
        Assert.Equal(LongText, Shown(label));
    }

    [Fact]
    public void SeveralTexts_HaveATooltipWithAllOfThem()
    {
        ThemedLabel label = Create("", LabelScroll.Endless(new[] { "First message of the banner", "Second message of the banner", "Third message" }));

        Assert.Equal(label.Text, Shown(label));
        Assert.Contains("Second message", Shown(label));
    }

    [Fact]
    public void ALabelThatIsDisposed_TakesItsTooltipWithIt()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());

        label.Dispose();

        Assert.Equal("", Shown(label));
    }
}
