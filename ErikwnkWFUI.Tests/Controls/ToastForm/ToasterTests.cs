using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The toast settings as properties: created by the factory, changed later, used by every toast.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToasterTests : ToastTestBase
{
    [Fact]
    public void CreateStandard_HasTheDefaults_AndCreatePrimaryTheAccent()
    {
        Toaster standard = ErikwnkWFUI.UIStyles.Toasts.CreateStandard();
        Toaster primary = ErikwnkWFUI.UIStyles.Toasts.CreatePrimary();

        Assert.Null(standard.Colors);
        Assert.Equal(PopupTransparency.Medium, standard.Transparency);
        Assert.Equal(PopupDuration.Medium, standard.Duration);
        Assert.Equal(PopupDelay.Instant, standard.Delay);
        Assert.Equal(ErikwnkWFUI.UIStyles.Colors.Sets.Primary.Basic, primary.Colors!.Basic);
    }

    [Fact]
    public void TheSettings_AreTakenByTheToast()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard(ErikwnkWFUI.UIStyles.Colors.Sets.Green, PopupTransparency.High, PopupDuration.Long);

        toaster.Show("Done", Host());

        Assert.Equal(PopupTransparency.High, Current()!.Transparency);
        Assert.Equal(PopupDuration.Long, Current()!.Duration);
        Assert.Equal(ErikwnkWFUI.UIStyles.Colors.Sets.Green.Basic, Current()!.Colors!.Basic);
    }

    [Fact]
    public void ChangedSettings_AreUsedByTheNextToast_NotByOneThatIsUp()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard();
        Form host = Host();

        toaster.Show("First", host);
        ToastForm first = Current()!;
        toaster.Duration = PopupDuration.Short;
        toaster.Transparency = PopupTransparency.Opaque;

        Assert.Equal(PopupDuration.Medium, first.Duration);

        toaster.Show("Second", host);

        Assert.Equal(PopupDuration.Short, Current()!.Duration);
        Assert.Equal(PopupTransparency.Opaque, Current()!.Transparency);
    }

    [Fact]
    public void ATitledToast_IsShownWithTheSettings()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard(duration: PopupDuration.Short);

        toaster.Show(new InfoPopupSection("Title", "Text"), Host());

        Assert.Equal("Title", Current()!.GetPrivateField<Label>("_titleLabel")!.Text);
        Assert.Equal(PopupDuration.Short, Current()!.Duration);
    }

    [Fact]
    public void ADelay_ShowsNoToastYet()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard(delay: PopupDelay.Long);

        toaster.Show("Later", Host());

        Assert.Null(Current());
    }

    [Fact]
    public void WithoutAWindowOrText_NothingHappens()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard();

        Exception? thrown = Record.Exception(() =>
        {
            toaster.Show("x", null!);
            toaster.Show("", Host());
        });

        Assert.Null(thrown);
        Assert.Null(Current());
    }

    // The symbol, the symbol of one's own and the place travel through every way to show a toast.
    [Fact]
    public void EveryWayToShowAToast_PassesTheIconTheSymbolAndThePositionOn()
    {
        Form host = Host();
        Rectangle client = host.RectangleToScreen(host.ClientRectangle);
        InfoPopupSection content = new InfoPopupSection("t", "c");
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard(
            icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft);

        Dictionary<string, Action> ways = new Dictionary<string, Action>
        {
            ["ShowStandard(message)"] = () => ErikwnkWFUI.UIStyles.Toasts.ShowStandard("a", host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["ShowStandard(content)"] = () => ErikwnkWFUI.UIStyles.Toasts.ShowStandard(content, host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["ShowPrimary(message)"] = () => ErikwnkWFUI.UIStyles.Toasts.ShowPrimary("a", host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["ShowPrimary(content)"] = () => ErikwnkWFUI.UIStyles.Toasts.ShowPrimary(content, host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["ShowToast(message)"] = () => ToastForm.ShowToast("a", host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["ShowToast(content)"] = () => ToastForm.ShowToast(content, host, icon: ToastIcon.Warning, symbol: ErikwnkWFUI.UIStyles.Symbols.Copy, position: ToastPosition.TopLeft),
            ["Toaster.Show(message)"] = () => toaster.Show("a", host),
            ["Toaster.Show(content)"] = () => toaster.Show(content, host)
        };

        foreach (KeyValuePair<string, Action> way in ways)
        {
            Current()?.Dispose();
            way.Value();

            Assert.True(Current() != null, way.Key + " showed no toast");
            Assert.Equal(ToastIcon.Warning, Current()!.Symbol);
            Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, Part("_iconLabel").Text);
            Assert.True(Current()!.Left < client.Left + client.Width / 4 && Current()!.Top < client.Top + client.Height / 4, way.Key + " did not pass the position on: " + Current()!.Location);
        }
    }

    [Fact]
    public void TheToasterProperties_AreTheOnesTheFactoryTookAsParameters()
    {
        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreatePrimary(
            icon: ToastIcon.Info, symbol: ErikwnkWFUI.UIStyles.Symbols.Delete, position: ToastPosition.Center);

        Assert.Equal(ToastIcon.Info, toaster.Icon);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Delete, toaster.CustomSymbol);
        Assert.Equal(ToastPosition.Center, toaster.Position);
    }
}
