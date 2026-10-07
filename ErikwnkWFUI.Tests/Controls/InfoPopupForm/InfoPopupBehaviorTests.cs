using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// What the info popup does with its content and its window: the text and the sections, mixing
/// the two on one reused popup, the show delay, a disposed owner, the position on the screen.
/// Popups are shown for real (briefly) next to a host form. [Collection]: the text of "none"
/// is a language string.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class InfoPopupBehaviorTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private (Form Host, Button Owner) CreateHost(Point? location = null)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = location ?? new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 200)
        };
        Button owner = new Button { Location = new Point(20, 60), Size = new Size(80, 30) };

        host.Controls.Add(owner);
        host.Show();
        _forms.Add(host);

        return (host, owner);
    }

    private InfoPopupForm CreatePopup(string title = "Info")
    {
        InfoPopupForm popup = new InfoPopupForm(title);
        _forms.Add(popup);

        return popup;
    }

    // What the popup shows now: the texts of the labels in its layout, in order.
    private static string[] Shown(InfoPopupForm popup)
    {
        FlowLayoutPanel layout = popup.GetPrivateField<FlowLayoutPanel>("_layout")!;

        return layout.Controls.OfType<Label>().Select(label => label.Text).ToArray();
    }

    private static InfoPopupSection Section(string header, string text) => new InfoPopupSection(header, text);

    [Fact]
    public void ShowInfo_ShowsTheTitleAndTheText_AndRepositionsWhenShownAgain()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("Info");

        popup.ShowInfo("First", owner);
        Assert.True(popup.Visible);
        Assert.Equal(new[] { "Info", "First" }, Shown(popup));

        popup.ShowInfo("Second", owner);
        Assert.Equal(new[] { "Info", "Second" }, Shown(popup));
    }

    [Fact]
    public void AnEmptyText_ShowsTheLocalizedNone()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("Info");

        popup.ShowInfo("  ", owner);

        Assert.Equal(UIStrings.Get("InfoPopup.None"), Shown(popup)[1]);
    }

    [Fact]
    public void TheText_AfterTheSections_IsShownAgain_OnTheSameReusedPopup()
    {
        // One popup is reused for everything: text, then sections, then text again.
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("Info");

        popup.ShowInfo("Plain", owner);
        popup.ShowSections(owner, Section("Header", "Body"));
        Assert.Equal(new[] { "Header", "Body" }, Shown(popup));

        popup.ShowInfo("Plain again", owner);
        Assert.Contains("Plain again", Shown(popup));
    }

    [Fact]
    public void TheSectionsAtTheMouse_AreRebuilt_AfterOtherSectionsWereShownInBetween()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");
        Point mouse = new Point(100, 100);

        popup.ShowSectionsAtMouse(owner, mouse, Section("A", "first"));
        popup.ShowSections(owner, Section("B", "second"));
        popup.ShowSectionsAtMouse(owner, mouse, Section("A", "first"));

        Assert.Equal(new[] { "A", "first" }, Shown(popup));
    }

    [Fact]
    public void TheSameSections_AreNotRebuilt_WhenShownAgainAtTheMouse()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");
        Point mouse = new Point(100, 100);

        popup.ShowSectionsAtMouse(owner, mouse, Section("A", "first"));
        Label before = popup.GetPrivateField<FlowLayoutPanel>("_layout")!.Controls.OfType<Label>().First();

        popup.ShowSectionsAtMouse(owner, new Point(120, 100), Section("A", "first"));
        Label after = popup.GetPrivateField<FlowLayoutPanel>("_layout")!.Controls.OfType<Label>().First();

        Assert.Same(before, after);
    }

    [Fact]
    public void ASectionWithoutAHeaderOrText_OmitsThatPart()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");

        popup.ShowSections(owner, Section("", "only text"), Section("only header", null!), Section(null!, ""));

        Assert.Equal(new[] { "only text", "only header" }, Shown(popup));
    }

    [Fact]
    public void ANullOrDisposedOwner_ShowsNothing_AndRaisesNothing()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        Button gone = new Button();
        gone.Dispose();

        Exception? thrown = Record.Exception(() =>
        {
            popup.ShowInfo("x", null!);
            popup.ShowInfo("x", gone);
            popup.ShowInfoAtMouse("x", gone, new Point(1, 1));
            popup.ShowInfoCenteredAbove("x", gone, 1);
            popup.ShowSections(gone, Section("a", "b"));
            popup.ShowSectionsAtMouseDelayed(gone, new Point(1, 1), Section("a", "b"));
        });

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void ACancelledDelayedShow_NeverShows_AndAnOwnerDisposedInTheMeantimeIsHarmless()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");

        popup.ShowSectionsAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        popup.CancelPendingShow();
        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);
        Assert.False(popup.Visible);

        popup.ShowSectionsAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        owner.Dispose();
        Exception? thrown = Record.Exception(() => popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty));

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void ADelayedShow_ShowsTheSections_WhenTheTimerFires()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");

        popup.ShowSectionsAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        Assert.False(popup.Visible);

        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "A", "b" }, Shown(popup));
    }

    [Fact]
    public void ThePopup_NeverTakesTheActivationAway_AndClosesWithItsOwnerForm()
    {
        (Form host, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowInfo("x", owner);

        Assert.True((bool)typeof(InfoPopupForm).GetProperty("ShowWithoutActivation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(popup)!);
        Assert.Same(host, popup.Owner);

        host.Close();
        Assert.True(popup.IsDisposed || !popup.Visible);
    }

    [Fact]
    public void AClickOnThePopup_DoesNotActivateIt()
    {
        InfoPopupForm popup = CreatePopup();
        System.Reflection.PropertyInfo property = typeof(Form).GetProperty("CreateParams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        CreateParams parameters = (CreateParams)property.GetValue(popup)!;

        Assert.NotEqual(0, parameters.ExStyle & 0x08000000);
    }

    [Fact]
    public void TheCompactForm_ShowsOneTextOnly_SectionsAreLeftOut()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = new InfoPopupForm { Compact = true };
        _forms.Add(popup);

        popup.ShowSections(owner, Section("Header", "Body"));
        Assert.False(popup.Visible);

        popup.ShowInfo("70%", owner);
        Assert.True(popup.Visible);
        Assert.Equal("70%", popup.GetPrivateField<Label>("_textLabel")!.Text);
    }

    [Fact]
    public void AnApplicationsOwnBackColor_IsReplacedOnTheNextShow_TheColorsPropertyIsTheWay()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        Color expected = popup.BackColor;

        popup.BackColor = Color.HotPink;
        popup.ShowInfo("x", owner);

        Assert.Equal(expected, popup.BackColor);
    }

    [Fact]
    public void TheSectionLabelsThrownAway_AreDisposed_NotLeftBehind()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("");

        popup.ShowSections(owner, Section("A", "first"));
        Label old = popup.GetPrivateField<FlowLayoutPanel>("_layout")!.Controls.OfType<Label>().First();

        popup.ShowSections(owner, Section("B", "second"));

        Assert.True(old.IsDisposed);
    }

    [Fact]
    public void ATextShownAfterTheSections_HasItsOwnColorsAfterAThemeChange()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup("Info");
        popup.ShowInfo("one", owner);
        popup.ShowSections(owner, Section("A", "b"));

        // A light set: its text is dark, unlike the gray the popup started with.
        popup.Colors = ErikwnkWFUI.UIStyles.Colors.CreateSet(Color.White);
        popup.ShowInfo("two", owner);

        Label text = popup.GetPrivateField<Label>("_textLabel")!;
        Color back = popup.BackColor;
        Assert.True(ColorContrastHelper.GetContrastRatio(text.ForeColor, back) >= 4.5);
    }

    [Fact]
    public void AHiddenPopup_CanBeShownAgain()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowInfo("one", owner);
        popup.Hide();
        Assert.False(popup.Visible);

        popup.ShowInfo("two", owner);
        Assert.True(popup.Visible);
        Assert.Equal("two", Shown(popup)[1]);
    }

    [Fact]
    public void AnOwnerAtTheEdgeOfTheScreen_GetsAPopupThatStaysOnTheScreen()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        (Form _, Button owner) = CreateHost(new Point(area.Right - 120, area.Bottom - 120));
        InfoPopupForm popup = CreatePopup("Info");

        popup.ShowInfo("A text that is long enough to wrap onto a few lines in the popup, " +
            "so that the popup is not just one small line at the corner.", owner);

        Assert.True(area.Contains(popup.Bounds), "the popup left the screen: " + popup.Bounds + " in " + area);
    }
}
