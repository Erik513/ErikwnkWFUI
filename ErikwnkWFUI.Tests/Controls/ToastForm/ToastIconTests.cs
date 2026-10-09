using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The symbol of a toast: none by default, a check mark, info, warning or error on request.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastIconTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastIconTests()
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

    private static Label MessageLabel() => Current()!.GetPrivateField<Label>("_messageLabel")!;

    [Fact]
    public void WithoutAnIcon_NoSymbolIsShown_AndTheTextUsesTheWidth()
    {
        ToastForm.ShowToast("Plain", Host());

        Assert.Equal(ToastIcon.None, Current()!.Symbol);
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

        Assert.Equal(icon, Current()!.Symbol);
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

        toast.Symbol = ToastIcon.Success;
        Assert.NotEqual(error, IconLabel().Text);

        toast.Symbol = ToastIcon.None;
        Assert.False(IconLabel().Visible);
        Assert.Equal(0, MessageLabel().Padding.Left);
    }

    [Fact]
    public void ASymbolOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.Symbol = ToastIcon.Warning));
    }

    [Fact]
    public void AToastThatWaits_KeepsItsSymbol_AndTwoOfTheSameTextWithOtherSymbolsAreTwo()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast("Same", host, icon: ToastIcon.Warning);
        ToastForm.ShowToast("Same", host, icon: ToastIcon.Error);
        ToastForm.ShowToast("Same", host, icon: ToastIcon.Error);

        var queue = (System.Collections.IEnumerable)((System.Collections.IDictionary)typeof(ToastForm)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)[host]!;

        Assert.Equal(2, queue.Cast<object>().Count());

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

        Assert.Equal(ToastIcon.Warning, Current()!.Symbol);
    }

    [Fact]
    public void TheFactories_AndTheToaster_PassTheIconOn()
    {
        Form host = Host();

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard("a", host, icon: ToastIcon.Info);
        Assert.Equal(ToastIcon.Info, Current()!.Symbol);

        ErikwnkWFUI.UIStyles.Toasts.ShowPrimary("b", host, icon: ToastIcon.Warning);
        Assert.Equal(ToastIcon.Warning, Current()!.Symbol);

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard(new InfoPopupSection("t", "c"), host, icon: ToastIcon.Error);
        Assert.Equal(ToastIcon.Error, Current()!.Symbol);

        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreatePrimary(icon: ToastIcon.Success);
        Assert.Equal(ToastIcon.Success, toaster.Icon);
        toaster.Show("d", host);
        Assert.Equal(ToastIcon.Success, Current()!.Symbol);

        toaster.Icon = ToastIcon.None;
        toaster.Show("e", host);
        Assert.Equal(ToastIcon.None, Current()!.Symbol);
    }
}
