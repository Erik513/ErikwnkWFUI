using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>Disposing the popup frees both of its timers and the attached controls, and can be done twice.</summary>
public class InfoPopupDisposeTests
{
    [Fact]
    public void Disposing_FreesBothTimers_AndDetachesTheControls()
    {
        InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        using Button button = new Button();
        popup.AttachTo(button, "Text");
        Timer hide = popup.GetPrivateField<Timer>("_hideTimer")!;
        Timer delay = popup.GetPrivateField<Timer>("_showDelayTimer")!;

        popup.Dispose();

        Assert.Null(popup.GetPrivateField<Timer>("_hideTimer"));
        Assert.Null(popup.GetPrivateField<Timer>("_showDelayTimer"));
        Assert.False(hide.Enabled);
        Assert.False(delay.Enabled);
        Assert.Empty(popup.GetPrivateField<System.Collections.IDictionary>("_bindings")!);
    }

    [Fact]
    public void ADisposedPopup_CanBeDisposedAgain_AndAskedToShowNothing()
    {
        InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        using Button button = new Button();
        popup.Dispose();

        popup.Dispose();
        popup.ShowNextTo(button);
        popup.CancelPendingShow();

        Assert.True(popup.IsDisposed);
    }
}
