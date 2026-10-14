using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>Any symbol of the library instead of the four kinds of message.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastCustomSymbolTests : ToastTestBase
{
    private static Label IconLabel() => Current()!.GetPrivateField<Label>("_iconLabel")!;

    [Fact]
    public void ACustomSymbol_IsShown_WithoutAnIcon()
    {
        ToastForm.ShowToast("Copied", Host(), symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);

        Assert.True(IconLabel().Visible);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, IconLabel().Text);
        Assert.True(Current()!.GetPrivateField<Panel>("_contentPanel")!.Padding.Left >= 45, "no room for the symbol");
    }

    [Fact]
    public void ACustomSymbol_WinsOverTheIcon()
    {
        ToastForm.ShowToast("Copied", Host(), icon: ToastIcon.Error, symbol: ErikwnkWFUI.UIStyles.Symbols.Delete);

        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Delete, IconLabel().Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoCustomSymbol_LeavesTheIconInForce(string? symbol)
    {
        ToastForm.ShowToast("Plain", Host(), icon: ToastIcon.Warning, symbol: symbol);

        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Warning, IconLabel().Text);
    }

    [Fact]
    public void ACustomSymbolInTheIconFont_GetsThatFont()
    {
        ToastForm.ShowToast("Info", Host(), symbol: ErikwnkWFUI.UIStyles.Symbols.Info);

        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.GetFontName(ErikwnkWFUI.UIStyles.Symbols.Info), IconLabel().Font.Name);
    }

    [Fact]
    public void RemovingTheCustomSymbol_OfAToastThatIsUp_ShowsTheIconAgain_OrNone()
    {
        ToastForm.ShowToast("Up", Host(), icon: ToastIcon.Success, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);
        ToastForm toast = Current()!;

        toast.CustomSymbol = null;
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Check, IconLabel().Text);

        toast.MessageIcon = ToastIcon.None;
        Assert.False(IconLabel().Visible);
        Assert.True(toast.GetPrivateField<Panel>("_contentPanel")!.Padding.Left < 45, "room for a symbol that is not there");
    }

    [Fact]
    public void ACustomSymbolOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.CustomSymbol = ErikwnkWFUI.UIStyles.Symbols.Copy));
    }

}
