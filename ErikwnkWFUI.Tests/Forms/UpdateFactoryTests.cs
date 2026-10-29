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
}
