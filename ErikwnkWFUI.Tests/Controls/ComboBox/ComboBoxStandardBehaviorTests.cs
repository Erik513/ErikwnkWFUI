using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// The standard combo box has more to offer than the library draws itself. Each of these
/// tests pins one answer: a variant that works (and keeps working), or a property the library
/// takes over on purpose and ignores. The decisions are written on the class
/// <c>ErikwnkWFUI.Controls.ComboBox</c>. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxStandardBehaviorTests
{
    private static ErikwnkWFUI.Controls.ComboBox Create(ComboBoxStyle style = ComboBoxStyle.DropDownList)
    {
        ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard(style);
        box.Items.AddRange(new object[] { "Cherry", "Apple", "Banana" });
        box.Width = 160;

        return box;
    }

    private static Bitmap Draw(Control control)
    {
        _ = control.Handle;
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    private static bool SamePixels(Bitmap first, Bitmap second)
    {
        if (first.Size != second.Size)
            return false;

        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }

    private static Bitmap DrawRow(System.Windows.Forms.ComboBox box, int index, DrawItemState state)
    {
        Bitmap bitmap = new Bitmap(120, 24);

        using (Graphics g = Graphics.FromImage(bitmap))
            box.InvokePrivate("OnDrawItem", new DrawItemEventArgs(g, box.Font, new Rectangle(0, 0, 120, 24), index, state));

        return bitmap;
    }

    // ---- variants that work ----

    [Fact]
    public void ASortedBox_ListsAlphabetically_AndDrawsItsRows()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();
        box.Sorted = true;

        Assert.Equal(new object[] { "Apple", "Banana", "Cherry" }, box.Items.Cast<object>().ToArray());

        using Bitmap row = DrawRow(box, 1, DrawItemState.None);
        Assert.Equal(UIColors.BackgroundLight, row.GetPixel(115, 12));
    }

    [Fact]
    public void TheWidthAndTheHeightOfTheList_AreTheStandardOnes()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();

        box.DropDownWidth = 300;
        box.MaxDropDownItems = 5;
        box.IntegralHeight = false;
        box.DropDownHeight = 120;

        Assert.Equal(300, box.DropDownWidth);
        Assert.Equal(5, box.MaxDropDownItems);
        Assert.Equal(120, box.DropDownHeight);
    }

    [Fact]
    public void ARowHeight_GrowsWithTheFont_SoARowIsNeverCutOff()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();
        int normal = box.ItemHeight;

        using Font large = new Font(box.Font.FontFamily, box.Font.Size * 2);
        box.Font = large;

        Assert.True(box.ItemHeight > normal, "the rows stay as low as for the small font");
        Assert.True(box.ItemHeight >= large.Height, "a row is lower than its text");
    }

    [Fact]
    public void AutoComplete_CanBeSwitchedOn_ForTheEditableStyle_WithoutTroubleForTheBox()
    {
        // The completion list needs a thread of the single-threaded apartment (OLE).
        StaThread.Run(() =>
        {
            ErikwnkWFUI.Controls.ComboBox box = Create(ComboBoxStyle.DropDown);

            Exception? thrown = Record.Exception(() =>
            {
                box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                box.AutoCompleteSource = AutoCompleteSource.ListItems;
                _ = box.Handle;
            });

            Assert.Null(thrown);
            Assert.Equal(AutoCompleteMode.SuggestAppend, box.AutoCompleteMode);
        });
    }

    [Fact]
    public void AHandlerOfTheDrawItemEvent_RunsAfterTheLibraryAndCanChangeTheRow()
    {
        // The library draws first and then raises the event: an application can still paint over a row.
        ErikwnkWFUI.Controls.ComboBox box = Create();
        box.DrawItem += (sender, e) => e.Graphics.FillRectangle(Brushes.Magenta, e.Bounds);

        using Bitmap row = DrawRow(box, 0, DrawItemState.None);

        Assert.Equal(Color.FromArgb(255, 0, 255), row.GetPixel(60, 12));
    }

    [Fact]
    public void ADrawModeOfVariableHeight_StillDrawsTheRows()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();
        box.DrawMode = DrawMode.OwnerDrawVariable;
        box.MeasureItem += (sender, e) => e.ItemHeight = 30;

        Exception? thrown = Record.Exception(() =>
        {
            _ = box.Handle;
            using Bitmap row = DrawRow(box, 0, DrawItemState.Selected);
            Assert.Equal(UIColors.BorderLight, row.GetPixel(115, 12));
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void SelectionChangeCommitted_FollowsTheUser_NotTheCode()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        host.Controls.Add(box);
        host.Show();
        int committed = 0;
        box.SelectionChangeCommitted += delegate { committed++; };

        box.SelectedIndex = 1;
        Assert.Equal(0, committed);

        // A key press on the box is the user.
        box.Select();
        SendMessage(box.Handle, 0x100, (IntPtr)(int)Keys.Down, IntPtr.Zero);
        SendMessage(box.Handle, 0x101, (IntPtr)(int)Keys.Down, IntPtr.Zero);
        Application.DoEvents();

        Assert.Equal(1, committed);
        host.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    // ---- taken over on purpose: these properties are ignored ----

    [Fact]
    public void FlatStyleAndRightToLeft_AreIgnored_ForTheClosedBoxOfADropDownList()
    {
        ErikwnkWFUI.Controls.ComboBox box = Create();
        box.SelectedIndex = 0;
        using Bitmap before = Draw(box);

        box.FlatStyle = FlatStyle.Popup;
        box.RightToLeft = RightToLeft.Yes;

        using Bitmap after = Draw(box);
        Assert.True(SamePixels(before, after), "FlatStyle or RightToLeft changed the picture");
    }

    [Fact]
    public void TheStylesWithATextField_KeepTheSystemsTextField_ButTheRowsAreTheLibrarys()
    {
        // Only the drop-down list has its whole closed box drawn by the library; the styles with
        // a text field inside keep that field the system's (the editable one gets its border and
        // arrow from the library), and their list is drawn row by row like the others.
        foreach (ComboBoxStyle style in new[] { ComboBoxStyle.DropDown, ComboBoxStyle.Simple })
        {
            ErikwnkWFUI.Controls.ComboBox box = Create(style);

            Assert.False(box.OwnsClosedPainting, style + " is drawn by the library");

            using Bitmap selected = DrawRow(box, 0, DrawItemState.Selected);
            Assert.Equal(UIColors.BorderLight, selected.GetPixel(115, 12));
        }
    }
}
