using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The text on the panels is readable on every surface and in both themes; a disabled panel is quieter.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelColorTests : IDisposable
{
    private const double ReadableText = 4.5;
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private static IEnumerable<Color> Surfaces()
    {
        yield return Color.Black;
        yield return Color.White;
        yield return Color.Gray;
        yield return UIColors.BackgroundBlack;
        yield return UIColors.BackgroundMedium;
        yield return UIColors.BackgroundMediumElevated;
        yield return UIColors.PrimaryDarkDark;
        yield return UIStyles.Colors.Sets.Green.Basic;
        yield return UIStyles.Colors.Sets.Yellow.Basic;
        yield return UIStyles.Colors.Sets.Red.Basic;
        yield return UIStyles.Colors.Sets.Orange.Basic;
    }

    [Theory]
    [InlineData(0.35)]
    [InlineData(0.45)]
    [InlineData(0.9)]
    public void MutedText_StaysReadable_OnEverySurface_InBothThemes(double amount)
    {
        foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
        {
            UIColors.ApplyTheme(theme);

            foreach (Color surface in Surfaces())
            {
                Color muted = UIColors.GetMutedText(surface, amount);
                Color plain = UIColors.BlendToward(UIColors.GetBestTextColor(surface), surface, amount);

                Assert.True(ColorContrastHelper.GetContrastRatio(muted, surface) >= ReadableText, $"{surface} -> {muted}");

                // Where the plain blend is readable already, nothing is changed.
                if (ColorContrastHelper.GetContrastRatio(plain, surface) >= ReadableText)
                    Assert.Equal(plain, muted);
            }
        }
    }

    [Fact]
    public void MutedText_WithoutAnyFade_IsTheBestTextColor()
    {
        foreach (Color surface in Surfaces())
            Assert.Equal(UIColors.GetBestTextColor(surface), UIColors.GetMutedText(surface, 0));
    }

    // The ink of a picture: the pixel that stands out most from the surface.
    private static Color Ink(Bitmap picture, Color surface)
    {
        Color best = surface;
        double bestRatio = 1;

        for (int x = 0; x < picture.Width; x++)
        {
            for (int y = 0; y < picture.Height; y++)
            {
                Color pixel = Color.FromArgb(picture.GetPixel(x, y).R, picture.GetPixel(x, y).G, picture.GetPixel(x, y).B);
                double ratio = ColorContrastHelper.GetContrastRatio(pixel, surface);

                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    best = pixel;
                }
            }
        }

        return best;
    }

    private Bitmap Draw(Control control, Rectangle area)
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 100) };
        control.Dock = DockStyle.None;
        control.Location = new Point(10, 10);
        control.Size = new Size(220, control is MultiPanel ? 80 : 24);
        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        using Bitmap whole = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(whole, new Rectangle(Point.Empty, control.Size));

        return whole.Clone(area, whole.PixelFormat);
    }

    public static IEnumerable<object[]> Variants()
    {
        foreach (var theme in new[] { "Dark", "Light" })
        {
            yield return new object[] { theme, "Standard", (Func<VersionPanel>)(() => UIStyles.VersionPanels.CreateStandard()) };
            yield return new object[] { theme, "Primary", (Func<VersionPanel>)(() => UIStyles.VersionPanels.CreatePrimary()) };
            yield return new object[] { theme, "Green", (Func<VersionPanel>)(() => UIStyles.VersionPanels.CreateStandard(UIStyles.Colors.Sets.Green)) };
            yield return new object[] { theme, "Yellow", (Func<VersionPanel>)(() => UIStyles.VersionPanels.CreateStandard(UIStyles.Colors.Sets.Yellow)) };
        }
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void TheVersionText_IsReadable_OnEverySurface_InBothThemes(string theme, string name, Func<VersionPanel> make)
    {
        UIColors.ApplyTheme(theme == "Dark" ? UIThemes.Dark : UIThemes.Light);
        VersionPanel panel = make();
        panel.VersionText = "v1.2.3";

        Color surface = SurfaceOf(panel);
        using Bitmap picture = Draw(panel, new Rectangle(120, 0, 100, 24));

        Color ink = Ink(picture, surface);

        // (The picture is smoothed: the pixel that stands out most is a little paler than the text itself.)
        Assert.True(ColorContrastHelper.GetContrastRatio(ink, surface) >= ReadableText - 0.5, $"{name} in {theme}: {ink} on {surface}");
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void ADisabledVersionPanel_IsQuieterThanAnEnabledOne_ButNotGone(string theme, string name, Func<VersionPanel> make)
    {
        UIColors.ApplyTheme(theme == "Dark" ? UIThemes.Dark : UIThemes.Light);
        VersionPanel enabled = make();
        enabled.VersionText = "v1.2.3";
        VersionPanel disabled = make();
        disabled.VersionText = "v1.2.3";
        disabled.Enabled = false;

        Color surface = SurfaceOf(enabled);
        double normal, quiet;

        using (Bitmap picture = Draw(enabled, new Rectangle(120, 0, 100, 24)))
            normal = ColorContrastHelper.GetContrastRatio(Ink(picture, surface), surface);

        using (Bitmap picture = Draw(disabled, new Rectangle(120, 0, 100, 24)))
            quiet = ColorContrastHelper.GetContrastRatio(Ink(picture, surface), surface);

        Assert.True(quiet < normal, $"{name} in {theme}: {quiet} < {normal}");
        Assert.True(quiet > 1.3, $"{name} in {theme}: {quiet}");
    }

    private static Color SurfaceOf(Control control) => control.BackColor;

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void TheCountAndTheTitleOfAMultiPanel_AreReadable_AndADisabledOneIsQuieter(string theme)
    {
        UIColors.ApplyTheme(theme == "Dark" ? UIThemes.Dark : UIThemes.Light);

        foreach (Func<MultiPanel> make in new Func<MultiPanel>[]
        {
            () => UIStyles.MultiPanels.CreateStandard(),
            () => UIStyles.MultiPanels.CreateCardStandard(),
            () => UIStyles.MultiPanels.CreateCardPrimary(),
            () => UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Green),
            () => UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Yellow)
        })
        {
            MultiPanel panel = make();
            panel.AddPage("Title");
            panel.AddPage("Other");
            panel.ShowPageCount = true;
            Color surface = panel.BackColor;

            MultiPanel quietPanel = make();
            quietPanel.AddPage("Title");
            quietPanel.AddPage("Other");
            quietPanel.ShowPageCount = true;
            quietPanel.Enabled = false;

            // The header is in the upper left of the area inside the padding: the page count first, the title behind it.
            Rectangle count = new Rectangle(8, 8, 18, 24);
            Rectangle title = new Rectangle(40, 8, 80, 24);

            foreach (Rectangle part in new[] { count, title })
            {
                double normal, quiet;

                using (Bitmap picture = Draw(panel, part))
                    normal = ColorContrastHelper.GetContrastRatio(Ink(picture, surface), surface);

                using (Bitmap picture = Draw(quietPanel, part))
                    quiet = ColorContrastHelper.GetContrastRatio(Ink(picture, surface), surface);

                // (The picture is smoothed: the pixel that stands out most is a little paler than the text itself.)
                Assert.True(normal >= ReadableText - 0.5, $"{surface} in {theme} at {part}: {normal}");
                Assert.True(quiet < normal, $"{surface} in {theme} at {part}: {quiet} < {normal}");
                Assert.True(quiet > 1.3, $"{surface} in {theme} at {part}: {quiet}");
            }
        }
    }

    [Fact]
    public void TheQuietLine_IsTheThemesBorderColor_WhereItIsSeen_AndMovedOnlyAsFarAsNeededWhereItIsNot()
    {
        foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
        {
            UIColors.ApplyTheme(theme);

            foreach (Color surface in Surfaces())
            {
                Color line = UIColors.GetLineColor(surface);
                double plain = ColorContrastHelper.GetContrastRatio(UIColors.BorderMedium, surface);

                Assert.True(ColorContrastHelper.GetContrastRatio(line, surface) >= 1.45, $"{surface} -> {line}");

                if (plain >= 1.5)
                    Assert.Equal(UIColors.BorderMedium, line);
            }
        }
    }

    // A panel in a color (a set): its frame, the lines of a table and the line of a card are seen on it, in both themes.
    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void TheFrameTheLinesAndTheCardLine_AreSeenOnAColor_InBothThemes(string theme)
    {
        UIColors.ApplyTheme(theme == "Dark" ? UIThemes.Dark : UIThemes.Light);

        foreach (Color surface in new[] { UIStyles.Colors.Sets.Green.Basic, UIStyles.Colors.Sets.Yellow.Basic, UIColors.PrimaryDarkDark, Color.Gray })
        {
            ThemedPanel panel = new ThemedPanel { BackColor = surface, BorderStyle = BorderStyle.FixedSingle, Size = new Size(80, 40) };
            ThemedTableLayoutPanel table = new ThemedTableLayoutPanel { BackColor = surface, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single, ColumnCount = 2, RowCount = 1, Size = new Size(80, 40) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            ThemedCardPanel card = new ThemedCardPanel { BackColor = surface, Size = new Size(80, 40) };

            Assert.True(ColorContrastHelper.GetContrastRatio(panel.BorderColor, surface) >= 1.45, $"frame on {surface}");
            Assert.True(ColorContrastHelper.GetContrastRatio(table.CellBorderColor, surface) >= 1.45, $"lines on {surface}");
            Assert.True(ColorContrastHelper.GetContrastRatio(table.InnerCellBorderColor, surface) >= 1.45, $"inner lines on {surface}");
            Assert.True(ColorContrastHelper.GetContrastRatio(card.BorderColor, surface) >= 1.45, $"card line on {surface}");

            // Set by hand it stays as it was set.
            panel.BorderColor = surface;
            Assert.Equal(surface, panel.BorderColor);
        }
    }
}
