using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// The themed combo box: how a row of the list is drawn (the selected or hovered one in
/// the neutral gray, in a color set or in the accent), and that the box is made as
/// described. The rows are drawn straight into a bitmap through the control's own draw
/// handler. [Collection]: the colors read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxTests
{
    private static System.Windows.Forms.ComboBox Create(UIColorSet? colors = null)
    {
        System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDownList, colors);
        box.Items.AddRange(new object[] { "One", "Two", "Three" });

        return box;
    }

    // Draws row 'index' as the control would, and returns the picture.
    private static Bitmap DrawRow(System.Windows.Forms.ComboBox box, int index, DrawItemState state)
    {
        Bitmap bitmap = new Bitmap(120, 24);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            DrawItemEventArgs args = new DrawItemEventArgs(g, box.Font, new Rectangle(0, 0, 120, 24), index, state);
            box.InvokePrivate("OnDrawItem", args);
        }

        return bitmap;
    }

    private static Color Background(System.Windows.Forms.ComboBox box, int index, DrawItemState state)
    {
        using Bitmap bitmap = DrawRow(box, index, state);

        // The right end of the row is clear of the text.
        return bitmap.GetPixel(115, 12);
    }

    [Fact]
    public void ItIsMadeFlatAndOwnerDrawn_InTheThemesColorsAndFont()
    {
        using System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard();

        Assert.Equal(ComboBoxStyle.DropDownList, box.DropDownStyle);
        Assert.Equal(FlatStyle.Flat, box.FlatStyle);
        Assert.Equal(DrawMode.OwnerDrawFixed, box.DrawMode);
        Assert.Equal(UIColors.BackgroundLight, box.BackColor);
        Assert.Equal(UIColors.TextPrimary, box.ForeColor);
        Assert.Same(UIFonts.Normal, box.Font);

        using System.Windows.Forms.ComboBox editable = UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDown);
        Assert.Equal(ComboBoxStyle.DropDown, editable.DropDownStyle);
    }

    [Fact]
    public void ARowThatIsNotSelected_IsTheNormalBackground_AndTheSelectedOneTheNeutralGray()
    {
        using System.Windows.Forms.ComboBox box = Create();

        Assert.Equal(UIColors.BackgroundLight, Background(box, 0, DrawItemState.None));
        Assert.Equal(UIColors.BorderLight, Background(box, 0, DrawItemState.Selected));
    }

    [Fact]
    public void WithASet_TheSelectedRowIsTheSetsColor_AndItsTextTheSetsHoverText()
    {
        // A set with its own text color (not the one the contrast rule would pick).
        UIColorSet set = UIStyles.Colors.CreateSet(
            Color.FromArgb(10, 70, 30), Color.FromArgb(10, 80, 40), Color.FromArgb(40, 180, 90), Color.FromArgb(255, 210, 0));
        using System.Windows.Forms.ComboBox box = Create(set);

        Assert.Equal(UIColors.BackgroundLight, Background(box, 0, DrawItemState.None));
        Assert.Equal(set.Hover, Background(box, 0, DrawItemState.Selected));

        // The text: some pixel of the row is exactly the text color of the set.
        using Bitmap selected = DrawRow(box, 1, DrawItemState.Selected);
        bool hasText = false;

        for (int y = 0; y < selected.Height && !hasText; y++)
        {
            for (int x = 0; x < selected.Width; x++)
            {
                if (selected.GetPixel(x, y) == set.HoverText)
                {
                    hasText = true;
                    break;
                }
            }
        }

        Assert.True(hasText, "the row text is not in the set's text color");
    }

    [Fact]
    public void Primary_IsTheStandardWithThePrimarySet_AndFollowsTheAccent()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            using System.Windows.Forms.ComboBox primary = UIStyles.ComboBoxes.CreatePrimary();
            primary.Items.Add("One");
            using System.Windows.Forms.ComboBox viaSet = Create(UIStyles.Colors.Sets.Primary);

            Color expected = UIStyles.Colors.Sets.Primary.Hover;
            Assert.Equal(expected, Background(primary, 0, DrawItemState.Selected));
            Assert.Equal(expected, Background(viaSet, 0, DrawItemState.Selected));
            Assert.Equal(UIColors.Primary, expected);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void ASet_LooksTheSame_InBothThemes_AndAnyColorWorks()
    {
        try
        {
            foreach (Color color in new[] { Color.FromArgb(230, 120, 20), Color.FromArgb(250, 220, 60), Color.FromArgb(15, 15, 15), Color.FromArgb(128, 128, 128) })
            {
                UIColors.ApplyTheme(UIThemes.Dark);
                using System.Windows.Forms.ComboBox dark = Create(UIStyles.Colors.CreateSet(color));
                Color darkRow = Background(dark, 0, DrawItemState.Selected);

                UIColors.ApplyTheme(UIThemes.Light);
                using System.Windows.Forms.ComboBox light = Create(UIStyles.Colors.CreateSet(color));

                Assert.Equal(darkRow, Background(light, 0, DrawItemState.Selected));
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void TheClosedBoxItself_IsNeverHighlighted_OnlyARowOfTheOpenList()
    {
        // The area that shows the current choice is drawn with the ComboBoxEdit state
        // (also while the box has the focus): it must stay in the normal background.
        using System.Windows.Forms.ComboBox box = Create(UIStyles.Colors.Sets.Green);

        Assert.Equal(
            UIColors.BackgroundLight,
            Background(box, 0, DrawItemState.Selected | DrawItemState.ComboBoxEdit));
    }

    [Fact]
    public void ADisabledBox_DrawsItsTextMuted()
    {
        using System.Windows.Forms.ComboBox box = Create();

        Color Brightest()
        {
            using Bitmap bitmap = DrawRow(box, 0, DrawItemState.ComboBoxEdit);
            int best = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color c = bitmap.GetPixel(x, y);
                    best = Math.Max(best, c.R + c.G + c.B);
                }
            }

            return Color.FromArgb(best / 3, best / 3, best / 3);
        }

        Color enabled = Brightest();
        box.Enabled = false;
        Color disabled = Brightest();

        Assert.True(disabled.R < enabled.R, "a disabled box looks like an enabled one");
    }

    [Fact]
    public void TheRowText_FollowsDisplayMember_AndNoRowOrAnOutOfRangeRowDrawsNothingWrong()
    {
        using System.Windows.Forms.ComboBox box = UIStyles.ComboBoxes.CreateStandard();
        box.DisplayMember = "Name";
        box.Items.Add(new Entry("Shown", 1));

        using Bitmap named = DrawRow(box, 0, DrawItemState.None);

        using System.Windows.Forms.ComboBox plain = UIStyles.ComboBoxes.CreateStandard();
        plain.Items.Add("Shown");
        using Bitmap text = DrawRow(plain, 0, DrawItemState.None);

        Assert.True(SamePixels(named, text), "the row shows the object, not its DisplayMember");

        // Index -1 (an empty box) and one past the end: nothing drawn, nothing thrown.
        Exception? thrown = Record.Exception(() =>
        {
            using Bitmap none = DrawRow(box, -1, DrawItemState.None);
            using Bitmap past = DrawRow(box, 5, DrawItemState.None);
        });

        Assert.Null(thrown);
    }

    private sealed class Entry
    {
        public Entry(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }

        public int Id { get; }
    }

}
