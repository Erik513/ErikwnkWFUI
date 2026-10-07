using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// Odd uses of the info popup: handlers that dispose it or its control, calls on a popup that
/// is disposed, the same call twice, a control that is not on a form, content that is huge or
/// comes from a function that fails, a change while it is up. [Collection]: the colors read the
/// live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupEdgeCaseTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;

    public InfoPopupEdgeCaseTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private (Form Host, Button Anchor) CreateHost()
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 300)
        };
        Button anchor = new Button { Location = new Point(20, 60), Size = new Size(80, 30) };

        host.Controls.Add(anchor);
        host.Show();
        _forms.Add(host);

        return (host, anchor);
    }

    private InfoPopupForm CreatePopup(PopupDelay delay = PopupDelay.Medium)
    {
        InfoPopupForm popup = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(delay: delay);
        _forms.Add(popup);

        return popup;
    }

    private static void Move(Control control) => control.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));

    // ---- a popup that is disposed ----

    [Fact]
    public void EveryCallOnADisposedPopup_IsHarmless()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.AttachTo(anchor, "tip");
        popup.Dispose();

        Exception? thrown = Record.Exception(() =>
        {
            popup.SetContent("again");
            popup.ShowNextTo(anchor);
            popup.ShowAtMouse(anchor, new Point(1, 1));
            popup.ShowCenteredAbove(anchor, 1);
            popup.ShowAtMouseDelayed(anchor, new Point(1, 1), new InfoPopupSection("a", "b"));
            popup.CancelPendingShow();
            popup.AttachTo(anchor, "tip");
            popup.Detach(anchor);
            popup.Delay = PopupDelay.Long;
            popup.Duration = PopupDuration.Long;
            popup.Transparency = PopupTransparency.High;
            popup.Colors = null;
            popup.FixedSize = new Size(50, 20);
            popup.Dispose();
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void AHandlerThatDisposesThePopup_WhileTheMouseMoves_LeavesNothingBroken()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.AttachTo(anchor, "tip");

        // Added after the attachment: runs before and after the popup's own handler, in order.
        anchor.MouseMove += delegate { popup.Dispose(); };

        Exception? thrown = Record.Exception(() => Move(anchor));

        Assert.Null(thrown);
        Assert.True(popup.IsDisposed);
    }

    [Fact]
    public void AHandlerThatDisposesTheControl_WhileThePopupIsUp_LeavesNothingBroken()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(anchor, "tip");
        Move(anchor);
        Assert.True(popup.Visible);

        anchor.Dispose();

        // The popup is not up for a control that is gone, and a change of content is no trouble.
        Exception? thrown = Record.Exception(() => popup.SetContent("Header", "A text that is quite a bit longer than the one before."));

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    // ---- the same call twice ----

    [Fact]
    public void TheSameCallTwice_IsAsOneCall()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("same");

        popup.ShowNextTo(anchor);
        popup.ShowNextTo(anchor);
        Assert.True(popup.Visible);

        popup.Hide();
        popup.Hide();
        Assert.False(popup.Visible);

        popup.CancelPendingShow();
        popup.CancelPendingShow();

        popup.AttachTo(anchor, "tip");
        popup.Detach(anchor);
        popup.Detach(anchor);

        popup.Dispose();
        popup.Dispose();
        Assert.True(popup.IsDisposed);
    }

    // ---- a control that is not on a form ----

    [Fact]
    public void AControlWithoutAForm_GetsThePopupOnTheScreen_WithoutAnOwner()
    {
        InfoPopupForm popup = CreatePopup();
        Button loose = new Button { Size = new Size(80, 30) };
        popup.SetContent("tip");

        Exception? thrown = Record.Exception(() =>
        {
            popup.ShowNextTo(loose);
            popup.ShowAtMouse(loose, new Point(200, 200));
        });

        Assert.Null(thrown);
        Assert.True(popup.Visible);
        Assert.Null(popup.Owner);
        Assert.True(Screen.FromControl(popup).WorkingArea.Contains(popup.Bounds));

        loose.Dispose();
    }

    // ---- content that is odd ----

    [Fact]
    public void HugeContent_WrapsAndFits_OrSitsAtTheTopLeftOfTheWindow()
    {
        (Form host, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        InfoPopupSection[] many = Enumerable.Range(0, 120)
            .Select(i => new InfoPopupSection("Block " + i, "A text of the block " + i + " that is long enough to wrap onto more than one line in the popup."))
            .ToArray();

        Exception? thrown = Record.Exception(() =>
        {
            popup.SetContent(many);
            popup.ShowNextTo(anchor);
        });

        Assert.Null(thrown);
        Assert.True(popup.Visible);
        Assert.True(popup.Width <= 400, "the popup grew wider than its maximum text width allows: " + popup.Width);
        Assert.True(popup.Left >= Math.Min(host.Left, Screen.FromControl(host).WorkingArea.Left));
    }

    [Fact]
    public void AFunctionThatReturnsNothing_ShowsNothing()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup(PopupDelay.Instant);
        popup.AttachTo(anchor, () => null!);

        Exception? thrown = Record.Exception(() => Move(anchor));

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void AFunctionThatFails_RaisesItsError_AndLeavesThePopupUsable()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        bool fail = true;
        popup.AttachTo(anchor, () =>
        {
            if (fail)
                throw new InvalidOperationException("the content failed");

            return new[] { new InfoPopupSection(null, "works now") };
        });

        Move(anchor);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty));
        Assert.Equal("the content failed", error.Message);
        Assert.False(popup.Visible);
        Assert.False(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled, "the timer keeps firing after the failure");

        fail = false;
        anchor.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        Move(anchor);
        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);
        Assert.True(popup.Visible);
    }

    // ---- shared by two windows, and going away while it is up ----

    [Fact]
    public void AnotherWindowsControl_TakesThePopupOver_WithThatWindowAsItsOwner()
    {
        (Form first, Button firstAnchor) = CreateHost();
        (Form second, Button secondAnchor) = CreateHost();
        second.Location = new Point(first.Right + 20, first.Top);
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        popup.ShowNextTo(firstAnchor);
        Assert.Same(first, popup.Owner);

        popup.ShowNextTo(secondAnchor);

        Assert.True(popup.Visible);
        Assert.Same(second, popup.Owner);

        // It follows the window it belongs to now.
        Point before = popup.Location;
        second.Location = new Point(second.Left + 30, second.Top);
        Assert.Equal(before.X + 30, popup.Location.X);
    }

    [Fact]
    public void AVisiblePopup_CanBeDisposed_WithoutAnError()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(anchor);
        Assert.True(popup.Visible);

        Exception? thrown = Record.Exception(() => popup.Dispose());

        Assert.Null(thrown);
        Assert.True(popup.IsDisposed);
    }

    [Fact]
    public void WhenTheOwnerWindowCloses_ThePopupGoesWithIt_AndLaterCallsAreHarmless()
    {
        (Form host, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(anchor);

        host.Close();

        Exception? thrown = Record.Exception(() =>
        {
            popup.ShowNextTo(anchor);
            popup.SetContent("later");
            popup.Hide();
        });

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    // ---- changes while it is up ----

    [Fact]
    public void TheColorsTheTransparencyAndTheSize_CanChangeWhileItIsUp()
    {
        (Form host, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Header", "text");
        popup.ShowNextTo(anchor);

        Exception? thrown = Record.Exception(() =>
        {
            popup.Colors = ErikwnkWFUI.UIStyles.Colors.Sets.Green;
            popup.Transparency = PopupTransparency.Opaque;
            popup.Delay = PopupDelay.Long;
            popup.Duration = PopupDuration.Short;
            popup.FixedSize = new Size(120, 60);
        });

        Assert.Null(thrown);
        Assert.True(popup.Visible);
        Assert.Equal(new Size(120, 60), popup.ClientSize);
        Assert.Equal(1.0, popup.Opacity, 2);
        Assert.True(Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea).Contains(popup.Bounds));
    }

    [Fact]
    public void TheDelayChangedWhileAShowIsScheduled_AppliesToTheNextOne_AndTheScheduledOneStillShows()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowAtMouseDelayed(anchor, new Point(100, 100), new InfoPopupSection("A", "b"));
        popup.Delay = PopupDelay.Instant;
        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);

        Assert.True(popup.Visible);
    }
}
