using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// What the info popup does with its content and its window: the blocks and what is left out,
/// the size that follows the content or stays fixed, the three places, the show delay, a
/// disposed owner, the position on the screen. Popups are shown for real (briefly) next to a
/// host form. [Collection]: the colors read the live accent.
/// </summary>
public abstract class InfoPopupTestBase: IDisposable
{
    protected readonly List<Form> _forms = new List<Form>();

    protected readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;

    // The popups are shown only while the application is in front; a test must not depend on
    // what is in front on the machine it runs on.
    public InfoPopupTestBase()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;

        foreach (Form form in _forms)
            form.Dispose();
    }

    protected (Form Host, Button Owner) CreateHost(Point? location = null, Size? clientSize = null, Point? ownerLocation = null)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = location ?? new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = clientSize ?? new Size(300, 200)
        };
        Button owner = new Button { Location = ownerLocation ?? new Point(20, 60), Size = new Size(80, 30) };

        host.Controls.Add(owner);
        host.Show();
        _forms.Add(host);

        return (host, owner);
    }

    protected InfoPopupForm CreatePopup(Size? fixedSize = null)
    {
        InfoPopupForm popup = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(fixedSize: fixedSize);
        _forms.Add(popup);

        return popup;
    }

    protected static InfoPopupSection Section(string? header, string? text) => new InfoPopupSection(header!, text!);

    // What the popup holds now: header and text of each block ("-" for a missing part).
    protected static string[] Content(InfoPopupForm popup)
    {
        InfoPopupSection[] sections = popup.GetPrivateField<InfoPopupSection[]>("_sections")!;

        return sections.Select(section => (section.Header ?? "-") + "|" + (section.Text ?? "-")).ToArray();
    }

    // The number of pixels that are not the background: the text that was drawn.
    protected static int InkPixels(Form form)
    {
        using Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        int ink = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != form.BackColor)
                    ink++;
            }
        }

        return ink;
    }

    protected static int Count(Form form, Color color)
    {
        using Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        int count = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    count++;
            }
        }

        return count;
    }

    // Where the popup may be: the window, as far as it is on the screen.
    protected static Rectangle Area(Form host)
    {
        return Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea);
    }

    protected static Point OnScreen(Form host, int x, int y)
    {
        return host.PointToScreen(new Point(x, y));
    }

    protected static int WaitInterval(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Interval;

    protected static int Interval(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_hideTimer")!.Interval;

    protected static bool TimeRuns(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_hideTimer")!.Enabled;
}
