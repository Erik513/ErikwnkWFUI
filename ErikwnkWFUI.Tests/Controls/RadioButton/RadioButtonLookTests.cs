using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.RadioButtons;

/// <summary>What the radio button paints: the box on the left or - with a right-hand CheckAlign - on the right.</summary>
[Collection(AccentColorTestCollection.Name)]
public class RadioButtonLookTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private System.Windows.Forms.RadioButton Shown(System.Windows.Forms.RadioButton radio)
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 60) };
        radio.Location = new Point(10, 10);
        radio.Width = 200;
        host.Controls.Add(radio);
        host.Show();
        _hosts.Add(host);

        return radio;
    }

    private static bool HasInk(Bitmap picture, Rectangle area)
    {
        Color back = picture.GetPixel(area.Left, 0);

        for (int x = area.Left; x < area.Right; x++)
            for (int y = area.Top; y < area.Bottom; y++)
                if (picture.GetPixel(x, y) != back)
                    return true;

        return false;
    }

    [Theory]
    [InlineData(ContentAlignment.TopRight)]
    [InlineData(ContentAlignment.MiddleRight)]
    [InlineData(ContentAlignment.BottomRight)]
    public void ARightHandCheckAlign_PutsTheBoxOnTheRight(ContentAlignment align)
    {
        System.Windows.Forms.RadioButton radio = Shown(UIStyles.RadioButtons.CreateStandard("Label", true));
        using Bitmap left = Draw(radio);

        radio.CheckAlign = align;
        using Bitmap right = Draw(radio);

        Assert.True(HasInk(left, new Rectangle(0, 0, 24, radio.Height)));
        Assert.True(HasInk(right, new Rectangle(radio.Width - 24, 0, 24, radio.Height)));

        radio.CheckAlign = ContentAlignment.MiddleLeft;
        using Bitmap back = Draw(radio);
        Assert.True(SamePixels(left, back));
    }

    [Fact]
    public void ASelectedAndAnEmptyButton_LookDifferent_InBothThemes()
    {
        foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
        {
            UIColors.ApplyTheme(theme);
            System.Windows.Forms.RadioButton on = Shown(UIStyles.RadioButtons.CreateStandard("Label", true));
            System.Windows.Forms.RadioButton off = Shown(UIStyles.RadioButtons.CreateStandard("Label", false));

            using Bitmap onPicture = Draw(on);
            using Bitmap offPicture = Draw(off);

            Assert.False(SamePixels(onPicture, offPicture));
        }
    }

    [Fact]
    public void ADisabledButton_LooksDifferentFromAnEnabledOne()
    {
        System.Windows.Forms.RadioButton enabled = Shown(UIStyles.RadioButtons.CreateStandard("Label", true));
        System.Windows.Forms.RadioButton disabled = Shown(UIStyles.RadioButtons.CreateStandard("Label", true));
        disabled.Enabled = false;

        using Bitmap a = Draw(enabled);
        using Bitmap b = Draw(disabled);

        Assert.False(SamePixels(a, b));
    }
}