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
        return ((TextField)box).ToolTipTextNow;
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

    // ---- the tooltip of the cut text is there before the mouse is: its text is always the text, Popup decides

    private static ToolTip CutToolTip(TextBox box)
    {
        return box.GetPrivateField<ToolTip>("_cutToolTip")!;
    }

    private static bool PopupIsCancelled(TextBox box)
    {
        PopupEventArgs e = new PopupEventArgs(box, box, false, new Size(100, 20));
        ((TextField)box).RaiseCutToolTipPopup(e);

        return e.Cancel;
    }

    [Fact]
    public void TheTextOfTheTooltip_IsAlwaysTheTextOfTheBox_ThePopupDecidesWhetherItShows()
    {
        TextBox cut = _host.Show(UIStyles.TextBoxes.CreateStandard(LongText), 150);
        TextBox fits = _host.Show(UIStyles.TextBoxes.CreateStandard("short"), 150);

        Assert.Equal(LongText, CutToolTip(cut).GetToolTip(cut));
        Assert.Equal("short", CutToolTip(fits).GetToolTip(fits));
        Assert.False(PopupIsCancelled(cut), "a cut text pops up");
        Assert.True(PopupIsCancelled(fits), "a text that fits does not");
    }

    [Fact]
    public void TheTextOfTheTooltip_FollowsTheTextOfTheBox_AndIsNoneWhereItIsNeverShown()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("one"), 150);
        box.Text = LongText;
        Assert.Equal(LongText, CutToolTip(box).GetToolTip(box));

        ((TextField)box).ToolTipWhenCut = false;
        Assert.Equal(string.Empty, CutToolTip(box).GetToolTip(box));

        TextBox password = _host.Show(UIStyles.TextBoxes.CreatePassword(LongText, ""), 150);
        Assert.Equal(string.Empty, CutToolTip(password).GetToolTip(password));
    }

    [Fact]
    public void OverTheButtonInTheBox_TheTooltipOfTheCutTextDoesNotPopUp_ThatOfTheButtonDoes()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateLockable(LongText), 150);
        System.Reflection.FieldInfo hot = typeof(TextField).GetField("_innerHot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        Assert.False(PopupIsCancelled(box), "over the text");

        hot.SetValue(box, true);
        Assert.True(PopupIsCancelled(box), "over the pencil: the tooltip of the pencil speaks");
        Assert.Equal(box.GetPrivateField<string>("_innerToolTip"), ((TextField)box).ToolTipTextNow);

        hot.SetValue(box, false);
        Assert.False(PopupIsCancelled(box));
    }
}