using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>Any symbol of the library instead of the four kinds of message.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastCustomSymbolTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastCustomSymbolTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();
        _host?.Dispose();
        ((System.Collections.IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
    }

    private Form Host()
    {
        _host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(100, 100), ShowInTaskbar = false, ClientSize = new Size(600, 400) };
        _host.Show();

        return _host;
    }

    private static ToastForm? Current()
    {
        return (ToastForm?)typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
    }

    private static Label IconLabel() => Current()!.GetPrivateField<Label>("_iconLabel")!;

    [Fact]
    public void ACustomSymbol_IsShown_WithoutAnIcon()
    {
        ToastForm.ShowToast("Copied", Host(), symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);

        Assert.True(IconLabel().Visible);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, IconLabel().Text);
        Assert.True(Current()!.GetPrivateField<Label>("_messageLabel")!.Padding.Left > 0);
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

        toast.Symbol = ToastIcon.None;
        Assert.False(IconLabel().Visible);
        Assert.Equal(0, toast.GetPrivateField<Label>("_messageLabel")!.Padding.Left);
    }

    [Fact]
    public void ACustomSymbolOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.CustomSymbol = ErikwnkWFUI.UIStyles.Symbols.Copy));
    }

    [Fact]
    public void AToastThatWaits_KeepsItsCustomSymbol_AndTwoWithOtherSymbolsAreTwo()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast("Same", host, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);
        ToastForm.ShowToast("Same", host, symbol: ErikwnkWFUI.UIStyles.Symbols.Delete);
        ToastForm.ShowToast("Same", host, symbol: ErikwnkWFUI.UIStyles.Symbols.Delete);

        var queue = (System.Collections.IEnumerable)((System.Collections.IDictionary)typeof(ToastForm)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)[host]!;

        Assert.Equal(2, queue.Cast<object>().Count());

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, IconLabel().Text);
    }

    [Fact]
    public void TheFactories_AndTheToaster_PassTheSymbolOn()
    {
        Form host = Host();

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard("a", host, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, IconLabel().Text);

        ErikwnkWFUI.UIStyles.Toasts.ShowPrimary("b", host, symbol: ErikwnkWFUI.UIStyles.Symbols.Delete);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Delete, IconLabel().Text);

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard(new InfoPopupSection("t", "c"), host, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, IconLabel().Text);

        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreatePrimary(symbol: ErikwnkWFUI.UIStyles.Symbols.Delete);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Delete, toaster.CustomSymbol);
        toaster.Show("d", host);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Delete, IconLabel().Text);

        toaster.CustomSymbol = null;
        toaster.Show("e", host);
        Assert.False(IconLabel().Visible);
    }
}
