using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The popup as the tooltip of controls (AttachTo): scheduled when the mouse moves over a
/// control, shown once for each time the mouse enters, gone when the mouse leaves or on a click
/// or key, one popup for several controls, a disabled control, detaching. The mouse events are
/// raised on the controls directly. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupAttachTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;

    public InfoPopupAttachTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private (Form Host, Button First, Button Second) CreateHost()
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 300)
        };
        Button first = new Button { Location = new Point(20, 60), Size = new Size(80, 30) };
        Button second = new Button { Location = new Point(200, 60), Size = new Size(80, 30) };

        host.Controls.Add(first);
        host.Controls.Add(second);
        host.Show();
        _forms.Add(host);

        return (host, first, second);
    }

    private InfoPopupForm CreatePopup(PopupDelay delay = PopupDelay.Medium)
    {
        InfoPopupForm popup = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(delay: delay);
        _forms.Add(popup);

        return popup;
    }

    private static void Move(Control control) => control.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));

    private static void Leave(Control control) => control.InvokePrivate("OnMouseLeave", EventArgs.Empty);

    private static void Tick(InfoPopupForm popup) => popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);

    private static string[] Content(InfoPopupForm popup)
    {
        InfoPopupSection[] sections = popup.GetPrivateField<InfoPopupSection[]>("_sections")!;

        return sections.Select(section => (section.Header ?? "-") + "|" + (section.Text ?? "-")).ToArray();
    }

    [Fact]
    public void WhenTheMouseMovesOverTheControl_ThePopupIsScheduled_AndShownAfterTheDelay()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.AttachTo(first, "Tip");

        Move(first);
        Assert.False(popup.Visible);

        Tick(popup);

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "-|Tip" }, Content(popup));
    }

    [Fact]
    public void WithoutADelay_ThePopupShowsAtTheFirstMove()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "Header", "Body");

        Move(first);

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "Header|Body" }, Content(popup));
    }

    [Fact]
    public void TheBlocksAndTheFunction_AreTakenAsContent_TheFunctionWhenItShows()
    {
        (Form _, Button first, Button second) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        int asked = 0;
        popup.AttachTo(first, new InfoPopupSection("A", "a"), new InfoPopupSection("B", "b"));
        popup.AttachTo(second, () => new[] { new InfoPopupSection(null, "asked " + ++asked) });

        Move(first);
        Assert.Equal(new[] { "A|a", "B|b" }, Content(popup));

        Leave(first);
        Assert.Equal(0, asked);

        Move(second);
        Assert.Equal(new[] { "-|asked 1" }, Content(popup));
        Leave(second);
        Move(second);
        Assert.Equal(new[] { "-|asked 2" }, Content(popup));
    }

    [Fact]
    public void WhileItIsUp_FurtherMovesDoNothing_AndAfterItWentAwayItDoesNotComeBackUntilTheMouseEntersAgain()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.AttachTo(first, "Tip");

        Move(first);
        Tick(popup);
        Move(first);
        Assert.False(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled, "it was scheduled again while it is up");

        // It goes away by itself after its duration.
        popup.Hide();
        Move(first);
        Assert.False(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled, "it came back without the mouse entering again");

        Leave(first);
        Move(first);
        Assert.True(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled, "it did not come back after the mouse entered again");
    }

    [Fact]
    public void WhenTheMouseLeaves_AScheduledPopupIsCancelled_AndAPopupThatIsUpGoesAway()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.AttachTo(first, "Tip");

        Move(first);
        Leave(first);
        Tick(popup);
        Assert.False(popup.Visible, "the cancelled popup showed");

        Move(first);
        Tick(popup);
        Assert.True(popup.Visible);

        Leave(first);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void AClickOrAKey_TakesThePopupAway()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "Tip");

        Move(first);
        Assert.True(popup.Visible);
        first.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.False(popup.Visible);

        Leave(first);
        Move(first);
        Assert.True(popup.Visible);
        first.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.A));
        Assert.False(popup.Visible);
    }

    [Fact]
    public void OnePopup_ServesSeveralControls_EachWithItsOwnContent()
    {
        (Form _, Button first, Button second) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "For the first");
        popup.AttachTo(second, "For the second");

        Move(first);
        Assert.Equal(new[] { "-|For the first" }, Content(popup));
        Leave(first);

        Move(second);
        Assert.Equal(new[] { "-|For the second" }, Content(popup));
        Assert.True(popup.Visible);

        // The first control's leave does not take the second one's popup away.
        Leave(first);
        Assert.True(popup.Visible);
    }

    [Fact]
    public void ADisabledControl_GetsItThroughItsParent()
    {
        (Form host, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        first.Enabled = false;
        popup.AttachTo(first, "Why it is disabled");

        host.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 300, 250, 0));
        Assert.False(popup.Visible, "the mouse is not over the control");

        host.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 30, 70, 0));
        Assert.True(popup.Visible);
        Assert.Equal(new[] { "-|Why it is disabled" }, Content(popup));

        host.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 300, 250, 0));
        Assert.False(popup.Visible, "the mouse left the disabled control");
    }

    [Fact]
    public void Detach_StopsIt_AndAttachingAgainReplacesTheContent()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "One");
        popup.AttachTo(first, "Two");

        Move(first);
        Assert.Equal(new[] { "-|Two" }, Content(popup));

        popup.Detach(first);
        Assert.False(popup.Visible, "the popup of a detached control stayed");

        Move(first);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void ADisposedControl_DetachesItself_AndADisposedPopupLetsGoOfItsControls()
    {
        (Form _, Button first, Button second) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "One");
        popup.AttachTo(second, "Two");

        first.Dispose();
        Assert.False(popup.GetPrivateField<System.Collections.IDictionary>("_bindings")!.Contains(first));

        popup.Dispose();

        Exception? thrown = Record.Exception(() =>
        {
            Move(second);
            Leave(second);
        });

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void AControlThatIsHidden_TakesThePopupAway()
    {
        (Form _, Button first, Button _) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(first, "Tip");
        Move(first);
        Assert.True(popup.Visible);

        first.Visible = false;

        Assert.False(popup.Visible);
    }
}
