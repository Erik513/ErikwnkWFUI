using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// The closed box of a drop-down list, drawn by the library: its border in every state, the
/// text, the arrow, and what stays the system's (the other drop-down styles). Probed on the
/// picture the control draws of itself. [Collection]: the colors read the live accent, and the
/// tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxClosedBoxTests
{
    private static ErikwnkWFUI.Controls.ComboBox Create(UIColorSet? colors = null, ComboBoxStyle style = ComboBoxStyle.DropDownList)
    {
        ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard(style, colors);
        box.Items.AddRange(new object[] { "First", "Second" });
        box.SelectedIndex = 0;
        box.Width = 160;

        return box;
    }

    private static Color LeftEdge(Control control)
    {
        using Bitmap bitmap = Draw(control);

        return bitmap.GetPixel(0, bitmap.Height / 2);
    }

    private static bool HasColor(Control control, Color color, Rectangle area)
    {
        using Bitmap bitmap = Draw(control);

        for (int y = area.Top; y < area.Bottom && y < bitmap.Height; y++)
        {
            for (int x = area.Left; x < area.Right && x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                    return true;
            }
        }

        return false;
    }

    private static void Enter(Control control) => control.InvokePrivate("OnMouseEnter", EventArgs.Empty);

    private static void Leave(Control control) => control.InvokePrivate("OnMouseLeave", EventArgs.Empty);

    [Fact]
    public void TheBorder_IsTheOutlineAtRest_AndStrongerHovered_ForTheGrayAndForASet()
    {
        foreach (UIColorSet? set in new UIColorSet?[] { null, UIStyles.Colors.Sets.Green })
        {
            using ErikwnkWFUI.Controls.ComboBox box = Create(set);
            UIColorSet colors = set ?? UIStyles.Colors.Sets.Gray;

            Assert.Equal(UIColors.GetOutlineColor(box.BackColor), LeftEdge(box));
            Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);

            Enter(box);
            Color hovered = UIColors.EnsureContrast(colors.Hover, box.BackColor, 4.5);
            Assert.Equal(hovered, box.BorderColor);
            Assert.Equal(hovered, LeftEdge(box));

            Leave(box);
            Assert.Equal(UIColors.GetOutlineColor(box.BackColor), LeftEdge(box));
        }
    }

    [Fact]
    public void TheHoverBorder_IsThickerThanTheRestOne()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();

        using (Bitmap rest = Draw(box))
            Assert.NotEqual(rest.GetPixel(0, 10), rest.GetPixel(1, 10));

        Enter(box);

        using (Bitmap hovered = Draw(box))
            Assert.Equal(hovered.GetPixel(0, 10), hovered.GetPixel(1, 10));
    }

    [Fact]
    public void ADisabledBox_HasTheFixedGrayBorder_AndNoHover()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();

        box.Enabled = false;
        Enter(box);

        Assert.Equal(UIColors.DisabledGray, box.BorderColor);
        Assert.Equal(UIColors.DisabledGray, LeftEdge(box));
    }

    [Fact]
    public void TheText_IsTheSelectedItems_InTheTextColor_AndFollowsTheSelection()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();
        Rectangle textArea = new Rectangle(4, 2, 110, box.Height - 4);

        Assert.True(HasColor(box, box.ForeColor, textArea), "no text in the text color");

        using Bitmap first = Draw(box);
        box.SelectedIndex = 1;
        using Bitmap second = Draw(box);

        bool differs = false;

        for (int x = 0; x < first.Width && !differs; x++)
        {
            for (int y = 0; y < first.Height; y++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                {
                    differs = true;
                    break;
                }
            }
        }

        Assert.True(differs, "the picture did not change with the selection");

        box.SelectedIndex = -1;
        Assert.False(HasColor(box, box.ForeColor, textArea), "text drawn without a selection");
    }

    // The first column and the last column that hold a pixel of the text color.
    private static (int First, int Last) TextColumns(Control control)
    {
        using Bitmap bitmap = Draw(control);
        int first = -1;
        int last = -1;

        for (int x = 3; x < bitmap.Width - 24; x++)
        {
            for (int y = 3; y < bitmap.Height - 3; y++)
            {
                if (bitmap.GetPixel(x, y) == control.ForeColor)
                {
                    if (first < 0)
                        first = x;

                    last = x;
                    break;
                }
            }
        }

        return (first, last);
    }

    [Fact]
    public void TheTextAndTheArrow_StayWhereTheyAre_WhenTheBorderGetsThicker()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();
        (int First, int Last) rest = TextColumns(box);

        using Bitmap restPicture = Draw(box);
        int arrowRest = ArrowColumn(restPicture, box);

        Enter(box);
        (int First, int Last) hovered = TextColumns(box);

        using Bitmap hoveredPicture = Draw(box);
        int arrowHovered = ArrowColumn(hoveredPicture, box);

        Assert.True(rest.First > 0, "no text found");
        Assert.Equal(rest, hovered);
        Assert.Equal(arrowRest, arrowHovered);
    }

    // The column where the arrow part has its leftmost inked pixel.
    private static int ArrowColumn(Bitmap bitmap, Control control)
    {
        for (int x = bitmap.Width - 24; x < bitmap.Width - 3; x++)
        {
            for (int y = 3; y < bitmap.Height - 3; y++)
            {
                if (bitmap.GetPixel(x, y) != control.BackColor)
                    return x;
            }
        }

        return -1;
    }

    [Fact]
    public void TheArrow_IsDrawnInTheArrowPart_InTheTextColor_AndGrayWhenDisabled()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();
        Rectangle arrowArea = new Rectangle(box.Width - 24, 2, 22, box.Height - 4);

        Assert.True(HasColor(box, box.ForeColor, arrowArea), "no arrow in the text color");

        box.Enabled = false;
        Assert.True(HasColor(box, UIColors.DisabledGray, arrowArea), "no gray arrow when disabled");
    }

    [Fact]
    public void TheEditableStyle_GetsItsBorderAndButtonFromTheLibrary_TheTextFieldStaysFree()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create(null, ComboBoxStyle.DropDown);
        Assert.True(box.OwnsEditableFramePainting);

        using Bitmap bitmap = Draw(box);
        Color border = box.BorderColor;
        Assert.Equal(border, bitmap.GetPixel(0, bitmap.Height / 2));
        Assert.Equal(border, bitmap.GetPixel(bitmap.Width - 1, bitmap.Height / 2));

        using ErikwnkWFUI.Controls.ComboBox simple = Create(null, ComboBoxStyle.Simple);
        Assert.False(simple.OwnsEditableFramePainting);
    }

    [Fact]
    public void TheStylesWithATextField_AreNotPaintedWholeByTheLibrary()
    {
        foreach (ComboBoxStyle style in new[] { ComboBoxStyle.DropDown, ComboBoxStyle.Simple })
        {
            using ErikwnkWFUI.Controls.ComboBox box = Create(null, style);

            // Not painted whole by the library (the text field inside stays the system's) ...
            Assert.False(box.OwnsClosedPainting, style + " is painted by the library");

            Exception? thrown = Record.Exception(() =>
            {
                using Bitmap bitmap = Draw(box);
                Assert.Equal(box.Size, bitmap.Size);
            });

            Assert.Null(thrown);
        }
    }

    [Fact]
    public void AfterAPick_TheBoxDoesNotStayHovered_WhenTheMouseIsNotOverIt()
    {
        // The mouse was over the open list, so the box never got a MouseLeave.
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        ErikwnkWFUI.Controls.ComboBox box = Create();
        host.Controls.Add(new Button());
        host.Controls.Add(box);
        host.Show();

        Enter(box);
        Assert.NotEqual(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);

        box.InvokePrivate("OnDropDownClosed", EventArgs.Empty);

        Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);
    }

    [Fact]
    public void TheSystemsOwnRepaintOfTheSelectionField_ComesOutAsTheSameClosedBox()
    {
        // On a focus change the system asks for the selection field as an item (ComboBoxEdit):
        // that must draw the closed box exactly as the normal paint does, not the text of a row.
        using ErikwnkWFUI.Controls.ComboBox box = Create();
        using Bitmap normal = Draw(box);
        using Bitmap viaItem = new Bitmap(box.Width, box.Height);

        using (Graphics g = Graphics.FromImage(viaItem))
        {
            DrawItemEventArgs args = new DrawItemEventArgs(g, box.Font, new Rectangle(3, 3, box.Width - 26, box.Height - 6), 0, DrawItemState.ComboBoxEdit);
            box.InvokePrivate("OnDrawItem", args);
        }

        for (int y = 0; y < normal.Height; y++)
        {
            for (int x = 0; x < normal.Width; x++)
                Assert.True(normal.GetPixel(x, y) == viaItem.GetPixel(x, y), "differs at " + x + "," + y);
        }
    }

    [Fact]
    public void OnlyTheDropDownList_IsPaintedByTheLibrary()
    {
        using ErikwnkWFUI.Controls.ComboBox list = Create();

        Assert.True(list.OwnsClosedPainting);
    }

    [Fact]
    public void TheLook_SurvivesTheHandleBeingRecreated()
    {
        using ErikwnkWFUI.Controls.ComboBox box = Create();
        Color before = LeftEdge(box);

        box.InvokePrivate("RecreateHandle");

        Assert.Equal(before, LeftEdge(box));
        Assert.Equal(0, box.SelectedIndex);
    }

    [Fact]
    public void AHiddenBox_ComesBackWithoutAHoverLook()
    {
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        ErikwnkWFUI.Controls.ComboBox box = Create();

        // The first control of a form gets the focus (and the focus look) when it opens.
        host.Controls.Add(new Button());
        host.Controls.Add(box);
        host.Controls.SetChildIndex(box, 0);
        host.Show();

        Enter(box);
        box.Visible = false;
        box.Visible = true;

        Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheBorder_StandsOutFromTheBox_AtRest_HoveredAndForEverySet(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                using ErikwnkWFUI.Controls.ComboBox box = Create((UIColorSet)property.GetValue(null)!);

                double rest = ColorContrastHelper.GetContrastRatio(box.BorderColor, box.BackColor);
                Enter(box);
                double hovered = ColorContrastHelper.GetContrastRatio(box.BorderColor, box.BackColor);

                if (rest < 3.0)
                    shortfalls.Add(property.Name + " rest " + rest.ToString("0.0"));

                if (hovered < 4.5)
                    shortfalls.Add(property.Name + " hovered " + hovered.ToString("0.0"));
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}

/// <summary>
/// A box that has the focus (after a pick with the mouse it keeps it) looks like any other
/// one: the focus does not change its border. Needs a real window with the focus, so it runs
/// on its own (see FocusTestCollection).
/// </summary>
[Trait("Focus", "true")]
[Collection(FocusTestCollection.Name)]
public class ComboBoxFocusTests
{
    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
            Thread.Sleep(40);
        }
    }

    [Fact]
    public void AFocusedBox_LooksLikeAnyOther_ItsBorderStaysTheRestOne()
    {
        StaThread.Run(() =>
        {
            using Form host = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
                ClientSize = new Size(300, 80)
            };

            ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard();
            box.Items.AddRange(new object[] { "First", "Second" });
            box.SelectedIndex = 0;
            box.Location = new Point(10, 10);
            host.Controls.Add(box);
            host.Show();
            Pump();

            box.Select();
            Pump();
            Assert.True(box.Focused, "the box did not get the focus");

            // Picking an entry (with the mouse) leaves the box focused, and unchanged.
            box.SelectedIndex = 1;
            Pump();

            Assert.True(box.Focused);
            Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);

            using Bitmap picture = new Bitmap(box.Width, box.Height);
            box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));
            Assert.Equal(UIColors.GetOutlineColor(box.BackColor), picture.GetPixel(0, box.Height / 2));
        });
    }

    [Fact]
    public void TheDottedFocusFrame_IsShownWhileTheKeyboardIsUsed_NotAfterAPickWithTheMouse()
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        StaThread.Run(() =>
        {
            using Form host = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
                ClientSize = new Size(300, 80)
            };

            ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard();
            box.Items.AddRange(new object[] { "First", "Second" });
            box.SelectedIndex = 0;
            box.Location = new Point(10, 10);
            host.Controls.Add(box);
            host.Show();
            Pump();

            // The keyboard is being used: Windows shows the focus cues.
            SendMessage(host.Handle, 0x127, (IntPtr)((1 << 16) | 2), IntPtr.Zero);
            box.Select();
            Pump();
            Assert.True(box.Focused);

            Bitmap Draw()
            {
                Bitmap picture = new Bitmap(box.Width, box.Height);
                box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));

                return picture;
            }

            box.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Down));
            using Bitmap withFrame = Draw();

            box.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            using Bitmap afterMouse = Draw();

            box.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Down));
            using Bitmap afterKey = Draw();

            Assert.False(SamePixels(withFrame, afterMouse), "the frame stays after the mouse was used");
            Assert.True(SamePixels(withFrame, afterKey), "the frame did not come back with the keyboard");
        });
    }
}
