using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>Everything of the message box can be read and seen, in both themes, with the standard look, a color set and Primary.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxColorTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void EverythingCanBeReadAndSeen_InBothThemes(bool lightTheme)
    {
        ColorContrastTestSupport.Run(lightTheme, shortfalls =>
        {
            foreach (string look in new[] { "standard", "yellow", "red", "primary" })
            {
                MessageBoxForm form = _host.Show(look == "primary"
                    ? UIStyles.Forms.CreateMessagePrimary("A message", buttons: MessageBoxButtons.AbortRetryIgnore, details: "Details")
                    : UIStyles.Forms.CreateMessageStandard("A message", buttons: MessageBoxButtons.AbortRetryIgnore, details: "Details",
                        colors: look == "yellow" ? UIStyles.Colors.Sets.Yellow : look == "red" ? UIStyles.Colors.Sets.Red : null));
                form.CheckBoxText = "Do not ask again";
                form.ShowHelpButton = true;
                form.DetailsExpanded = true;

                Label message = form.GetPrivateField<Label>("_messageLabel")!;
                Color back = message.BackColor.A == 0 ? form.GetPrivateField<Panel>("_messagePanel")!.BackColor : message.BackColor;
                CheckBox box = form.GetPrivateField<CheckBox>("_checkBox")!;
                TextBox details = form.GetPrivateField<TextBox>("_detailsBox")!;

                ColorContrastTestSupport.Check(shortfalls, look + ", message", message.ForeColor, back, 4.5);
                ColorContrastTestSupport.Check(shortfalls, look + ", details", details.ForeColor, details.BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, look + ", check box (it draws its text in TextPrimary)", UIColors.TextPrimary, box.Parent!.BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, look + ", details button", form.GetPrivateField<Button>("_detailsButton")!.ForeColor, form.GetPrivateField<Button>("_detailsButton")!.BackColor, 4.5);

                foreach (Button button in form.GetPrivateField<List<Button>>("_dialogButtons")!)
                    ColorContrastTestSupport.Check(shortfalls, look + ", " + button.Text, button.ForeColor, button.BackColor, 4.5);
            }
        });
    }

    [Fact]
    public void TheOpenBox_FollowsALiveChangeOfTheTheme_InAllItsParts()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("A message", details: "Details"));
        form.CheckBoxText = "Do not ask again";
        form.DetailsExpanded = true;
        Label message = form.GetPrivateField<Label>("_messageLabel")!;
        TextBox details = form.GetPrivateField<TextBox>("_detailsBox")!;

        try
        {
            UIColors.ApplyTheme(UIThemes.Light);
            Application.DoEvents();

            Assert.Equal(UIColors.TextPrimary.ToArgb(), message.ForeColor.ToArgb());
            Assert.True(UIColors.IsLight(details.BackColor), "details " + details.BackColor);
            Assert.True(UIColors.IsLight(form.GetPrivateField<Control>("_buttonPanel")!.BackColor));
            Assert.False(UIColors.IsLight(message.ForeColor));
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
