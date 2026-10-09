using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The toast settings as properties: created by the factory, changed later, used by every toast.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToasterTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToasterTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();
        _host?.Dispose();
        ((System.Collections.IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();

        System.Collections.IList scheduled = (System.Collections.IList)typeof(ToastForm).GetField("_scheduled", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        foreach (System.Windows.Forms.Timer timer in scheduled.Cast<System.Windows.Forms.Timer>().ToArray())
        {
            timer.Stop();
            timer.Dispose();
        }

        scheduled.Clear();
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
}
