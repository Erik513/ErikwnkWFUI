using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>What the message box has beyond the buttons: Ctrl+C copies its text, a text that is too long scrolls, the buttons with a symbol have names, the default button, and the details.</summary>
[Collection(ClipboardTestCollection.Name)]
public class MessageBoxFeatureTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();
    private const string LongText = "A long message that goes on and on, to see how the box wraps it and how much room it needs.";

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
    }

    private static List<Button> Buttons(MessageBoxForm form)
    {
        return form.GetPrivateField<List<Button>>("_dialogButtons")!;
    }

    private static Panel MessagePanel(MessageBoxForm form)
    {
        return form.GetPrivateField<Panel>("_messagePanel")!;
    }

    private static Label MessageLabel(MessageBoxForm form)
    {
        return form.GetPrivateField<Label>("_messageLabel")!;
    }

    // ---- Ctrl+C

    [Fact]
    public void CtrlC_CopiesTheTextOfTheBox_InTheFormOfTheBoxOfWindows()
    {
        TestClipboard.SetText("before");
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Delete the file?", "Delete", MessageBoxButtons.YesNoCancel));
        Message message = new Message();

        bool handled = form.InvokePrivate<bool>("ProcessCmdKey", message, Keys.Control | Keys.C);

        Assert.True(handled);
        string expected = string.Join("\r\n", "---------------------------", "Delete", "---------------------------", "Delete the file?", "---------------------------", "Yes   No   Cancel", "---------------------------") + "\r\n";
        Assert.Equal(expected, TestClipboard.GetText());
    }

    [Fact]
    public void TheCopiedText_HasTheDetails_AndTheButtonsInTheLanguage()
    {
        UIStrings.Language = UILanguage.German;
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Speichern?", "Hinweis", MessageBoxButtons.YesNo, details: "Zeile 1\r\nZeile 2"));

        string text = form.GetTextForCopy();

        Assert.Contains("Speichern?", text);
        Assert.Contains("Zeile 1\r\nZeile 2", text);
        Assert.Contains("Ja   Nein", text);
    }

    [Fact]
    public void CtrlC_WithTextSelectedInTheDetails_IsLeftToTheDetails()
    {
        TestClipboard.SetText("before");
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "the details"));
        form.DetailsExpanded = true;
        TextBox box = form.GetPrivateField<TextBox>("_detailsBox")!;
        box.SelectAll();
        form.ActiveControl = box;
        Message message = new Message();

        bool handled = form.InvokePrivate<bool>("ProcessCmdKey", message, Keys.Control | Keys.C);

        Assert.False(handled);
        Assert.Equal("before", TestClipboard.GetText());
    }

    // ---- a text that is too long

    [Fact]
    public void ALongText_MakesTheWindowOfASizeHigher_UpToTheScreen_AndThenItScrolls()
    {
        string tall = string.Join(" ", Enumerable.Repeat(LongText, 12));
        string huge = string.Join(" ", Enumerable.Repeat(LongText, 300));
        MessageBoxForm normal = _host.Show(UIStyles.Forms.CreateMessageStandard("Short."));
        MessageBoxForm higher = _host.Show(UIStyles.Forms.CreateMessageStandard(tall));
        MessageBoxForm huge1 = _host.Show(UIStyles.Forms.CreateMessageStandard(huge));
        int room = Screen.PrimaryScreen!.WorkingArea.Height;

        Assert.True(higher.Height > normal.Height, "the window grows for the text");
        Assert.Equal(normal.Width, higher.Width);
        Assert.False(MessagePanel(higher).AutoScrollMinSize.Height > 0, "it fits now: no scrolling");
        Assert.True(huge1.Height <= room, "never higher than the screen: " + huge1.Height + " of " + room);
        Assert.True(MessagePanel(huge1).AutoScrollMinSize.Height > MessagePanel(huge1).Height, "what does not fit scrolls");
    }

    [Fact]
    public void WithAnOwnSize_TheWindowKeepsIt_AndALongTextScrollsInIt()
    {
        string tall = string.Join(" ", Enumerable.Repeat(LongText, 12));
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard(tall, size: new Size(500, 220)));

        Assert.Equal(new Size(500, 220), form.Size);
        Assert.True(MessagePanel(form).AutoScrollMinSize.Height > MessagePanel(form).Height);
    }

    [Fact]
    public void AShortText_StandsInTheMiddle_OfTheSpaceThatItHas()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Short."));
        Label label = MessageLabel(form);
        Panel panel = MessagePanel(form);

        // (In the padding of the preset - 20 above and 10 below for the middle size - as it always was.)
        int above = label.Top - 20;
        int below = panel.Height - label.Bottom - 10;

        Assert.True(Math.Abs(above - below) <= 2, "above " + above + ", below " + below);
        Assert.Equal(new Size(500, 220), form.Size);
    }

    [Fact]
    public void TheButtonsAndTheMessageNeverOverlap_InAnySize()
    {
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard(string.Join(" ", Enumerable.Repeat(LongText, 8)), buttons: MessageBoxButtons.YesNoCancel, size: size, details: "d"));
            Control buttons = form.GetPrivateField<Control>("_buttonPanel")!;

            Assert.True(MessagePanel(form).Bottom <= buttons.Top, size + ": the message reaches the buttons");
            Assert.True(form.ContentPanel.ClientRectangle.Contains(buttons.Bounds), size + ": the buttons are not inside");
        }
    }

    // ---- the names of the buttons

    [Fact]
    public void TheButtonsWithASymbol_HaveANameForTheScreenReaderAndTheTooltip_InTheLanguage()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNoCancel));
        string[] english = Buttons(form).Select(button => button.AccessibleName ?? "").ToArray();

        Assert.Equal(new[] { "Yes", "No", "Cancel" }, english);

        UIStrings.Language = UILanguage.German;
        MessageBoxForm german = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo));
        MessageBoxForm ok = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));

        Assert.Equal(new[] { "Ja", "Nein" }, Buttons(german).Select(button => button.AccessibleName ?? "").ToArray());
        Assert.Equal(new[] { "OK" }, Buttons(ok).Select(button => button.AccessibleName ?? "").ToArray());
    }

    // ---- the default button

    [Fact]
    public void TheSafeButtonHasTheFocusFirst_Unless_AnotherIsNamed_AndTheNamedOneAnswersEnter()
    {
        MessageBoxForm safe = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNoCancel));
        MessageBoxForm first = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNoCancel, defaultButton: MessageBoxDefault.Button1));
        MessageBoxForm second = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNoCancel, defaultButton: MessageBoxDefault.Button2));
        MessageBoxForm third = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNoCancel, defaultButton: MessageBoxDefault.Button3));

        Assert.Same(safe.CancelButton, safe.ActiveControl);
        Assert.Same(Buttons(first)[0], first.ActiveControl);
        Assert.Same(Buttons(first)[0], first.AcceptButton);
        Assert.Same(Buttons(second)[1], second.ActiveControl);
        Assert.Same(Buttons(second)[1], second.AcceptButton);
        Assert.Same(Buttons(third)[2], third.ActiveControl);
    }

    [Fact]
    public void ADefaultButtonThatIsNotThere_FallsBackToTheSafeOne()
    {
        MessageBoxForm form = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, defaultButton: MessageBoxDefault.Button3));
        MessageBoxForm ok = _host.Add(UIStyles.Forms.CreateMessageStandard("x", defaultButton: MessageBoxDefault.Button2));

        Assert.Same(form.CancelButton, form.ActiveControl);
        Assert.NotNull(ok.AcceptButton);
    }

    // ---- the details

    [Fact]
    public void WithoutDetails_ThereIsNoButtonForThem_WithDetails_TheyAreClosedUntilTheUserOpensThem()
    {
        MessageBoxForm without = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        MessageBoxForm with = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "Some details"));
        Button toggle = with.GetPrivateField<Button>("_detailsButton")!;
        TextBox box = with.GetPrivateField<TextBox>("_detailsBox")!;

        Assert.False(without.GetPrivateField<Button>("_detailsButton")!.Visible);
        Assert.True(toggle.Visible);
        Assert.False(with.DetailsExpanded);
        Assert.False(box.Visible);
        Assert.Equal(without.Height, with.Height);
        Assert.StartsWith("Details", toggle.Text);
    }

    [Fact]
    public void OpeningTheDetails_MakesTheWindowHigher_ShowsTheText_AndClosingGivesTheHeightBack()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "Line one\r\nLine two"));
        int closed = form.Height;
        Button toggle = form.GetPrivateField<Button>("_detailsButton")!;
        TextBox box = form.GetPrivateField<TextBox>("_detailsBox")!;

        toggle.PerformClick();

        Assert.True(form.DetailsExpanded);
        Assert.Equal(closed + 120, form.Height);
        Assert.True(box.Visible);
        Assert.Equal("Line one\r\nLine two", box.Text);
        Assert.True(box.ReadOnly);
        Assert.True(MessagePanel(form).Bottom <= box.Top);
        Assert.True(box.Bottom <= form.GetPrivateField<Control>("_buttonPanel")!.Top);
        Assert.EndsWith("\u25B4", toggle.Text);

        toggle.PerformClick();

        Assert.False(form.DetailsExpanded);
        Assert.Equal(closed, form.Height);
        Assert.False(box.Visible);
        Assert.EndsWith("\u25BE", toggle.Text);
    }

    [Fact]
    public void TheDetailsCanBeSetLaterOrTakenAway_AndExpandedFromCode()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        int closed = form.Height;

        form.DetailsExpanded = true;
        Assert.False(form.DetailsExpanded, "nothing to open without details");

        form.Details = "Now there are details";
        form.DetailsExpanded = true;
        Assert.True(form.DetailsExpanded);
        Assert.True(form.Height > closed);

        form.Details = null!;
        Assert.False(form.DetailsExpanded);
        Assert.False(form.GetPrivateField<Button>("_detailsButton")!.Visible);
        Assert.Equal(closed, form.Height);
    }

    [Fact]
    public void TheDetailsOfEachSize_HaveTheirOwnHeight_AndALongTextInThemScrolls()
    {
        int[] heights = new int[3];
        FormSize[] sizes = { FormSize.Small, FormSize.Medium, FormSize.Large };

        for (int i = 0; i < 3; i++)
        {
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", size: sizes[i], details: string.Join("\r\n", Enumerable.Range(1, 60).Select(n => "line " + n))));
            int closed = form.Height;
            form.DetailsExpanded = true;
            heights[i] = form.Height - closed;
            TextBox box = form.GetPrivateField<TextBox>("_detailsBox")!;

            Assert.Equal(ScrollBars.Vertical, box.ScrollBars);
        }

        Assert.True(heights[0] < heights[1] && heights[1] < heights[2], string.Join(", ", heights));
    }

    [Fact]
    public void TheWindowThatGrowsForTheDetails_DoesNotReachBelowTheScreen()
    {
        MessageBoxForm form = UIStyles.Forms.CreateMessageStandard("x", details: "d", size: FormSize.Large);
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(area.Left + 20, area.Bottom - form.Height - 5);
        _host.Show(form);

        form.DetailsExpanded = true;

        Assert.True(form.Bottom <= area.Bottom, "bottom " + form.Bottom + " of " + area.Bottom);
    }

    [Fact]
    public void TheResultIsTheSame_WithAndWithoutDetails_WhateverIsOpen()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, details: "d"));
        form.DetailsExpanded = true;

        form.Close();

        Assert.Equal(DialogResult.No, form.DialogResult);
    }
}
