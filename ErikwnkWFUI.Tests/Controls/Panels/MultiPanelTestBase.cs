using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Xunit;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.Panels;

public abstract class MultiPanelTestBase: IDisposable
{
    protected readonly List<Form> _forms = new();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    protected MultiPanel Shown(MultiPanel panel, int pages)
    {
        for (int i = 1; i <= pages; i++)
            panel.AddPage("Page " + i);

        panel.Dock = DockStyle.None;
        panel.Size = new Size(400, 200);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(420, 220) };
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return panel;
    }

    protected static Button[] Arrows(MultiPanel panel)
    {
        return panel.Controls.OfType<Button>().OrderBy(button => button.Left).ToArray();
    }

    // PerformClick only works on buttons that can take the focus, which the arrows cannot (they are clicked with the mouse).
    protected static void Click(Button button)
    {
        typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(button, new object[] { EventArgs.Empty });
    }

    protected static Bitmap Render(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    // The columns of the header (without the arrows) where something other than the background is painted.
    protected static (int First, int Last) InkColumns(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        Color back = bitmap.GetPixel(panel.Width / 2, panel.Height - 12);
        int arrowsLeft = Arrows(panel)[0].Left - 2;
        int first = -1, last = -1;

        for (int x = 12; x < arrowsLeft; x++)
        {
            for (int y = 6; y < 30; y++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != back.ToArgb())
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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    protected static (WeakReference Panel, MultiPanelPage Page) MakeAndRemove()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        MultiPanelPage page = panel.AddPage("A");
        panel.AddPage("B");
        panel.RemovePage(page);
        panel.Dispose();

        return (new WeakReference(panel), page);
    }

    protected static byte[] ToBytes(Bitmap bitmap)
    {
        using System.IO.MemoryStream stream = new System.IO.MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

        return stream.ToArray();
    }

    protected static bool PressKey(MultiPanel panel, Keys keys)
    {
        object[] arguments = { new Message(), keys };

        return (bool)typeof(Control).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(panel, arguments)!;
    }

    protected static int Brightest(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        int best = 0;

        for (int x = 12; x < 100; x++)
            for (int y = 6; y < 30; y++)
                best = Math.Max(best, bitmap.GetPixel(x, y).R);

        return best;
    }

    protected static int Darkest(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        int best = 255;

        for (int x = 12; x < 100; x++)
            for (int y = 6; y < 30; y++)
                best = Math.Min(best, bitmap.GetPixel(x, y).R);

        return best;
    }
}
