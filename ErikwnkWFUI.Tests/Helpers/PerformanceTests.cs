using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Many controls at once: a panel of 500 check boxes, a list of 10 000 items and fifty toasts in a row. The budgets are many times what a normal
/// PC needs, so that only a step that grows with the square of the count (a layout that runs for every control added, a repaint of the whole
/// list for every item) fails; the time is in the message.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class PerformanceTests
{
    private static void Within(int milliseconds, string what, Action action)
    {
        Stopwatch watch = Stopwatch.StartNew();
        action();
        watch.Stop();

        Assert.True(watch.ElapsedMilliseconds <= milliseconds, what + " took " + watch.ElapsedMilliseconds + " ms (budget " + milliseconds + " ms)");
    }

    [Fact]
    public void FiveHundredCheckBoxes_AreMadeShownAndPaintedInReasonableTime()
    {
        using Panel panel = new Panel { Size = new Size(400, 600), AutoScroll = true };

        Within(8000, "making and showing 500 check boxes", () =>
        {
            for (int i = 0; i < 500; i++)
            {
                System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Option " + i, i % 2 == 0);
                box.Location = new Point(5, i * 24);
                panel.Controls.Add(box);
            }

            using Form host = OffscreenForm.Show(panel, new Size(420, 620), Color.Black, new Point(5, 5));
            Application.DoEvents();
            using Bitmap picture = TestPictures.Draw(panel);
        });

        Within(4000, "disposing 500 check boxes", panel.Controls.Clear);
    }

    [Fact]
    public void TenThousandListItems_AreAddedPaintedAndScrolledInReasonableTime()
    {
        ErikwnkWFUI.Controls.ListBox list = UIStyles.ListBoxes.CreateStandard();
        list.Size = new Size(300, 400);

        using Form host = OffscreenForm.Show(list, new Size(320, 420), Color.Black, new Point(5, 5));

        Within(5000, "adding 10 000 items", () =>
        {
            list.BeginUpdate();
            for (int i = 0; i < 10000; i++)
                list.Items.Add("Item " + i);
            list.EndUpdate();
            Application.DoEvents();
        });

        Within(5000, "painting and scrolling through 10 000 items", () =>
        {
            for (int top = 0; top < 10000; top += 500)
            {
                list.TopIndex = top;
                using Bitmap picture = TestPictures.Draw(list);
            }
        });

        Within(3000, "selecting the last of 10 000 items", () => list.SelectedIndex = 9999);
    }

    [Fact]
    public void FiftyToastsInARow_AreShownAndCloseWithTheirOwner()
    {
        using Form owner = new Form { Size = new Size(400, 300), StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        owner.Show();
        Application.DoEvents();

        Within(8000, "showing 50 toasts", () =>
        {
            for (int i = 0; i < 50; i++)
            {
                UIStyles.Toasts.ShowStandard("Message " + i, owner);
                Application.DoEvents();
            }
        });

        Within(4000, "closing the owner of 50 toasts", () =>
        {
            owner.Close();
            Application.DoEvents();
        });
    }
}
