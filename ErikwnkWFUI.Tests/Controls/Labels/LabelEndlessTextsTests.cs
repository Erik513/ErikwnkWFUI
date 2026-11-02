using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// Several texts that move one behind the other (LabelScroll.Endless(texts)): the separator between each two,
/// the same room after each of them. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelEndlessTextsTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private ScrollingLabel Create(LabelScroll scroll, int width = 400)
    {
        ScrollingLabel label = UIStyles.Labels.CreateStandard("Plain", scroll: scroll);
        label.Size = new Size(width, 24);

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(40, 40, 40),
            ClientSize = new Size(width + 10, 60)
        };
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);

        return label;
    }

    private static int Cycle(ScrollingLabel label) => label.GetPrivateField<int>("_cycle");

    private static void SetOffset(ScrollingLabel label, float offset)
    {
        typeof(ScrollingLabel).GetField("_offset", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(label, offset);
    }

    // The columns of ink, joined to groups wherever the gap between two columns is not wider than 8 pixels.
    private static List<(int Start, int End)> Groups(ScrollingLabel label)
    {
        using Bitmap bitmap = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(bitmap, new Rectangle(0, 0, label.Width, label.Height));
        List<(int Start, int End)> groups = new List<(int Start, int End)>();

        for (int x = 0; x < bitmap.Width; x++)
        {
            bool ink = false;

            for (int y = 0; y < bitmap.Height && !ink; y++)
                ink = bitmap.GetPixel(x, y).GetBrightness() > 0.45f;

            if (!ink)
                continue;

            if (groups.Count > 0 && x - groups[groups.Count - 1].End <= 8)
                groups[groups.Count - 1] = (groups[groups.Count - 1].Start, x);
            else
                groups.Add((x, x));
        }

        return groups;
    }

    [Fact]
    public void TheTexts_AreTheTextOfTheLabelWhereItDoesNotMove_JoinedBytheSeparator()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "One", "Two" }, separator: LabelScrollSeparator.Diamond));

        Assert.Equal("One ◆ Two", label.Text);
        Assert.Equal(new[] { "One", "Two" }, label.Scroll.Texts);
        Assert.Equal(LabelScrollMode.Endless, label.ScrollMode);
    }

    [Fact]
    public void TheTextsOfAScroll_CannotBeChangedThroughTheList()
    {
        string[] given = { "One", "Two" };
        LabelScroll scroll = LabelScroll.Endless(given);

        Assert.False(scroll.Texts is string[]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)scroll.Texts!)[0] = "Changed");
        given[0] = "Changed";
        Assert.Equal(new[] { "One", "Two" }, scroll.Texts);
    }

    [Fact]
    public void ANewSeparator_ChangesTheJoinedText_AndTheTextsStay()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "One", "Two" }));

        label.ScrollSeparator = LabelScrollSeparator.Bar;

        Assert.Equal("One | Two", label.Text);
        Assert.Equal(new[] { "One", "Two" }, label.Scroll.Texts);
    }

    [Fact]
    public void WithoutASeparator_TheTextsAreJoinedByASpace()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "One", "Two" }, separator: LabelScrollSeparator.None));

        Assert.Equal("One Two", label.Text);
    }

    [Fact]
    public void EmptyTexts_AreLeftOut_AndNoTexts_MeansTheTextOfTheLabel()
    {
        Assert.Equal(new[] { "A", "B" }, LabelScroll.Endless(new[] { "A", "", null!, "  ", "B" }).Texts);
        Assert.Null(LabelScroll.Endless(new string[0]).Texts);
        Assert.Null(LabelScroll.Endless(null!).Texts);
        Assert.Null(LabelScroll.Endless().Texts);
        Assert.Null(LabelScroll.BackAndForth().Texts);
    }

    [Fact]
    public void ANewText_EndsTheTexts_AndTheLabelMovesThatTextAlone()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "One", "Two" }));

        label.Text = "Another";

        Assert.Null(label.Scroll.Texts);
        Assert.Equal("Another", label.Text);
        Assert.True(Cycle(label) > 0);
    }

    [Fact]
    public void ANewScroll_WithoutTexts_EndsThem()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "One", "Two" }));

        label.Scroll = LabelScroll.Endless();

        Assert.Null(label.Scroll.Texts);
    }

    [Fact]
    public void TheRoomAfterEveryText_IsTheSame_TheSeparatorInTheMiddleOfIt()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "Aa", "Bb" }, separator: LabelScrollSeparator.Bar));
        Assert.Equal(400, Cycle(label));
        SetOffset(label, 0f);

        List<(int Start, int End)> groups = Groups(label);

        // The text, the bar, the text, the bar - and the round begins again.
        Assert.Equal(4, groups.Count);

        int[] gaps =
        {
            groups[1].Start - groups[0].End,
            groups[2].Start - groups[1].End,
            groups[3].Start - groups[2].End,
            400 + groups[0].Start - groups[3].End
        };

        for (int i = 1; i < gaps.Length; i++)
            Assert.InRange(gaps[i], gaps[0] - 4, gaps[0] + 4);
    }

    [Fact]
    public void SeveralTexts_TakeTheirOwnRoom_WhenTheLabelIsShort()
    {
        using ScrollingLabel label = Create(LabelScroll.Endless(new[] { "A rather long first text", "and a second one" }), width: 100);

        // Both texts, and a gap, the separator and a gap behind each of them.
        Assert.True(Cycle(label) > 2 * 100);
    }
}
