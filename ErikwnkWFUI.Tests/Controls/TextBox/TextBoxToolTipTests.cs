using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The tooltip with the whole text of a box that is too narrow for it.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxToolTipTests : IDisposable
{
    private const string LongText = "A text that is much wider than this narrow box shows itself in a tooltip";

    private readonly TextBoxHost _host = new TextBoxHost();

    public void Dispose()
    {
        _host.Dispose();
    }

    private static string ToolTipOf(TextBox box)
    {
        return box.GetPrivateField<ToolTip>("_toolTip")!.GetToolTip(box) ?? string.Empty;
    }

    [Fact]
    public void ATextThatWasGivenBeforeTheBoxWasShown_HasItsToolTip_WhenItIsCut()
    {
        // (The text is there before the window is: this is how a box is made in a table of a program.)
        TextBox cut = _host.Show(UIStyles.TextBoxes.CreateStandard(LongText), 150);
        TextBox fits = _host.Show(UIStyles.TextBoxes.CreateStandard("short"), 150);
        TextBox off = UIStyles.TextBoxes.CreateStandard(LongText);
        ((TextField)off).ToolTipWhenCut = false;
        _host.Show(off, 150);

        Assert.Equal(LongText, ToolTipOf(cut));
        Assert.Equal(string.Empty, ToolTipOf(fits));
        Assert.Equal(string.Empty, ToolTipOf(off));
    }

    [Fact]
    public void WhenTheMouseComesAfterTheBoxWasMadeNarrowerOrWider_TheToolTipIsRight()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard(LongText), 700);
        Assert.Equal(string.Empty, ToolTipOf(box));

        box.Width = 150;
        box.InvokePrivate("OnMouseEnter", EventArgs.Empty);

        Assert.Equal(LongText, ToolTipOf(box));

        box.Width = 700;
        box.InvokePrivate("OnMouseEnter", EventArgs.Empty);

        Assert.Equal(string.Empty, ToolTipOf(box));
    }

    [Fact]
    public void ThePasswordThatIsHidden_HasNoToolTip_AndTheOneThatIsShownHasIt()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreatePassword(LongText, ""), 150);

        Assert.Equal(string.Empty, ToolTipOf(box));
    }
}
