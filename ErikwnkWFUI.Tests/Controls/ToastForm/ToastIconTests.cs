using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The symbol of a toast: none by default, a check mark, info, warning or error on request.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastIconTests : ToastTestBase
{
    private static Label IconLabel() => Current()!.GetPrivateField<Label>("_iconLabel")!;

    private static Label MessageLabel() => Current()!.GetPrivateField<Label>("_messageLabel")!;

    [Theory]
    [InlineData(ToastIcon.Success)]
    [InlineData(ToastIcon.Info)]
    [InlineData(ToastIcon.Warning)]
    [InlineData(ToastIcon.Error)]
    public void TheFontOfTheSymbol_IsOneThatCanStillBeUsed_ForEveryIcon(ToastIcon icon)
    {
        ToastForm.ShowToast("Message", Host(), icon: icon);

        // A font that was given back throws when it is used - and the label paints a red cross instead of the symbol.
        Assert.Null(Record.Exception(() => IconLabel().Font.Height));
    }

    [Fact]
    public void TheSameSymbolFontTwice_IsOneFont_NotAnEqualOneThatTheLabelWouldNotTake()
    {
        ToastForm.ShowToast("Message", Host(), icon: ToastIcon.Info);
        ToastForm toast = Current()!;
        Font first = toast.GetPrivateField<Font>("_iconFont")!;

        toast.CustomSymbol = ErikwnkWFUI.UIStyles.Symbols.Info;

        Assert.Same(first, toast.GetPrivateField<Font>("_iconFont"));
        Assert.Null(Record.Exception(() => IconLabel().Font.Height));
    }

    [Fact]
    public void WithoutAnIcon_NoSymbolIsShown_AndTheTextUsesTheWidth()
    {
        ToastForm.ShowToast("Plain", Host());

        Assert.Equal(ToastIcon.None, Current()!.MessageIcon);
        Assert.False(IconLabel().Visible);
        Assert.Equal("", IconLabel().Text);
        Assert.Equal(0, MessageLabel().Padding.Left);
    }

    [Theory]
    [InlineData(ToastIcon.Success)]
    [InlineData(ToastIcon.Info)]
    [InlineData(ToastIcon.Warning)]
    [InlineData(ToastIcon.Error)]
    public void AnIcon_ShowsItsSymbol_AndMakesRoomForIt(ToastIcon icon)
    {
        ToastForm.ShowToast("With a symbol", Host(), icon: icon);

        Assert.Equal(icon, Current()!.MessageIcon);
        Assert.True(IconLabel().Visible);
        Assert.False(string.IsNullOrEmpty(IconLabel().Text));
        Assert.True(MessageLabel().Padding.Left > 0);
        Assert.True(IconLabel().Right <= MessageLabel().Left + MessageLabel().Padding.Left, "the symbol overlaps the text");
    }

    [Fact]
    public void TheSymbol_IsInFrontOfTheTexts_SoTheirBackgroundDoesNotCoverIt()
    {
        ToastForm.ShowToast("Front", Host(), icon: ToastIcon.Success);

        Control parent = IconLabel().Parent!;

        Assert.True(parent.Controls.GetChildIndex(IconLabel()) < parent.Controls.GetChildIndex(MessageLabel()));
        Assert.True(parent.Controls.GetChildIndex(IconLabel()) < parent.Controls.GetChildIndex(Current()!.GetPrivateField<Label>("_titleLabel")!));
    }

    [Fact]
    public void TheFourSymbols_AreDifferent()
    {
        Form host = Host();
        List<string> glyphs = new List<string>();

        foreach (ToastIcon icon in new[] { ToastIcon.Success, ToastIcon.Info, ToastIcon.Warning, ToastIcon.Error })
        {
            ToastForm.ShowToast("x", host, icon: icon);
            glyphs.Add(IconLabel().Text);
        }

        Assert.Equal(4, glyphs.Distinct().Count());
    }

    [Fact]
    public void TheSymbolColor_FollowsTheTextColor()
    {
        ToastForm.ShowToast("Colored", Host(), ErikwnkWFUI.UIStyles.Colors.Sets.Yellow, icon: ToastIcon.Warning);

        Assert.Equal(PopupSupport.GetTextColor(ErikwnkWFUI.UIStyles.Colors.Sets.Yellow), IconLabel().ForeColor);
    }

    [Fact]
    public void ChangingTheSymbol_OfAToastThatIsUp_ShowsTheNewOne_AndHidesItForNone()
    {
        ToastForm.ShowToast("Up", Host(), icon: ToastIcon.Error);
        ToastForm toast = Current()!;
        string error = IconLabel().Text;

        toast.MessageIcon = ToastIcon.Success;
        Assert.NotEqual(error, IconLabel().Text);

        toast.MessageIcon = ToastIcon.None;
        Assert.False(IconLabel().Visible);
        Assert.Equal(0, MessageLabel().Padding.Left);
    }

    [Fact]
    public void ASymbolOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.MessageIcon = ToastIcon.Warning));
    }

}
