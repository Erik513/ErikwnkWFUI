using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>The mechanics of the value popup, without a slider: a plain control that answers its questions.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ValuePopupTests : IDisposable
{
    private sealed class FakeHost : IValuePopupHost
    {
        public Control Owner { get; set; } = null!;
        public double Value { get; set; } = 10;
        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public Func<double, string> Formatter { get; set; } = v => v.ToString("0.##");
        public int AnchorX { get; set; } = 20;
        public UIColorSet PopupColors { get; set; } = null!;

        public string Format(double value) => Formatter(value);
    }

    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private readonly int _originalDelay = ValuePopup.HideDelayMs;
    private readonly List<IDisposable> _disposables = new();

    public ValuePopupTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        ValuePopup.HideDelayMs = _originalDelay;

        foreach (IDisposable item in _disposables)
            item.Dispose();
    }

    private (ValuePopup popup, FakeHost host, Control owner) Create(bool enabled = true)
    {
        Form form = OffscreenForm.Create(new Size(300, 120), Color.Black);
        _disposables.Add(form);
        Control owner = new Panel { Location = new Point(20, 60), Size = new Size(200, 20) };
        form.Controls.Add(owner);
        form.Show();
        Application.DoEvents();

        FakeHost host = new FakeHost { Owner = owner, PopupColors = UIStyles.Colors.Sets.Gray };
        ValuePopup popup = new ValuePopup(host) { Enabled = enabled };
        _disposables.Add(new DisposeAction(popup.Dispose));

        return (popup, host, owner);
    }

    private sealed class DisposeAction : IDisposable
    {
        private readonly Action _action;
        public DisposeAction(Action action) { _action = action; }
        public void Dispose() => _action();
    }

    [Fact]
    public void NothingIsShown_WhileItIsNotEnabled()
    {
        (ValuePopup popup, _, _) = Create(enabled: false);

        popup.ShowUpdate();
        popup.ShowAfterInteraction();
        popup.MouseEnter(dragging: false);

        Assert.Null(popup.Form);
    }

    [Fact]
    public void ShowUpdate_ShowsTheFormattedValue_AndTurningItOffTakesItAway()
    {
        (ValuePopup popup, FakeHost host, _) = Create();
        host.Value = 42;
        host.Formatter = v => v + " units";

        popup.ShowUpdate();

        Assert.NotNull(popup.Form);
        Assert.True(popup.Form!.Visible);
        Assert.Equal("42 units", Text(popup.Form));

        popup.Enabled = false;
        Assert.False(popup.Form.Visible);
    }

    [Fact]
    public void ADisabledOwner_ShowsNothing()
    {
        (ValuePopup popup, _, Control owner) = Create();
        owner.Enabled = false;

        popup.ShowUpdate(force: true);

        Assert.Null(popup.Form);
    }

    [Fact]
    public void TheWidth_IsThatOfTheWidestValueOfTheRange_NotOfTheEnds()
    {
        (ValuePopup popup, FakeHost host, _) = Create();
        host.Minimum = 0;
        host.Maximum = 1;
        host.Value = 0;
        host.Formatter = v => v.ToString("0.000");   // the middle is no wider here, but "0.000" is wider than "0"

        popup.ShowUpdate(force: true);
        int wide = popup.Form!.FixedSize.Width;

        host.Formatter = v => ((int)Math.Round(v)).ToString();
        popup.MarkWidthStale();
        popup.ShowUpdate(force: true);

        Assert.True(wide > popup.Form.FixedSize.Width, "the width did not follow the format after MarkWidthStale");
    }

    [Fact]
    public void AValueWiderThanThoseMeasured_MakesThePopupGrow()
    {
        (ValuePopup popup, FakeHost host, _) = Create();
        host.Formatter = v => v > 52 && v < 53 ? "1234567890" : "1";   // (wide only between two of the twenty steps)
        host.Value = 5;
        popup.ShowUpdate(force: true);
        int narrow = popup.Form!.FixedSize.Width;

        host.Value = 52.5;
        popup.ShowUpdate(force: true);

        Assert.True(popup.Form.FixedSize.Width > narrow);
    }

    [Fact]
    public void AnUpdateInsideTheThrottle_IsMadeLater_WithTheValueOfThatMoment()
    {
        (ValuePopup popup, FakeHost host, _) = Create();
        host.Formatter = v => "v" + (int)v;
        host.Value = 1;
        popup.ShowUpdate(force: true);
        InfoPopupForm form = popup.Form!;

        host.Value = 95;
        popup.ShowUpdate();             // inside the 40 ms: held back
        host.Value = 100;
        popup.ShowUpdate();

        Assert.NotEqual("v100", Text(form));

        TestPictures.Pump(200);

        Assert.Equal("v100", Text(form));
    }

    private static string Text(InfoPopupForm form)
    {
        InfoPopupSection[] sections = form.GetPrivateField<InfoPopupSection[]>("_sections")!;
        return sections.Length == 0 ? "" : sections[0].Text;
    }

    [Fact]
    public void AfterAnInteraction_ThePopupStaysForTheDelay_AndThenGoes()
    {
        ValuePopup.HideDelayMs = 150;
        (ValuePopup popup, _, _) = Create();

        popup.ShowAfterInteraction(force: true);
        Assert.True(popup.Form!.Visible);
        Assert.True(popup.LingerTimer!.Enabled);

        TestPictures.Pump(500);

        Assert.False(popup.Form.Visible);
        Assert.False(popup.LingerTimer.Enabled);
    }

    [Fact]
    public void AHover_GoesAtOnce_ButAPopupThatLingers_StaysWhenTheMouseLeaves()
    {
        ValuePopup.HideDelayMs = 5000;
        (ValuePopup popup, _, _) = Create();

        popup.MouseEnter(dragging: false);
        Assert.True(popup.Form!.Visible);
        popup.MouseLeave(dragging: false);
        Assert.False(popup.Form.Visible);

        popup.ShowAfterInteraction(force: true);
        popup.MouseLeave(dragging: false);
        Assert.True(popup.Form.Visible);
    }

    [Fact]
    public void WhileDragging_TheMouseDoesNotShowOrHide()
    {
        (ValuePopup popup, _, _) = Create();

        popup.MouseEnter(dragging: true);
        Assert.Null(popup.Form);

        popup.ShowUpdate(force: true);
        popup.MouseLeave(dragging: true);
        Assert.True(popup.Form!.Visible);
    }

    [Fact]
    public void ALostFocus_TakesItAwayAtOnce_EvenWhileItLingers()
    {
        ValuePopup.HideDelayMs = 5000;
        (ValuePopup popup, _, _) = Create();
        popup.ShowAfterInteraction(force: true);

        popup.LostFocus();

        Assert.False(popup.Form!.Visible);
        Assert.False(popup.LingerTimer!.Enabled);
    }

    [Fact]
    public void ThePopupFollowsTheColorsOfTheHost()
    {
        (ValuePopup popup, FakeHost host, _) = Create();
        popup.ShowUpdate(force: true);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Hover, popup.Form!.Colors!.Hover);

        host.PopupColors = UIStyles.Colors.Sets.Teal;

        Assert.Equal(UIStyles.Colors.Sets.Teal.Hover, popup.Form.Colors!.Hover);
    }

    [Fact]
    public void Dispose_DisposesThePopupAndTheTimers_AndMayBeCalledAgain()
    {
        ValuePopup.HideDelayMs = 5000;
        (ValuePopup popup, _, _) = Create();
        popup.ShowAfterInteraction(force: true);
        InfoPopupForm form = popup.Form!;
        System.Windows.Forms.Timer linger = popup.LingerTimer!;

        popup.Dispose();
        popup.Dispose();

        Assert.True(form.IsDisposed);
        Assert.Null(popup.Form);
        Assert.Null(popup.LingerTimer);
        Assert.False(linger.Enabled);
    }

    [Fact]
    public void ThePositionOfAScrolledParent_IsKeptWhenThePopupComesAndGoes()
    {
        Form form = OffscreenForm.Create(new Size(300, 120), Color.Black);
        _disposables.Add(form);
        Panel scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Panel owner = new Panel { Location = new Point(20, 300), Size = new Size(200, 20) };
        scroller.Controls.Add(owner);
        form.Controls.Add(scroller);
        form.Show();
        Application.DoEvents();
        scroller.AutoScrollPosition = new Point(0, 250);
        int before = -scroller.AutoScrollPosition.Y;

        FakeHost host = new FakeHost { Owner = owner, PopupColors = UIStyles.Colors.Sets.Gray };
        ValuePopup popup = new ValuePopup(host) { Enabled = true };
        _disposables.Add(new DisposeAction(popup.Dispose));
        popup.ShowUpdate(force: true);
        popup.Hide();

        Assert.Equal(before, -scroller.AutoScrollPosition.Y);
    }
}
