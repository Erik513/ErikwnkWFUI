using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>The frame of the focus in the two small arrow fields is one pixel, like the line around the whole box (two pixels made a heavy ring in a field of 14 pixels).</summary>
[Collection(FocusTestCollection.Name)]
public class NumericUpDownFocusFrameTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiNumericUpDown Focused(UIColorSet? colors)
    {
        WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(colors: colors);
        Form form = OffscreenForm.Create(new Size(200, 100), UIColors.BackgroundMedium);
        box.Location = new Point(10, 10);
        form.Controls.Add(box);
        _forms.Add(form);
        form.Show();
        Application.DoEvents();
        box.Focus();
        Application.DoEvents();

        return box;
    }

    // The field (its fill state 0, rest) painted on its own: a column through the middle, from the edge inward.
    private static Color[] Column(WfuiNumericUpDown box, UIColorSet set, int count)
    {
        using Bitmap picture = new Bitmap(14, 16);
        using (Graphics graphics = Graphics.FromImage(picture))
            box.InvokePrivate("PaintField", graphics, new Rectangle(0, 0, 14, 16), set, 0, true);

        return Enumerable.Range(0, count).Select(x => picture.GetPixel(x, 8)).ToArray();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFrameOfAnArrowFieldThatHasTheFocus_IsOnePixel_AndTheFillIsRightInsideIt(bool light)
    {
        try
        {
            UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);

            foreach (UIColorSet? colors in new UIColorSet?[] { null, UIStyles.Colors.Sets.Green, UIStyles.Colors.Sets.Yellow })
            {
                WfuiNumericUpDown box = Focused(colors);
                Assert.True(box.ContainsFocus, "the box has the focus");

                UIColorSet set = colors ?? WfuiNumericUpDown.GetNeutralSet();
                Color fill = WfuiNumericUpDown.GetStateColors(set, 0, box.BackColor).Fill;
                Color[] column = Column(box, set, 3);

                Assert.NotEqual(fill.ToArgb(), column[0].ToArgb());
                Assert.Equal(fill.ToArgb(), column[1].ToArgb());
                Assert.Equal(fill.ToArgb(), column[2].ToArgb());
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}