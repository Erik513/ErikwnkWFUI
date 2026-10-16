using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The label at its limits, without any movement: every alignment, enabled and disabled; text with line
/// breaks, with signs from other scripts, with only ampersands; a label with more padding than room; the text
/// color of a label with a color set when the page behind it changes. [Collection]: the colors read the live
/// accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelEdgeCaseTests : IDisposable
{
    private static readonly Color Page = Color.FromArgb(40, 40, 40);

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    // The label as it is painted in a window that is really shown (out of sight), with the page behind it.
    private Bitmap Paint(Label label, Color? page = null)
    {
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = page ?? Page,
            ClientSize = new Size(label.Width + 10, label.Height + 10)
        };
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);

        using Bitmap whole = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(whole, new Rectangle(0, 0, form.Width, form.Height));

        return whole.Clone(new Rectangle(5, 5, label.Width, label.Height), whole.PixelFormat);
    }

    // The smallest rectangle that holds every pixel that is not the page; empty when there is no ink.
    private static Rectangle InkBounds(Bitmap bitmap, Color page)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);

                if (Math.Abs(pixel.R - page.R) + Math.Abs(pixel.G - page.G) + Math.Abs(pixel.B - page.B) > 24)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        }

        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static Color TextColor(Label label) => label.InvokePrivate<Color>("GetTextColor");

    public static IEnumerable<object[]> Alignments()
    {
        foreach (ContentAlignment alignment in Enum.GetValues(typeof(ContentAlignment)))
        {
            yield return new object[] { alignment, true };
            yield return new object[] { alignment, false };
        }
    }

    // ---- alignment ----

    [Theory]
    [MemberData(nameof(Alignments))]
    public void EveryAlignment_PutsTheTextInsideTheLabel_EnabledAndDisabled(ContentAlignment alignment, bool enabled)
    {
        using Label label = UIStyles.Labels.CreateStandard("Aligned text");
        label.Size = new Size(160, 50);
        label.TextAlign = alignment;
        label.Enabled = enabled;

        using Bitmap painted = Paint(label);
        Rectangle ink = InkBounds(painted, Page);

        Assert.False(ink.IsEmpty, "no text at all");
        Assert.True(new Rectangle(0, 0, 160, 50).Contains(ink), "the text leaves the label: " + ink);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheAlignment_PutsTheTextAtTheRightPlace_LeftCenterRight(bool enabled)
    {
        Rectangle Ink(ContentAlignment alignment)
        {
            using Label label = UIStyles.Labels.CreateStandard("Aligned");
            label.Size = new Size(200, 30);
            label.TextAlign = alignment;
            label.Enabled = enabled;
            using Bitmap painted = Paint(label);

            return InkBounds(painted, Page);
        }

        Rectangle left = Ink(ContentAlignment.MiddleLeft);
        Rectangle center = Ink(ContentAlignment.MiddleCenter);
        Rectangle right = Ink(ContentAlignment.MiddleRight);

        Assert.True(left.Left < center.Left && center.Left < right.Left);
        Assert.True(left.Left <= 6, "not at the left edge: " + left);
        Assert.True(right.Right >= 193, "not at the right edge: " + right);
        Assert.InRange(center.Left + center.Width / 2, 95, 105);
    }

    // ---- text of all kinds ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATextWithALineBreak_IsOnTwoLines_EnabledAndDisabled(bool enabled)
    {
        using Label one = UIStyles.Labels.CreateStandard("First line");
        using Label two = UIStyles.Labels.CreateStandard("First line\nSecond line");
        one.Size = two.Size = new Size(200, 60);
        one.TextAlign = two.TextAlign = ContentAlignment.TopLeft;
        one.Enabled = two.Enabled = enabled;

        using Bitmap first = Paint(one);
        using Bitmap second = Paint(two);

        Assert.True(InkBounds(second, Page).Height > InkBounds(first, Page).Height + 8, "no second line");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATextTooLong_EndsAtTheEdge_AndDoesNotRunOut(bool enabled)
    {
        using Label label = UIStyles.Labels.CreateStandard("A text that is much too long to fit in this small label of the test");
        label.Size = new Size(120, 24);
        label.Enabled = enabled;

        using Bitmap painted = Paint(label);
        Rectangle ink = InkBounds(painted, Page);

        Assert.True(ink.Right <= 120, "the text runs out: " + ink);
        Assert.True(ink.Right > 90, "the text is cut far too early: " + ink);
    }

    [Theory]
    [InlineData("Umlauts: äöüß ÄÖÜ")]
    [InlineData("日本語のテキスト")]
    [InlineData("שלום עולם")]
    [InlineData("Emoji 😀 🚀")]
    [InlineData("Tab\tseparated\tcolumns")]
    public void TextFromOtherScripts_AndWithATab_IsPainted_EnabledAndDisabled(string text)
    {
        foreach (bool enabled in new[] { true, false })
        {
            using Label label = UIStyles.Labels.CreateStandard(text);
            label.Size = new Size(240, 30);
            label.Enabled = enabled;

            using Bitmap painted = Paint(label);

            Assert.False(InkBounds(painted, Page).IsEmpty, "nothing painted for " + text);
        }
    }

    [Fact]
    public void OnlyAmpersands_AreShownAsTheyAre()
    {
        using Label one = UIStyles.Labels.CreateStandard("&");
        using Label three = UIStyles.Labels.CreateStandard("&&&");
        one.Size = three.Size = new Size(100, 24);

        using Bitmap first = Paint(one);
        using Bitmap second = Paint(three);

        // Three signs are about three times as wide as one: none of them is taken for the mark of a key.
        Assert.True(InkBounds(second, Page).Width > InkBounds(first, Page).Width * 2);
    }

    [Fact]
    public void AWhitespaceOnlyText_PaintsNothing_WithoutAnError()
    {
        using Label label = UIStyles.Labels.CreateStandard("      ");
        label.Size = new Size(100, 24);

        using Bitmap painted = Paint(label);

        Assert.True(InkBounds(painted, Page).IsEmpty);
    }

    // ---- no room ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MorePaddingThanRoom_AndASizeOfNothing_DoNotFail(bool enabled)
    {
        using Label padded = UIStyles.Labels.CreateStandard("Padded");
        padded.Size = new Size(60, 24);
        padded.Padding = new Padding(100, 20, 100, 20);
        padded.Enabled = enabled;

        Assert.Null(Record.Exception(() => Paint(padded).Dispose()));

        using Label tiny = UIStyles.Labels.CreateStandard("Tiny");
        tiny.Size = new Size(1, 1);
        tiny.Enabled = enabled;

        Assert.Null(Record.Exception(() => Paint(tiny).Dispose()));
    }

    // ---- the colors of a label with a color set ----

    [Fact]
    public void TheTextOfALabelWithASet_FollowsThePageBehindIt_WhenThePageChanges()
    {
        using Label label = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);
        using Panel page = new Panel { BackColor = Color.Black };
        page.Controls.Add(label);

        Color onDark = TextColor(label);
        page.BackColor = Color.White;
        Color onLight = TextColor(label);

        Assert.NotEqual(onDark, onLight);
        Assert.True(ColorContrastHelper.GetContrastRatio(onDark, Color.Black) >= 4.49);
        Assert.True(ColorContrastHelper.GetContrastRatio(onLight, Color.White) >= 4.49);
    }

    [Fact]
    public void ALabelWithAnOpaqueBackColor_ReadsAgainstItsOwnColor_NotTheParents()
    {
        using Label label = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);
        using Panel page = new Panel { BackColor = Color.White };
        page.Controls.Add(label);
        label.BackColor = Color.Black;

        Assert.True(ColorContrastHelper.GetContrastRatio(TextColor(label), Color.Black) >= 4.49);
    }

    [Fact]
    public void ALabelWithATransparentChain_ReadsAgainstTheFirstSolidOneAbove_OrTheThemeWithoutAny()
    {
        using Label label = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);
        using Panel outer = new Panel { BackColor = Color.White };
        using Panel inner = new Panel { BackColor = Color.Transparent };
        outer.Controls.Add(inner);
        inner.Controls.Add(label);

        Assert.True(ColorContrastHelper.GetContrastRatio(TextColor(label), Color.White) >= 4.49);

        using Label alone = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);

        Assert.True(ColorContrastHelper.GetContrastRatio(TextColor(alone), UIColors.BackgroundMedium) >= 4.49);
    }

    // ---- enabling and disabling ----

    [Fact]
    public void EnablingAndDisablingOften_LeavesTheColorsAsTheyWere()
    {
        using Label label = UIStyles.Labels.CreateStandard("x");
        label.ForeColor = Color.Orange;

        for (int i = 0; i < 20; i++)
        {
            label.Enabled = i % 2 == 0;
            Assert.Equal(label.Enabled ? Color.Orange : UIColors.DisabledGray, TextColor(label));
        }

        Assert.Equal(Color.Orange, label.ForeColor);
    }
}
