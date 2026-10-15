using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The card: rounded corners that show the page behind them, a thin line, and what can be set.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelCardTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private ThemedCardPanel Card()
    {
        ThemedCardPanel card = (ThemedCardPanel)UIStyles.Panels.CreateCard();
        card.Size = new Size(100, 60);
        card.BackColor = Color.Black;
        card.Location = new Point(20, 20);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 200), BackColor = Color.White };
        host.Controls.Add(card);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return card;
    }

    private static Color Pixel(Control control, int x, int y)
    {
        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));
        Color color = bitmap.GetPixel(x, y);

        return Color.FromArgb(color.R, color.G, color.B);
    }

    [Fact]
    public void TheCard_HasTheElevatedBackground_NoDock_AndAPaddingOfItsOwn()
    {
        using Control card = UIStyles.Panels.CreateCard();

        Assert.IsType<ThemedCardPanel>(card);
        Assert.Equal(UIColors.BackgroundMediumElevated, card.BackColor);
        Assert.Equal(DockStyle.None, card.Dock);
        Assert.True(card.Padding.All > 0);
        Assert.Equal(Padding.Empty, card.Margin);
    }

    [Fact]
    public void TheCorners_ShowThePageBehind_TheMiddleTheCardColor_TheEdgeTheLine()
    {
        ThemedCardPanel card = Card();

        Assert.Equal(Color.White.ToArgb(), Pixel(card, 0, 0).ToArgb());
        Assert.Equal(Color.White.ToArgb(), Pixel(card, 99, 59).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), Pixel(card, 50, 30).ToArgb());
        Assert.Equal(UIColors.BorderMedium, Pixel(card, 50, 0));
        Assert.Equal(UIColors.BorderMedium, Pixel(card, 0, 30));
    }

    [Fact]
    public void TheCornerIsSmooth_ThereAreInBetweenColors()
    {
        ThemedCardPanel card = Card();
        card.BorderColor = Color.Black;
        int inBetween = 0;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Color pixel = Pixel(card, x, y);

                if (pixel.R > 20 && pixel.R < 235)
                    inBetween++;
            }
        }

        Assert.True(inBetween >= 6, "the corner is not smooth: " + inBetween);
    }

    [Fact]
    public void WhatIsSet_IsUsed_AndTheLineColorFollowsTheThemeUntilThen()
    {
        ThemedCardPanel card = Card();

        card.ShowBorder = false;
        Assert.Equal(Color.Black.ToArgb(), Pixel(card, 50, 0).ToArgb());

        card.ShowBorder = true;
        card.BorderColor = Color.Red;
        Assert.Equal(Color.Red.ToArgb(), Pixel(card, 50, 0).ToArgb());

        card.BorderColor = Color.Empty;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Assert.Equal(UIColors.BorderMedium, Pixel(card, 50, 0));

        card.CornerRadius = 0;
        Assert.NotEqual(Color.White.ToArgb(), Pixel(card, 0, 0).ToArgb()); // square: the corner is the card's
    }

    [Fact]
    public void ANegativeRadius_IsZero()
    {
        using ThemedCardPanel card = new ThemedCardPanel { CornerRadius = -5 };

        Assert.Equal(0, card.CornerRadius);
    }
}
