using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>UIStyles.Forms.CreateUpdateAvailableStandard / CreateUpdateAvailablePrimary: the title, the color set, the sizes.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UpdateFactoryTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
    }

    [Fact]
    public void TheFactories_MakeAFixedDialog_WithTheTitleOfTheLanguage_OrTheOneGiven()
    {
        UpdateAvailableForm standard = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0"));
        UpdateAvailableForm named = _host.Show(UIStyles.Forms.CreateUpdateAvailablePrimary("1.0.0", "1.1.0", "New version"));

        Assert.Equal("Update available", standard.Text);
        Assert.Equal("New version", named.Text);
        Assert.Equal(standard.MinimumSize, standard.MaximumSize);
        Assert.False(standard.ShowInTaskbar);
        Assert.False(standard.UpdateChosen);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Update verfügbar", UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0").Text);
    }

    [Fact]
    public void TheDefaultSize_IsTheOneItAlwaysHad_AndTheThreeSizesGrow()
    {
        Assert.Equal(new Size(420, 190), _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2")).Size);
        Assert.Equal(new Size(420, 190), _host.Show(new UpdateAvailableForm("1", "2")).Size);

        Size small = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", size: FormSize.Small)).Size;
        Size medium = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", size: FormSize.Medium)).Size;
        Size large = _host.Show(UIStyles.Forms.CreateUpdateAvailablePrimary("1", "2", size: FormSize.Large)).Size;

        Assert.True(small.Width < medium.Width && medium.Width < large.Width);
        Assert.True(small.Height < medium.Height && medium.Height < large.Height);
        Assert.Equal(new Size(500, 260), _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", size: new Size(500, 260))).Size);
    }

    [Fact]
    public void TheTitleBar_TakesAColorSet_OrTheAccentWithPrimary()
    {
        UpdateAvailableForm plain = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2"));
        UpdateAvailableForm yellow = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", colors: UIStyles.Colors.Sets.Yellow));
        UpdateAvailableForm primary = _host.Show(UIStyles.Forms.CreateUpdateAvailablePrimary("1", "2"));

        Assert.NotEqual(plain.TitleBar.BackColor.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.NotEqual(plain.TitleBar.BackColor.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void EveryKindOfSize_KeepsBothButtonsInsideTheWindow_InBothLanguages()
    {
        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            UIStrings.Language = language;
            UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0", size: size));

            foreach (Button button in new[] { (Button)form.AcceptButton!, (Button)form.CancelButton! })
            {
                Rectangle inForm = form.ContentPanel.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                Assert.True(form.ContentPanel.ClientRectangle.Contains(inForm), language + ", " + size + ": " + button.Text + " " + inForm);
            }
        }
    }

    // ---- release notes and skip

    private static Button NotesButton(UpdateAvailableForm form)
    {
        return form.GetPrivateField<Button>("_notesButton")!;
    }

    private static Button SkipButton(UpdateAvailableForm form)
    {
        return form.GetPrivateField<Button>("_skipButton")!;
    }

    [Fact]
    public void WithoutNotesAndSkip_ThereAreNoButtonsForThem_AndTheWindowIsThePlainOne()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2"));

        Assert.False(NotesButton(form).Visible);
        Assert.False(SkipButton(form).Visible);
        Assert.False(form.AllowSkip);
        Assert.False(form.SkipChosen);
        Assert.Equal("", form.ReleaseNotes);
        Assert.Equal(new Size(420, 190), form.Size);

        form.NotesExpanded = true;
        Assert.False(form.NotesExpanded);
    }

    [Fact]
    public void TheNotes_HaveAButton_ThatOpensThemUnderTheVersions_AndTheWindowGrowsAndShrinksAgain()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", releaseNotes: "- faster\n- fixed a bug\n\nThanks!"));
        TextBox box = form.GetPrivateField<TextBox>("_notesBox")!;
        int closedHeight = form.Height;

        Assert.True(NotesButton(form).Visible);
        Assert.StartsWith("What's new", NotesButton(form).Text);
        Assert.False(form.NotesExpanded);
        Assert.Equal("- faster\r\n- fixed a bug\r\n\r\nThanks!", box.Text);

        NotesButton(form).PerformClick();

        Assert.True(form.NotesExpanded);
        Assert.True(form.Height > closedHeight);
        Assert.Equal(form.MinimumSize, form.MaximumSize);
        Assert.True(box.Height > 40, "the box has room: " + box.Height);
        Assert.True(box.Bottom <= form.ContentPanel.ClientSize.Height);

        NotesButton(form).PerformClick();
        Assert.Equal(closedHeight, form.Height);

        form.ReleaseNotes = "";
        Assert.False(NotesButton(form).Visible);
    }

    [Fact]
    public void ANotesTextThatIsOnlyBlank_IsNoNotes()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", releaseNotes: "  \n \n"));

        Assert.False(NotesButton(form).Visible);
    }

    [Fact]
    public void Skip_EndsTheDialogWithSkipChosen_NotWithUpdateChosen()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", allowSkip: true));

        Assert.True(SkipButton(form).Visible);
        SkipButton(form).PerformClick();

        Assert.True(form.SkipChosen);
        Assert.False(form.UpdateChosen);
        Assert.Equal(DialogResult.Ignore, form.DialogResult);
    }

    [Fact]
    public void EverySizeInBothLanguages_HoldsNotesSkipLaterAndUpdate_SideBySide_TheWindowGrowsWhereNeeded()
    {
        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            UIStrings.Language = language;
            UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0", size: size, releaseNotes: "Notes", allowSkip: true));
            string where = language + ", " + size;
            Button[] buttons = { NotesButton(form), SkipButton(form), (Button)form.CancelButton!, (Button)form.AcceptButton! };
            Rectangle[] bounds = buttons.Select(b => form.ContentPanel.RectangleToClient(b.RectangleToScreen(b.ClientRectangle))).ToArray();

            for (int i = 0; i < buttons.Length; i++)
            {
                int needed = TextRenderer.MeasureText(buttons[i].Text, buttons[i].Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
                Assert.True(needed + 4 <= buttons[i].Width, where + ": '" + buttons[i].Text + "' needs " + needed + " of " + buttons[i].Width);
                Assert.True(form.ContentPanel.ClientRectangle.Contains(bounds[i]), where + ": " + buttons[i].Text + " " + bounds[i]);

                for (int j = i + 1; j < buttons.Length; j++)
                    Assert.False(bounds[i].IntersectsWith(bounds[j]), where + ": '" + buttons[i].Text + "' overlaps '" + buttons[j].Text + "'");
            }
        }
    }

    [Fact]
    public void AnOwnSize_IsKept_EvenWithNotesAndSkip()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", size: new Size(500, 230), releaseNotes: "Notes", allowSkip: true));

        Assert.Equal(500, form.Width);
        Assert.Equal(230, form.Height);
    }

    [Fact]
    public void TheNewTexts_ExistInBothLanguages()
    {
        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        {
            UIStrings.Language = language;

            foreach (string key in new[] { "Update.Skip", "Update.WhatsNew", "Update.DownloadFailed", "Update.Later", "Update.UpdateNow", "Update.Downloading", "Update.AvailableTitle", "Update.AvailableMessage" })
                Assert.NotEqual(key, UIStrings.Get(key));
        }

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Diese Version überspringen", UIStrings.Get("Update.Skip"));
        Assert.Equal("Neuigkeiten", UIStrings.Get("Update.WhatsNew"));
    }
}